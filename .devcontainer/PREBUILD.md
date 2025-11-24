# DevContainer Prebuild Setup

## Overview

This repository uses GitHub Actions to prebuild the devcontainer image, significantly reducing Codespaces startup time from 3-5 minutes to 30-60 seconds.

## How It Works

### 1. Dockerfile (`.devcontainer/Dockerfile`)
- Based on `mcr.microsoft.com/devcontainers/dotnet:9.0`
- Installs all development tools (Git, Docker, GitHub CLI)
- Pre-restores NuGet packages for faster development
- Creates a ready-to-use development environment

### 2. GitHub Actions Workflow (`.github/workflows/codespaces-prebuild.yml`)
Automatically builds and publishes the devcontainer image when:
- **On Push to Main**: When `.devcontainer/**` or `.csproj` files change
- **Weekly Schedule**: Every Sunday at 2 AM UTC (keeps dependencies fresh)
- **Manual Trigger**: Can be run manually via workflow dispatch

### 3. DevContainer Configuration (`.devcontainer/devcontainer.json`)
- References the Dockerfile for building
- Adds Docker-in-Docker feature
- Configures VS Code extensions and settings
- Sets up port forwarding (8080, 18888, etc.)

## Published Image

The prebuilt image is published to GitHub Container Registry:
```
ghcr.io/busadave13/mockery/devcontainer:latest
```

This image is automatically used when:
- Creating a new Codespace from the main branch
- Rebuilding an existing Codespace
- Opening the repository in VS Code with Dev Containers extension

## Benefits

### Speed
- **Cold start**: ~30-60 seconds (vs 3-5 minutes without prebuild)
- **Warm start**: ~10-20 seconds (if image is cached)

### Consistency
- All developers get the same environment
- Dependencies are pre-installed and versioned
- Reduces "works on my machine" issues

### Cost
- Less Codespaces build time = lower costs
- Prebuilds run on GitHub Actions (free for public repos)

## Maintenance

### Triggering a Manual Rebuild
1. Go to Actions tab in GitHub
2. Select "Codespaces Prebuild" workflow
3. Click "Run workflow" → "Run workflow"

### Updating the DevContainer
When you modify files in `.devcontainer/`:
1. Commit and push to a feature branch
2. Test the changes locally or in a Codespace
3. Merge to main - prebuild will run automatically
4. New Codespaces will use the updated image

### Monitoring
- Check workflow runs: `.github/workflows` → Actions tab
- View published images: Repository → Packages
- Build logs show dependency versions and build time

## Troubleshooting

### Prebuild Failed
- Check workflow logs in Actions tab
- Common issues: Dockerfile syntax, missing dependencies, network issues
- Fix and push - workflow will retry automatically

### Codespace Not Using Prebuild
- Check that you're on the main branch
- Wait for prebuild workflow to complete (5-10 minutes)
- Delete and recreate the Codespace
- Check package registry for the latest image

### Local Build Issues
If building locally with Dev Containers:
```bash
# Rebuild container without cache
Ctrl+Shift+P → "Dev Containers: Rebuild Container"
```

## Related Files

- `.devcontainer/devcontainer.json` - DevContainer configuration
- `.devcontainer/Dockerfile` - Container image definition
- `.github/workflows/codespaces-prebuild.yml` - Prebuild automation
- `.devcontainer/README.md` - General devcontainer documentation
