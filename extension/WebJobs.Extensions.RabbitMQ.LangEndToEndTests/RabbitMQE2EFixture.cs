// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Diagnostics;
using Azure.Storage.Queues;
using RabbitMQ.Client;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.RabbitMQ.LangEndToEndTests;

/// <summary>
/// Test fixture that manages Docker Compose lifecycle for Lang E2E tests.
/// </summary>
public class RabbitMQE2EFixture : IAsyncLifetime
{
    private readonly string _dockerComposeDirectory;
    private Process? _dockerComposeProcess;

    public HttpClient HttpClient { get; } = new HttpClient
    {
        Timeout = Constants.Timeouts.HttpRequest
    };

    public QueueServiceClient QueueServiceClient { get; private set; } = null!;

    public RabbitMQE2EFixture()
    {
        // Find docker-compose.yml directory
        var currentDir = Directory.GetCurrentDirectory();
        _dockerComposeDirectory = FindDockerComposeDirectory(currentDir)
            ?? throw new InvalidOperationException($"Could not find docker-compose.yml in {currentDir} or parent directories");
    }

    public async Task InitializeAsync()
    {
        Console.WriteLine("Starting Docker Compose...");

        // Start docker-compose
        await StartDockerComposeAsync();

        // Wait for RabbitMQ to be healthy
        await WaitForRabbitMQAsync();

        // Wait for Azurite to be ready
        await WaitForAzuriteAsync();

        // Initialize Queue Service Client
        QueueServiceClient = new QueueServiceClient(Constants.Azurite.ConnectionString);

        // Create result queue if not exists
        var resultQueueClient = QueueServiceClient.GetQueueClient(Constants.Queues.ResultQueue);
        await resultQueueClient.CreateIfNotExistsAsync();

        // Wait for function apps to be ready
        await WaitForFunctionAppsAsync();

        Console.WriteLine("Docker Compose environment ready.");
    }

    public async Task DisposeAsync()
    {
        Console.WriteLine("Stopping Docker Compose...");
        await StopDockerComposeAsync();
        HttpClient.Dispose();
    }

    private static string? FindDockerComposeDirectory(string startDir)
    {
        // First, try to find the source directory by looking for the .git folder
        // This ensures we use the original docker-compose.yml with correct relative paths
        var dir = startDir;
        while (!string.IsNullOrEmpty(dir))
        {
            var gitPath = Path.Combine(dir, ".git");
            if (Directory.Exists(gitPath) || File.Exists(gitPath))
            {
                // Found repo root, look for docker-compose.yml in LangEndToEndTests
                var dockerComposePath = Path.Combine(dir, "extension", "WebJobs.Extensions.RabbitMQ.LangEndToEndTests", "docker-compose.yml");
                if (File.Exists(dockerComposePath))
                {
                    return Path.GetDirectoryName(dockerComposePath);
                }
            }

            dir = Path.GetDirectoryName(dir);
        }

        // Fallback: search from current directory upward (for local development)
        dir = startDir;
        while (!string.IsNullOrEmpty(dir))
        {
            var dockerComposePath = Path.Combine(dir, "docker-compose.yml");
            if (File.Exists(dockerComposePath))
            {
                // Make sure this is the source directory, not the bin output directory
                // by checking if FunctionApps/java/Dockerfile exists
                var javaDockerfile = Path.Combine(dir, "FunctionApps", "java", "Dockerfile");
                if (File.Exists(javaDockerfile))
                {
                    return dir;
                }
            }

            dir = Path.GetDirectoryName(dir);
        }

        return null;
    }

    /// <summary>
    /// Gets the docker compose command. Tries "docker compose" (V2) first, then falls back to "docker-compose" (V1).
    /// </summary>
    private static (string FileName, IReadOnlyList<string> CommandPrefix) GetDockerComposeCommand()
    {
        // Try docker compose V2 first (docker compose as a subcommand)
        try
        {
            var psi = CreateProcessStartInfo("docker", ["compose", "version"]);

            using var process = Process.Start(psi);
            if (process is not null)
            {
                process.WaitForExit();
                if (process.ExitCode == 0)
                {
                    Console.WriteLine("Using docker compose V2");
                    return ("docker", ["compose"]);
                }
            }
        }
        catch (Win32Exception)
        {
            // Ignore and try V1
        }

        // Fall back to docker-compose V1
        Console.WriteLine("Using docker-compose V1");
        return ("docker-compose", []);
    }

    private async Task StartDockerComposeAsync()
    {
        var (fileName, commandPrefix) = GetDockerComposeCommand();
        var arguments = new List<string>(commandPrefix);
        AddComposeFiles(arguments);
        arguments.Add("up");
        arguments.Add("-d");
        arguments.Add("--build");

        var psi = CreateProcessStartInfo(fileName, arguments, _dockerComposeDirectory);

        Console.WriteLine($"Running: {fileName} {string.Join(' ', arguments)} in {_dockerComposeDirectory}");

        _dockerComposeProcess = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start docker-compose");

        var outputTask = _dockerComposeProcess.StandardOutput.ReadToEndAsync();
        var errorTask = _dockerComposeProcess.StandardError.ReadToEndAsync();
        await _dockerComposeProcess.WaitForExitAsync();
        var output = await outputTask;
        var error = await errorTask;

        // The function app images are built here, so this is the only record of why a build
        // step failed. Emit it before throwing, since the exception message alone is liable to
        // be truncated by test reporters. Successful runs stay quiet.
        if (_dockerComposeProcess.ExitCode != 0)
        {
            WriteComposeOutput(output, error);

            throw new InvalidOperationException(
                $"docker-compose up failed with exit code {_dockerComposeProcess.ExitCode}."
                + $"{Environment.NewLine}--- stdout ---{Environment.NewLine}{output}"
                + $"{Environment.NewLine}--- stderr ---{Environment.NewLine}{error}");
        }
    }

