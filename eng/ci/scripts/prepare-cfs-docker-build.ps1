[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Initialize', 'Finalize', 'Cleanup')]
    [string] $Phase,

    [Parameter(Mandatory)]
    [string] $RepositoryRoot,

    [Parameter(Mandatory)]
    [string] $OutputDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$npmRegistry = 'https://pkgs.dev.azure.com/azfunc/public/_packaging/upstream-public/npm/registry/'
$nugetRegistry = 'https://pkgs.dev.azure.com/azfunc/public/_packaging/upstream-public/nuget/v3/index.json'
$repositoryRootPath = [System.IO.Path]::GetFullPath($RepositoryRoot)
$outputDirectoryPath = [System.IO.Path]::GetFullPath($OutputDirectory)

function Assert-SafeOutputDirectory {
    $root = [System.IO.Path]::GetPathRoot($outputDirectoryPath)
    if ([string]::IsNullOrWhiteSpace($outputDirectoryPath) -or
        $outputDirectoryPath -eq $root -or
        $outputDirectoryPath -eq $repositoryRootPath -or
        [System.IO.Path]::GetFileName($outputDirectoryPath) -ne 'rabbitmq cfs e2e') {
        throw "Unsafe CFS output directory '$outputDirectoryPath'."
    }
}

function Set-AzurePipelinesVariable([string] $Name, [string] $Value) {
    Write-Host "##vso[task.setvariable variable=$Name]$Value"
}

function Replace-ExactlyOnce([string] $Content, [string] $OldValue, [string] $NewValue, [string] $Description) {
    $matchCount = ([regex]::Matches($Content, [regex]::Escape($OldValue))).Count
    if ($matchCount -ne 1) {
        throw "Expected one $Description in the Dockerfile, but found $matchCount."
    }

    return $Content.Replace($OldValue, $NewValue)
}

function ConvertTo-YamlSingleQuotedString([string] $Value) {
    return "'$($Value.Replace("'", "''"))'"
}

function Write-Utf8File([string] $Path, [string] $Content) {
    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function New-NuGetConfig([string] $Path, [string] $AccessToken) {
    $settings = [System.Xml.XmlWriterSettings]::new()
    $settings.Indent = $true
    $settings.Encoding = [System.Text.UTF8Encoding]::new($false)

    $writer = [System.Xml.XmlWriter]::Create($Path, $settings)
    try {
        $writer.WriteStartDocument()
        $writer.WriteStartElement('configuration')

        $writer.WriteStartElement('packageSources')
        $writer.WriteElementString('clear', '')
        $writer.WriteStartElement('add')
        $writer.WriteAttributeString('key', 'upstream-public')
        $writer.WriteAttributeString('value', $nugetRegistry)
        $writer.WriteEndElement()
        $writer.WriteStartElement('add')
        $writer.WriteAttributeString('key', 'Local')
        $writer.WriteAttributeString('value', '/workspace/temp')
        $writer.WriteEndElement()
        $writer.WriteEndElement()

        $writer.WriteStartElement('packageSourceMapping')
        $writer.WriteElementString('clear', '')
        $writer.WriteStartElement('packageSource')
        $writer.WriteAttributeString('key', 'upstream-public')
        $writer.WriteStartElement('package')
        $writer.WriteAttributeString('pattern', '*')
        $writer.WriteEndElement()
        $writer.WriteEndElement()
        $writer.WriteStartElement('packageSource')
        $writer.WriteAttributeString('key', 'Local')
        $writer.WriteStartElement('package')
        $writer.WriteAttributeString('pattern', 'Microsoft.Azure.WebJobs.Extensions.RabbitMQ')
        $writer.WriteEndElement()
        $writer.WriteEndElement()
        $writer.WriteEndElement()

        $writer.WriteStartElement('packageSourceCredentials')
        $writer.WriteStartElement('upstream-public')
        $writer.WriteStartElement('add')
        $writer.WriteAttributeString('key', 'Username')
        $writer.WriteAttributeString('value', 'AzureDevOps')
        $writer.WriteEndElement()
        $writer.WriteStartElement('add')
        $writer.WriteAttributeString('key', 'ClearTextPassword')
        $writer.WriteAttributeString('value', $AccessToken)
        $writer.WriteEndElement()
        $writer.WriteEndElement()
        $writer.WriteEndElement()

        $writer.WriteEndElement()
        $writer.WriteEndDocument()
    }
    finally {
        $writer.Dispose()
    }
}

function New-CfsDockerfile([string] $SourcePath, [string] $DestinationPath, [bool] $IsPython) {
    $content = (Get-Content -LiteralPath $SourcePath -Raw).Replace("`r`n", "`n")
    $functionApp = if ($IsPython) { 'python' } else { 'java' }
    $functionAppsPath = '/workspace/extension/WebJobs.Extensions.RabbitMQ.LangEndToEndTests/FunctionApps'
    $content = Replace-ExactlyOnce $content `
        "RUN mkdir /workspace`nCOPY . /workspace" `
        @"
RUN mkdir -p /workspace/temp $functionAppsPath
COPY temp /workspace/temp
COPY extension/WebJobs.Extensions.RabbitMQ.LangEndToEndTests/FunctionApps/$functionApp $functionAppsPath/$functionApp
"@ `
        'workspace copy'
    $content = Replace-ExactlyOnce $content `
        'RUN npm install -g azure-functions-core-tools@4 --unsafe-perm true' `
        @'
RUN --mount=type=secret,id=npmrc,target=/root/.npmrc,required=true \
    NPM_CONFIG_USERCONFIG=/root/.npmrc npm install -g azure-functions-core-tools@4 --unsafe-perm true
'@ `
        'Azure Functions Core Tools npm install'
    $content = Replace-ExactlyOnce $content `
        'RUN dotnet nuget add source /workspace/temp --name Local && dotnet nuget list source' `
        @'
RUN --mount=type=secret,id=nuget_config,target=/workspace/NuGet.config,required=true \
    dotnet nuget list source --configfile /workspace/NuGet.config
'@ `
        'local NuGet source setup'

    if ($IsPython) {
        $content = Replace-ExactlyOnce $content `
            'RUN python3 -m pip install --upgrade pip --break-system-packages' `
            @'
RUN --mount=type=secret,id=pip_index_url,target=/run/secrets/pip_index_url,required=true \
    PIP_INDEX_URL="$(cat /run/secrets/pip_index_url)" python3 -m pip install --upgrade pip --break-system-packages
'@ `
            'pip upgrade'
        $content = Replace-ExactlyOnce $content `
            'RUN pip3 install -r requirements.txt --break-system-packages' `
            @'
RUN --mount=type=secret,id=pip_index_url,target=/run/secrets/pip_index_url,required=true \
    PIP_INDEX_URL="$(cat /run/secrets/pip_index_url)" pip3 install -r requirements.txt --break-system-packages
'@ `
            'Python requirements install'
        $content = Replace-ExactlyOnce $content `
            'RUN dotnet build extensions.csproj -o bin' `
            @'
RUN --mount=type=secret,id=nuget_config,target=/workspace/NuGet.config,required=true \
    dotnet restore extensions.csproj --configfile /workspace/NuGet.config
RUN dotnet build extensions.csproj -o bin --no-restore
RUN rm -f /root/.nuget/NuGet/NuGet.Config
'@ `
            'Python extension build'
    }
    else {
        $content = Replace-ExactlyOnce $content `
            'RUN mvn clean package -DskipTests' `
            @'
RUN --mount=type=secret,id=nuget_config,target=/workspace/NuGet.config,required=true \
    mvn clean package -DskipTests
RUN rm -f /root/.nuget/NuGet/NuGet.Config
'@ `
            'Java function app build'
    }

    Write-Utf8File $DestinationPath $content
}

Assert-SafeOutputDirectory

switch ($Phase) {
    'Initialize' {
        if (Test-Path -LiteralPath $outputDirectoryPath) {
            Remove-Item -LiteralPath $outputDirectoryPath -Recurse -Force
        }

        New-Item -ItemType Directory -Path $outputDirectoryPath | Out-Null
        $npmConfigPath = Join-Path $outputDirectoryPath '.npmrc'
        Write-Utf8File $npmConfigPath @"
registry=$npmRegistry
@types:registry=$npmRegistry
"@

        Set-AzurePipelinesVariable 'cfsTempDirectory' $outputDirectoryPath
        Set-AzurePipelinesVariable 'cfsNpmConfig' $npmConfigPath
    }

    'Finalize' {
        $npmConfigPath = Join-Path $outputDirectoryPath '.npmrc'
        if (-not (Test-Path -LiteralPath $npmConfigPath)) {
            throw "The temporary npm configuration '$npmConfigPath' does not exist."
        }

        $npmConfig = Get-Content -LiteralPath $npmConfigPath -Raw
        if ($npmConfig -notmatch '(?m):(_authToken|_password)=') {
            throw 'npmAuthenticate@0 did not add credentials to the temporary npm configuration.'
        }

        $pipIndexUrl = $env:PIP_INDEX_URL
        if ([string]::IsNullOrWhiteSpace($pipIndexUrl)) {
            throw 'PipAuthenticate@1 did not set PIP_INDEX_URL.'
        }

        $pipIndexUri = [System.Uri] $pipIndexUrl
        if ($pipIndexUri.Host -ne 'pkgs.dev.azure.com' -or
            -not $pipIndexUri.AbsolutePath.Contains('/azfunc/public/_packaging/upstream-public/pypi/simple/')) {
            throw 'PIP_INDEX_URL does not target the azfunc/public/upstream-public feed.'
        }

        $nugetAccessToken = $env:VSS_NUGET_ACCESSTOKEN
        if ([string]::IsNullOrWhiteSpace($nugetAccessToken)) {
            throw 'NuGetAuthenticate@1 did not set VSS_NUGET_ACCESSTOKEN.'
        }

        $nugetConfigPath = Join-Path $outputDirectoryPath 'NuGet.config'
        $pipIndexPath = Join-Path $outputDirectoryPath 'pip-index-url'
        $javaDockerfilePath = Join-Path $outputDirectoryPath 'java.Dockerfile'
        $pythonDockerfilePath = Join-Path $outputDirectoryPath 'python.Dockerfile'
        $composeOverridePath = Join-Path $outputDirectoryPath 'docker-compose.cfs.yml'

        New-NuGetConfig $nugetConfigPath $nugetAccessToken
        Write-Utf8File $pipIndexPath $pipIndexUrl
        New-CfsDockerfile `
            (Join-Path $repositoryRootPath 'extension/WebJobs.Extensions.RabbitMQ.LangEndToEndTests/FunctionApps/java/Dockerfile') `
            $javaDockerfilePath `
            $false
        New-CfsDockerfile `
            (Join-Path $repositoryRootPath 'extension/WebJobs.Extensions.RabbitMQ.LangEndToEndTests/FunctionApps/python/Dockerfile') `
            $pythonDockerfilePath `
            $true

        $javaDockerfile = ConvertTo-YamlSingleQuotedString $javaDockerfilePath
        $pythonDockerfile = ConvertTo-YamlSingleQuotedString $pythonDockerfilePath
        $npmConfig = ConvertTo-YamlSingleQuotedString $npmConfigPath
        $nugetConfig = ConvertTo-YamlSingleQuotedString $nugetConfigPath
        $pipIndex = ConvertTo-YamlSingleQuotedString $pipIndexPath
        Write-Utf8File $composeOverridePath @"
services:
  java-app:
    build:
      dockerfile: $javaDockerfile
      secrets:
        - npmrc
        - nuget_config
  python-app:
    build:
      dockerfile: $pythonDockerfile
      secrets:
        - npmrc
        - nuget_config
        - pip_index_url

secrets:
  npmrc:
    file: $npmConfig
  nuget_config:
    file: $nugetConfig
  pip_index_url:
    file: $pipIndex
"@

        Set-AzurePipelinesVariable 'cfsDockerComposeOverride' $composeOverridePath
    }

    'Cleanup' {
        if (Test-Path -LiteralPath $outputDirectoryPath) {
            Remove-Item -LiteralPath $outputDirectoryPath -Recurse -Force
        }
    }
}
