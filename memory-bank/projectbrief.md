# Project Brief: Mockery

## Overview
Mockery is a mock API service that serves static mock responses from files. It's designed to help developers test applications by providing configurable mock endpoints without requiring a full backend implementation.

## Core Purpose
- Serve mock API responses from file-based storage
- Support multiple response formats (JSON, HTML, etc.)
- Enable custom status codes and headers per response
- Provide both local file and Git repository-based mock storage

## Key Features
- **File-based mock responses**: Responses are stored as files with naming conventions for status codes and headers
- **Git repository integration**: Can pull mocks from a Git repository for centralized mock management
- **Rate limiting**: Built-in rate limiting middleware
- **OpenTelemetry integration**: Observability support for monitoring and tracing
- **Docker/Kubernetes ready**: Includes Dockerfile and Helm charts for container deployment

## Technology Stack
- **Language**: C# 9.0+
- **Framework**: ASP.NET Core 9.0+
- **Runtime**: .NET 9.0+
- **Testing**: xUnit, Moq, FluentAssertions
- **Containerization**: Docker
- **Orchestration**: Kubernetes with Helm charts

## Repository
- **URL**: https://github.com/busadave13/Mockery.git
- **Latest Commit**: 2859f48af5a2c775d89465d4b08bc24a4891d8ad
