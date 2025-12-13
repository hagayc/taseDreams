FROM atlassian/bitbucket:9.4.9

USER root

# Install curl if not already present (needed for plugin downloads)
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Create plugins directories
RUN mkdir -p /opt/atlassian/bitbucket/app/WEB-INF/classes/atlassian-bundled-plugins && \
    mkdir -p /var/atlassian/application-data/bitbucket/shared/plugins/installed-plugins

# Copy plugins directory (if plugins are provided locally)
# This allows plugins to be bundled with the image
# Note: If plugins directory is empty, this will create an empty directory (which is fine)
COPY plugins/ /opt/atlassian/bitbucket/app/WEB-INF/classes/atlassian-bundled-plugins/

# Copy and set up entrypoint script
COPY install-plugins.sh /usr/local/bin/install-plugins.sh
RUN chmod +x /usr/local/bin/install-plugins.sh

# Note: We stay as root for the entrypoint script to handle plugin installation
# The script will switch to the appropriate user when calling the original entrypoint

# Use custom entrypoint that installs plugins before starting Bitbucket
# The script will call the original entrypoint at the end
ENTRYPOINT ["/usr/local/bin/install-plugins.sh"]

