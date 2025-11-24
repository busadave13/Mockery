# Devcontainer Dockerfile for Mockery
# This image is prebuilt by GitHub Actions for faster Codespaces startup

FROM mcr.microsoft.com/devcontainers/dotnet:9.0

# Install additional tools
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends \
    git \
    curl \
    wget \
    vim \
    && apt-get clean -y && rm -rf /var/lib/apt/lists/*

# Install GitHub CLI
RUN curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg | dd of=/usr/share/keyrings/githubcli-archive-keyring.gpg \
    && chmod go+r /usr/share/keyrings/githubcli-archive-keyring.gpg \
    && echo "deb [arch=$(dpkg --print-architecture) signed-by=/usr/share/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" | tee /etc/apt/sources.list.d/github-cli.list > /dev/null \
    && apt-get update \
    && apt-get install gh -y \
    && apt-get clean -y && rm -rf /var/lib/apt/lists/*

# Install Docker (Docker-in-Docker will be handled by the feature)
RUN apt-get update && export DEBIAN_FRONTEND=noninteractive \
    && apt-get -y install --no-install-recommends docker.io docker-compose \
    && apt-get clean -y && rm -rf /var/lib/apt/lists/*

# Set up workspace
WORKDIR /workspaces/Mockery

# Copy project files for dependency restore
COPY NuGet.config .
COPY src/Mockery/Mockery.csproj ./src/Mockery/
COPY src/Mockery.Test/Mockery.Test.csproj ./src/Mockery.Test/

# Restore dependencies to cache them in the image
RUN dotnet restore src/Mockery/Mockery.csproj \
    && dotnet restore src/Mockery.Test/Mockery.Test.csproj

# Clean up project files (they'll be mounted from the actual repository)
RUN rm -rf src NuGet.config

# Set the default user
USER vscode
