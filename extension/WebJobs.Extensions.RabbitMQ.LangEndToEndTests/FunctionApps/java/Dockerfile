# Use .NET 8 SDK as base with Node.js for npm install of Core Tools
FROM mcr.microsoft.com/dotnet/sdk:8.0-bookworm-slim

# Install Node.js and Java 21 (Temurin)
RUN apt-get update && \
    apt-get install -y curl gnupg maven && \
    curl -fsSL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs && \
    # Add Adoptium repository for Temurin JDK
    curl -fsSL https://packages.adoptium.net/artifactory/api/gpg/key/public | gpg --dearmor -o /usr/share/keyrings/adoptium.gpg && \
    echo "deb [signed-by=/usr/share/keyrings/adoptium.gpg] https://packages.adoptium.net/artifactory/deb $(. /etc/os-release && echo $VERSION_CODENAME) main" | tee /etc/apt/sources.list.d/adoptium.list && \
    apt-get update && \
    apt-get install -y temurin-21-jdk && \
    rm -rf /var/lib/apt/lists/*

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

# Set environment
ENV JAVA_HOME=/usr/lib/jvm/temurin-21-jdk-amd64
ENV PATH="${JAVA_HOME}/bin:${PATH}"

WORKDIR ${WORK_DIR}

ENTRYPOINT [ "/usr/bin/mvn", "azure-functions:run" ]
