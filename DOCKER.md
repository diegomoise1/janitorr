# Docker Build and Deployment Guide

This guide explains how to build and deploy Janitorr using Docker, including integration with Portainer.

## Building the Image

### Option 1: Standard JVM Image (Recommended)

Build the standard Docker image:
```bash
docker build -t janitorr:latest .
```

### Option 2: Native Image (Smaller, Faster Startup)

Build the native image (takes longer to build but smaller runtime):
```bash
docker build -f Dockerfile.native -t janitorr:native .
```

## Running with Docker

### Quick Start
```bash
# Create config directory
mkdir -p ./config ./logs

# Run the container
docker run -d \
  --name janitorr \
  -p 8978:8978 \
  -v ./config:/config:ro \
  -v ./logs:/logs \
  -v /path/to/your/media:/data/media:ro \
  janitorr:latest
```

### Using Docker Compose

1. Copy the provided `docker-compose.yml`
2. Adjust the volume paths to match your setup
3. Run:
```bash
docker-compose up -d
```

## Portainer Integration

### Method 1: Git Repository Deployment

1. In Portainer, go to **App Templates** or **Stacks**
2. Create a new stack
3. Choose **Repository** as source
4. Enter this repository URL: `https://github.com/Schaka/janitorr`
5. Set compose file path: `docker-compose.yml`
6. Deploy

### Method 2: Custom App Template

1. In Portainer, go to **App Templates**
2. Click **Add Template**
3. Configure:
   - **Title**: Janitorr
   - **Repository URL**: `https://github.com/Schaka/janitorr`
   - **Compose file path**: `docker-compose.yml`
   - **Platform**: linux

### Method 3: Direct Stack Deployment

1. Copy the contents of `docker-compose.yml`
2. In Portainer, go to **Stacks** → **Add Stack**
3. Paste the compose content
4. Adjust volume paths in the editor
5. Deploy

## Configuration

### Environment Variables

- `TZ`: Timezone (default: UTC)
- `JAVA_OPTS`: JVM options (JVM image only)
- `SPRING_CONFIG_ADDITIONAL_LOCATION`: Additional config file locations

### Volume Mounts

- `/config`: Configuration files (read-only recommended)
- `/logs`: Application logs
- `/data/media`: Your media library (read-only for safety)
- `/data/media/leaving-soon`: Directory for "leaving soon" collections

### Configuration File

Create `config/application.yml` with your settings:

```yaml
server:
  port: 8978

application:
  dry-run: true  # Set to false when ready
  # ... other settings
```

## Health Checks

The container includes health checks that verify the application is running:
- **Endpoint**: `http://localhost:8978/health`
- **Interval**: 30 seconds
- **Timeout**: 5 seconds
- **Start period**: 40 seconds (20s for native)

## Resource Requirements

### JVM Image
- **Minimum RAM**: 256MB
- **Recommended RAM**: 1GB
- **CPU**: 1 core minimum

### Native Image
- **Minimum RAM**: 128MB
- **Recommended RAM**: 512MB
- **CPU**: 1 core minimum

## Troubleshooting

### Build Issues

1. **Out of memory during build**:
   ```bash
   docker build --memory=4g -t janitorr:latest .
   ```

2. **Gradle daemon issues**:
   The Dockerfile uses `--no-daemon` to avoid issues in containers

### Runtime Issues

1. **Permission errors**:
   - Ensure volume directories are readable by UID 1000
   - Use `:ro` for read-only mounts where appropriate

2. **Health check failures**:
   - Check if port 8978 is accessible
   - Verify the application started successfully in logs

3. **Configuration not found**:
   - Ensure `config/application.yml` exists
   - Check volume mount paths

### Logs

View application logs:
```bash
docker logs janitorr
```

Or check the mounted logs directory:
```bash
tail -f ./logs/janitorr.log
```

## Security Considerations

1. **Run as non-root**: Both Dockerfiles create and use a non-root user
2. **Read-only media**: Mount media directories as read-only (`:ro`)
3. **Network security**: Only expose necessary ports
4. **Resource limits**: Set memory and CPU limits in production

## Performance Tips

1. **Use volume mounts**: Avoid copying large media directories
2. **Enable JVM optimizations**: The default `JAVA_OPTS` are optimized for containers
3. **Consider native image**: For lower memory usage and faster startup
4. **Monitor resources**: Use Portainer's monitoring features

## Integration with *arr Stack

Example docker-compose.yml section for integration:

```yaml
version: '3.8'

services:
  janitorr:
    build: .
    # ... janitorr config
    
  sonarr:
    image: linuxserver/sonarr:latest
    # ... sonarr config
    
  radarr:
    image: linuxserver/radarr:latest
    # ... radarr config
    
  jellyfin:
    image: jellyfin/jellyfin:latest
    # ... jellyfin config

networks:
  default:
    name: media_stack
```
