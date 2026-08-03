# The isolated CI agents cannot reach Docker Hub, NodeSource, Adoptium, or the
# Debian apt repos. The Microsoft Java dev container image is built on the
# Microsoft Build of OpenJDK 21 and bundles Maven and Node.js on Debian bookworm,
# so use it as the base and copy in only the .NET SDK (to build the extension).
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim AS dotnet

FROM mcr.microsoft.com/devcontainers/java:dev-21-bookworm

# The dev container image defaults to a non-root user; the build steps below
# write to system locations, so run them as root.
USER root

# Bring in the .NET 8 SDK
COPY --from=dotnet /usr/share/dotnet /usr/share/dotnet
RUN ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
ENV DOTNET_ROOT=/usr/share/dotnet

# Install Azure Functions Core Tools via npm (uses .NET 8)
RUN npm install -g azure-functions-core-tools@4 --unsafe-perm true

ARG WORK_DIR=/workspace/extension/WebJobs.Extensions.RabbitMQ.LangEndToEndTests/FunctionApps/java
ARG TARGET_DIR=${WORK_DIR}/target/azure-functions/RabbitMQE2E

# Copy entire workspace (including temp NuGet packages)
RUN mkdir /workspace
COPY . /workspace
WORKDIR ${WORK_DIR}

# Add local NuGet source for development packages
RUN dotnet nuget add source /workspace/temp --name Local && dotnet nuget list source

# Build the Java function app (this will also build extensions)
RUN mvn clean package -DskipTests

WORKDIR ${WORK_DIR}

ENTRYPOINT [ "mvn", "azure-functions:run" ]
