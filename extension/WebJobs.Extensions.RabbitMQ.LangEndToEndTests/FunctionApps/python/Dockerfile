# The isolated CI agents cannot reach Docker Hub, NodeSource, or the Debian apt
# repos, so Node.js and the .NET SDK are copied in from MCR images instead of
# being installed with apt/NodeSource.

# Node.js (for the Core Tools npm install) from the MCR Docker Hub mirror
FROM mcr.microsoft.com/mirror/docker/library/node:20-bookworm-slim AS node

# .NET 8 SDK to build the extension bundle
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS dotnet

# Python 3.11 base already bundles Python + pip (from the MCR Docker Hub mirror)
FROM mcr.microsoft.com/mirror/docker/library/python:3.11-bookworm

# Bring in Node.js from the mirror stage
COPY --from=node /usr/local/bin/node /usr/local/bin/node
COPY --from=node /usr/local/lib/node_modules /usr/local/lib/node_modules
RUN ln -sf /usr/local/lib/node_modules/npm/bin/npm-cli.js /usr/local/bin/npm \
    && ln -sf /usr/local/lib/node_modules/npm/bin/npx-cli.js /usr/local/bin/npx

# Bring in the .NET 8 SDK
COPY --from=dotnet /usr/share/dotnet /usr/share/dotnet
RUN ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
ENV DOTNET_ROOT=/usr/share/dotnet

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