    private static void WriteComposeOutput(string output, string error)
    {
        if (!string.IsNullOrWhiteSpace(output))
        {
            Console.WriteLine($"docker compose stdout:{Environment.NewLine}{output}");
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            Console.WriteLine($"docker compose stderr:{Environment.NewLine}{error}");
        }
    }

    private async Task StopDockerComposeAsync()
    {
        var (fileName, commandPrefix) = GetDockerComposeCommand();
        var arguments = new List<string>(commandPrefix);
        AddComposeFiles(arguments);
        arguments.Add("down");
        arguments.Add("--remove-orphans");

        var psi = CreateProcessStartInfo(fileName, arguments, _dockerComposeDirectory);

        using var process = Process.Start(psi);
        if (process is not null)
        {
            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            await Task.WhenAll(outputTask, errorTask);
        }
    }

    private void AddComposeFiles(List<string> arguments)
    {
        var cfsOverride = Environment.GetEnvironmentVariable("CFS_DOCKER_COMPOSE_OVERRIDE");
        if (string.IsNullOrWhiteSpace(cfsOverride))
        {
            return;
        }

        if (!File.Exists(cfsOverride))
        {
            throw new FileNotFoundException("The CFS Docker Compose override file does not exist.", cfsOverride);
        }

        arguments.Add("--file");
        arguments.Add(Path.Combine(_dockerComposeDirectory, "docker-compose.yml"));

        var localOverride = Path.Combine(_dockerComposeDirectory, "docker-compose.override.yml");
        if (File.Exists(localOverride))
        {
            arguments.Add("--file");
            arguments.Add(localOverride);
        }

        arguments.Add("--file");
        arguments.Add(cfsOverride);
    }

    private static ProcessStartInfo CreateProcessStartInfo(
        string fileName, IEnumerable<string> arguments, string? workingDirectory = null)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            WorkingDirectory = workingDirectory ?? string.Empty,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        foreach (var argument in arguments)
        {
            processStartInfo.ArgumentList.Add(argument);
        }

        // BuildKit's default progress collapses each step's output, so a failing build step
        // reports only its exit code. Plain progress keeps the step logs. This is captured
        // into a buffer rather than streamed, so it costs nothing on a successful run.
        if (!processStartInfo.Environment.ContainsKey("BUILDKIT_PROGRESS"))
        {
            processStartInfo.Environment["BUILDKIT_PROGRESS"] = "plain";
        }

        return processStartInfo;
    }

    private async Task WaitForRabbitMQAsync()
    {
        Console.WriteLine("Waiting for RabbitMQ...");
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < Constants.Timeouts.DockerComposeStartup)
        {
            try
            {
                var factory = new ConnectionFactory
                {
                    HostName = Constants.RabbitMQ.Host,
                    Port = Constants.RabbitMQ.Port,
                    UserName = Constants.RabbitMQ.User,
                    Password = Constants.RabbitMQ.Password
                };

                using var connection = await factory.CreateConnectionAsync();
                Console.WriteLine("RabbitMQ is ready.");
                return;
            }
            catch
            {
                await Task.Delay(1000);
            }
        }

        throw new TimeoutException("RabbitMQ did not become ready in time");
    }

    private async Task WaitForAzuriteAsync()
    {
        Console.WriteLine("Waiting for Azurite...");
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < Constants.Timeouts.ServiceHealthCheck)
        {
            try
            {
                var client = new QueueServiceClient(Constants.Azurite.ConnectionString);
                await client.GetPropertiesAsync();
                Console.WriteLine("Azurite is ready.");
                return;
            }
            catch
            {
                await Task.Delay(1000);
            }
        }

        throw new TimeoutException("Azurite did not become ready in time");
    }

    private async Task WaitForFunctionAppsAsync()
    {
        Console.WriteLine("Waiting for Function Apps...");

        // Wait for Java app
        await WaitForFunctionAppAsync("Java", Constants.FunctionApps.Java.BaseUrl);

        // Wait for Python app
        await WaitForFunctionAppAsync("Python", Constants.FunctionApps.Python.BaseUrl);
    }

    private async Task WaitForFunctionAppAsync(string name, string baseUrl)
    {
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < Constants.Timeouts.DockerComposeStartup)
        {
            try
            {
                var response = await HttpClient.GetAsync(baseUrl);
                Console.WriteLine($"{name} app is ready (status: {response.StatusCode}).");
                return;
            }
            catch
            {
                await Task.Delay(2000);
            }
        }

        throw new TimeoutException($"{name} Function App did not become ready in time");
    }

    /// <summary>
    /// Creates a queue in RabbitMQ for testing.
    /// </summary>
    public async Task CreateRabbitMQQueueAsync(string queueName)
    {
        var factory = new ConnectionFactory
        {
            HostName = Constants.RabbitMQ.Host,
            Port = Constants.RabbitMQ.Port,
            UserName = Constants.RabbitMQ.User,
            Password = Constants.RabbitMQ.Password
        };

        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();
        await channel.QueueDeclareAsync(queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);
    }
}

/// <summary>
/// Collection definition for RabbitMQ E2E tests.
/// </summary>
[CollectionDefinition("RabbitMQ Lang E2E")]
public class RabbitMQE2ECollection : ICollectionFixture<RabbitMQE2EFixture>
{
}
