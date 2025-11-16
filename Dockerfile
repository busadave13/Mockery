# Base image
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Install Git for LibGit2Sharp
RUN apt-get update && apt-get install -y git && rm -rf /var/lib/apt/lists/*

# Build image
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/Mockery/Mockery.csproj", "Mockery/"]
RUN dotnet restore "Mockery/Mockery.csproj"
COPY src/Mockery/. Mockery/
WORKDIR "/src/Mockery"
RUN dotnet build "Mockery.csproj" -c Release -o /app/build

# Publish image
FROM build AS publish
RUN dotnet publish "Mockery.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create directory for Git repository clone
RUN mkdir -p /app/mocks

ENTRYPOINT ["dotnet", "Mockery.dll"]
