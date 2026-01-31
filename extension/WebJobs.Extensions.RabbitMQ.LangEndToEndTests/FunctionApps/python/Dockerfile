# Use .NET 8 SDK as base with Node.js for npm install of Core Tools
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim

# Install Node.js and Python 3.10
RUN apt-get update && \
    apt-get install -y curl gnupg python3 python3-pip python3-venv && \
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs && \
    rm -rf /var/lib/apt/lists/*

# Install Azure Functions Core Tools via npm (uses .NET 8)
RUN npm install -g azure-functions-core-tools@4 --unsafe-perm true

ARG WORK_DIR=/workspace/extension/WebJobs.Extensions.RabbitMQ.LangEndToEndTests/FunctionApps/python

# Copy entire workspace (including temp NuGet packages)
RUN mkdir /workspace
COPY . /workspace
WORKDIR ${WORK_DIR}

# Add local NuGet source for development packages
RUN dotnet nuget add source /workspace/temp --name Local && dotnet nuget list source

# Install Python dependencies
RUN python3 -m pip install --upgrade pip --break-system-packages
RUN pip3 install -r requirements.txt --break-system-packages

# Build extensions from extensions.csproj (uses local NuGet source)
RUN dotnet build extensions.csproj -o bin

# Set environment
ENV AzureWebJobsScriptRoot=${WORK_DIR}
ENV FUNCTIONS_WORKER_RUNTIME=python

EXPOSE 7071

ENTRYPOINT [ "func", "host", "start", "--python" ]
