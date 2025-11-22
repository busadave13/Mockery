# Publishing Docker Images and Helm Charts

This document explains how the automated publishing process works for Mockery.

## Workflow Overview

The `publish-docker-helm.yml` workflow automatically publishes:
- Docker images to Docker Hub
- Helm charts to Docker Hub (as OCI artifacts)

## Trigger

The workflow runs automatically when:
- A Pull Request is **merged** to the `main` branch
- Manually triggered via workflow_dispatch

## Prerequisites

### GitHub Secrets Required

Set up the following secrets in your GitHub repository (Settings → Secrets and variables → Actions):

1. **DOCKERHUB_TOKEN** (Required)
   - Go to Docker Hub → Account Settings → Security → New Access Token
   - Create a token with Read & Write permissions
   - Add the token as a GitHub secret

2. **GITHUB_TOKEN** (Automatic)
   - Automatically provided by GitHub Actions
   - No setup required

## What Gets Published

### Docker Images

Three tags are created for each build:

1. **latest** - Always points to the most recent build
   ```bash
   docker pull davhar/mockery:latest
   ```

2. **Version tag** - Based on Chart.yaml version (e.g., `1.0.0`)
   ```bash
   docker pull davhar/mockery:1.0.0
   ```

3. **Commit SHA** - Specific git commit reference
   ```bash
   docker pull davhar/mockery:abc1234
   ```

### Helm Charts

Published as OCI artifacts to Docker Hub:

```bash
# Install from Docker Hub
helm install mockery oci://registry-1.docker.io/davhar/mockery --version 1.0.0

# Pull chart locally
helm pull oci://registry-1.docker.io/davhar/mockery --version 1.0.0
```

## Versioning

The version is automatically extracted from `charts/mockery/Chart.yaml`:

```yaml
version: 1.0.0  # This becomes the Docker and Helm chart version
```

To release a new version:
1. Update the `version` field in `charts/mockery/Chart.yaml`
2. Create a PR with your changes
3. Merge the PR
4. Workflow automatically publishes with the new version

## Release Notes

The workflow automatically creates a GitHub Release with:
- Docker pull commands
- Helm install commands
- PR title in the release notes

## Manual Trigger

You can manually trigger the workflow:

1. Go to: **Actions** → **Publish Docker Image and Helm Charts**
2. Click **Run workflow**
3. Select branch and click **Run workflow**

## Workflow Steps

1. ✅ Run tests to ensure quality
2. 🔨 Build Docker image
3. 📤 Push to Docker Hub (3 tags)
4. 📦 Package Helm chart
5. 📤 Push Helm chart to Docker Hub (OCI)
6. 🏷️ Create GitHub Release
7. 📋 Generate summary

## Using Published Artifacts

### Docker Image

```bash
# Pull and run latest
docker pull davhar/mockery:latest
docker run -p 8080:8080 davhar/mockery:latest

# Pull specific version
docker pull davhar/mockery:1.0.0
```

### Helm Chart

```bash
# Install from OCI registry
helm install mockery oci://registry-1.docker.io/davhar/mockery \
  --version 1.0.0 \
  --namespace dev \
  --create-namespace

# Upgrade existing installation
helm upgrade mockery oci://registry-1.docker.io/davhar/mockery \
  --version 1.0.0 \
  --namespace dev

# List available versions
helm search repo davhar/mockery --versions
```

## Troubleshooting

### Workflow Fails to Publish

1. **Check Docker Hub Token**
   - Ensure `DOCKERHUB_TOKEN` secret is set correctly
   - Token must have Read & Write permissions

2. **Version Already Exists**
   - If the version in Chart.yaml hasn't changed, it will overwrite
   - Update Chart.yaml version for new releases

3. **Tests Failing**
   - Workflow stops if tests fail
   - Fix tests before merging PR

### Can't Pull Helm Chart

```bash
# Login to Docker Hub OCI registry
helm registry login registry-1.docker.io -u davhar

# Pull chart
helm pull oci://registry-1.docker.io/davhar/mockery --version 1.0.0
```

## Best Practices

1. **Always update Chart.yaml version** when making changes
2. **Use semantic versioning** (MAJOR.MINOR.PATCH)
3. **Test locally** before creating PR
4. **Review workflow runs** in GitHub Actions tab
5. **Update release notes** if needed after auto-creation

## Additional Resources

- [Docker Hub - davhar/mockery](https://hub.docker.com/r/davhar/mockery)
- [Helm OCI Documentation](https://helm.sh/docs/topics/registries/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
