# Bitbucket Server with ScriptRunner and ControlFreak Plugins

This directory contains the Dockerfile and installation scripts for Bitbucket Server with ScriptRunner and ControlFreak plugins pre-installed.

## Plugin Installation Methods

There are two ways to install the plugins:

### Method 1: Place Plugin JARs in `plugins/` Directory (Recommended)

1. Download the plugin JAR files:
   - **ScriptRunner for Bitbucket**: Download from [Atlassian Marketplace](https://marketplace.atlassian.com/apps/1212398/scriptrunner-for-bitbucket)
   - **ControlFreak for Bitbucket**: Download from [Atlassian Marketplace](https://marketplace.atlassian.com/apps/1212399/controlfreak-for-bitbucket)

2. Place the JAR files in the `plugins/` directory:
   ```
   infra/bitbucket/plugins/
   ├── scriptrunner.jar
   └── controlfreak.jar
   ```

3. The Dockerfile will automatically copy these files during the build.

### Method 2: Use Environment Variables (Download URLs)

Set environment variables in your `.env` file or `docker-compose.infrastructure.yaml`:

```env
SCRIPTRUNNER_PLUGIN_URL=https://example.com/path/to/scriptrunner.jar
CONTROLFREAK_PLUGIN_URL=https://example.com/path/to/controlfreak.jar
```

The install script will download the plugins from these URLs on container startup.

## Building the Image

The image will be built automatically when you run:

```bash
docker-compose -f docker-compose.infrastructure.yaml up -d --build
```

Or build manually:

```bash
docker build -t bitbucket-with-plugins ./infra/bitbucket
```

## Plugin Location

Once installed, plugins are located at:
```
/var/atlassian/application-data/bitbucket/shared/plugins/installed-plugins/
```

## Notes

- **Licensing**: Both ScriptRunner and ControlFreak are commercial plugins that require valid licenses.
- **Plugin Versions**: Ensure you download plugin versions compatible with your Bitbucket Server version (check the image tag in Dockerfile).
- **First Startup**: Plugins will be installed on the first container startup. Bitbucket will need to be restarted for plugins to be activated.
- **Manual Installation**: You can also install plugins manually through the Bitbucket UI after the container is running.

## Troubleshooting

If plugins don't appear in Bitbucket:

1. Check the container logs:
   ```bash
   docker-compose -f docker-compose.infrastructure.yaml logs bitbucket
   ```

2. Verify plugins are in the correct directory:
   ```bash
   docker exec infrastructure-bitbucket ls -la /var/atlassian/application-data/bitbucket/shared/plugins/installed-plugins/
   ```

3. Check Bitbucket plugin management UI at: `http://localhost:7990/plugins/servlet/upm`

