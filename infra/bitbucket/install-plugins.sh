#!/bin/bash

# Script to install Bitbucket plugins (ScriptRunner and ControlFreak)
# This script runs before Bitbucket starts
# Note: Errors are handled gracefully - missing plugins won't prevent Bitbucket from starting

PLUGINS_DIR="/var/atlassian/application-data/bitbucket/shared/plugins/installed-plugins"
BUNDLED_PLUGINS_DIR="/opt/atlassian/bitbucket/app/WEB-INF/classes/atlassian-bundled-plugins"

# Create plugins directory if it doesn't exist
mkdir -p "$PLUGINS_DIR"

# Function to download plugin if URL is provided
download_plugin() {
    local plugin_name=$1
    local plugin_url=$2
    local plugin_file="$PLUGINS_DIR/${plugin_name}.jar"
    
    if [ -n "$plugin_url" ] && [ ! -f "$plugin_file" ]; then
        echo "Downloading $plugin_name from $plugin_url..."
        curl -L -f -o "$plugin_file" "$plugin_url" && echo "Successfully downloaded $plugin_name" || echo "Warning: Failed to download $plugin_name (will try bundled version)"
    fi
}

# Function to copy plugin from bundled directory if it exists
copy_bundled_plugin() {
    local plugin_name=$1
    local bundled_file="$BUNDLED_PLUGINS_DIR/${plugin_name}.jar"
    local plugin_file="$PLUGINS_DIR/${plugin_name}.jar"
    
    if [ -f "$bundled_file" ] && [ ! -f "$plugin_file" ]; then
        echo "Copying $plugin_name from bundled plugins..."
        cp "$bundled_file" "$plugin_file"
        echo "Successfully copied $plugin_name"
    elif [ ! -f "$plugin_file" ]; then
        echo "Warning: $plugin_name not found in bundled plugins directory"
    fi
}

# Install ScriptRunner plugin
echo "Installing ScriptRunner plugin..."
if [ -n "$SCRIPTRUNNER_PLUGIN_URL" ]; then
    download_plugin "scriptrunner" "$SCRIPTRUNNER_PLUGIN_URL"
fi
copy_bundled_plugin "scriptrunner"

# Install ControlFreak plugin
echo "Installing ControlFreak plugin..."
if [ -n "$CONTROLFREAK_PLUGIN_URL" ]; then
    download_plugin "controlfreak" "$CONTROLFREAK_PLUGIN_URL"
fi
copy_bundled_plugin "controlfreak"

# Set proper permissions
chown -R atlbitbucket:atlbitbucket "$PLUGINS_DIR" 2>/dev/null || true
chmod -R 755 "$PLUGINS_DIR" 2>/dev/null || true

echo "Plugin installation completed. Starting Bitbucket..."

# Execute the original Bitbucket entrypoint
# The Bitbucket image uses /usr/local/bin/entrypoint.sh as the default entrypoint
if [ -f /usr/local/bin/entrypoint.sh ]; then
    exec /usr/local/bin/entrypoint.sh "$@"
elif [ -f /entrypoint.sh ]; then
    exec /entrypoint.sh "$@"
else
    # Fallback: start Bitbucket directly using the start script
    exec /opt/atlassian/bitbucket/bin/start-bitbucket.sh -fg "$@"
fi

