#!/bin/bash
# Script to configure LDAP authentication in Confluence
# This script uses the Confluence REST API to configure LDAP directory

CONFLUENCE_URL="${CONFLUENCE_URL:-http://localhost:8090}"
CONFLUENCE_ADMIN_USER="${CONFLUENCE_ADMIN_USER:-admin}"
CONFLUENCE_ADMIN_PASS="${CONFLUENCE_ADMIN_PASS:-admin}"

LDAP_HOST="${LDAP_HOST:-ldap}"
LDAP_PORT="${LDAP_PORT:-389}"
LDAP_BASE_DN="${LDAP_BASE_DN:-dc=tasedreams,dc=local}"
LDAP_BIND_DN="${LDAP_BIND_DN:-cn=admin,dc=tasedreams,dc=local}"
LDAP_BIND_PASSWORD="${LDAP_BIND_PASSWORD:-admin}"
LDAP_USER_DN="${LDAP_USER_DN:-ou=users,dc=tasedreams,dc=local}"
LDAP_GROUP_DN="${LDAP_GROUP_DN:-ou=groups,dc=tasedreams,dc=local}"

echo "Configuring LDAP for Confluence..."
echo "Confluence URL: $CONFLUENCE_URL"
echo "LDAP Host: $LDAP_HOST:$LDAP_PORT"

# Wait for Confluence to be ready
echo "Waiting for Confluence to be ready..."
for i in {1..30}; do
    if curl -s -f "$CONFLUENCE_URL/status" > /dev/null 2>&1; then
        echo "Confluence is ready!"
        break
    fi
    echo "Waiting... ($i/30)"
    sleep 5
done

# Get authentication token
echo "Getting authentication token..."
AUTH_RESPONSE=$(curl -s -X POST "$CONFLUENCE_URL/rest/api/content?os_authType=basic" \
    -u "$CONFLUENCE_ADMIN_USER:$CONFLUENCE_ADMIN_PASS" \
    -H "Content-Type: application/json")

if [ $? -ne 0 ]; then
    echo "ERROR: Failed to authenticate with Confluence. Please ensure:"
    echo "1. Confluence is fully set up (not in FIRST_RUN state)"
    echo "2. Admin credentials are correct"
    echo "3. You can access Confluence at $CONFLUENCE_URL"
    exit 1
fi

echo ""
echo "=========================================="
echo "LDAP Configuration for Confluence"
echo "=========================================="
echo ""
echo "Please configure LDAP manually through the Confluence UI:"
echo ""
echo "1. Go to: $CONFLUENCE_URL"
echo "2. Log in as admin"
echo "3. Go to: Administration > User Directories"
echo "4. Click 'Add Directory' > 'LDAP'"
echo ""
echo "LDAP Settings:"
echo "  - Host: $LDAP_HOST"
echo "  - Port: $LDAP_PORT"
echo "  - Base DN: $LDAP_BASE_DN"
echo "  - Bind DN: $LDAP_BIND_DN"
echo "  - Bind Password: $LDAP_BIND_PASSWORD"
echo "  - User DN: $LDAP_USER_DN"
echo "  - User Filter: (uid={0})"
echo "  - Group DN: $LDAP_GROUP_DN"
echo "  - Group Filter: (memberUid={0})"
echo ""
echo "After configuration, users 'tomer' and 'testuser' can log in with:"
echo "  - Username: tomer (password: tomer123)"
echo "  - Username: testuser (password: testpassword123)"
echo ""

