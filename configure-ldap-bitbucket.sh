#!/bin/bash
# Script to configure LDAP authentication in Bitbucket
# This script provides instructions and can test LDAP connectivity

BITBUCKET_URL="${BITBUCKET_URL:-http://localhost:7990}"
BITBUCKET_ADMIN_USER="${BITBUCKET_ADMIN_USER:-admin}"
BITBUCKET_ADMIN_PASS="${BITBUCKET_ADMIN_PASS:-admin}"

LDAP_HOST="${LDAP_HOST:-ldap}"
LDAP_PORT="${LDAP_PORT:-389}"
LDAP_BASE_DN="${LDAP_BASE_DN:-dc=tasedreams,dc=local}"
LDAP_BIND_DN="${LDAP_BIND_DN:-cn=admin,dc=tasedreams,dc=local}"
LDAP_BIND_PASSWORD="${LDAP_BIND_PASSWORD:-admin}"
LDAP_USER_DN="${LDAP_USER_DN:-ou=users,dc=tasedreams,dc=local}"
LDAP_GROUP_DN="${LDAP_GROUP_DN:-ou=groups,dc=tasedreams,dc=local}"

echo "Configuring LDAP for Bitbucket..."
echo "Bitbucket URL: $BITBUCKET_URL"
echo "LDAP Host: $LDAP_HOST:$LDAP_PORT"

# Wait for Bitbucket to be ready
echo "Waiting for Bitbucket to be ready..."
for i in {1..30}; do
    STATUS=$(curl -s "$BITBUCKET_URL/status" 2>/dev/null | grep -o '"state":"[^"]*"' | cut -d'"' -f4)
    if [ "$STATUS" = "RUNNING" ]; then
        echo "Bitbucket is ready!"
        break
    fi
    echo "Waiting... ($i/30) - Status: $STATUS"
    sleep 5
done

# Test LDAP connectivity from Bitbucket container
echo ""
echo "Testing LDAP connectivity..."
docker exec infrastructure-bitbucket bash -c "command -v ldapsearch > /dev/null 2>&1 || (apt-get update && apt-get install -y ldap-utils)" 2>/dev/null

if docker exec infrastructure-bitbucket ldapsearch -x -H "ldap://$LDAP_HOST:$LDAP_PORT" -D "$LDAP_BIND_DN" -w "$LDAP_BIND_PASSWORD" -b "$LDAP_BASE_DN" -s base "(objectclass=*)" dn > /dev/null 2>&1; then
    echo "✓ LDAP connectivity test passed!"
else
    echo "✗ LDAP connectivity test failed. Please check network connectivity."
fi

echo ""
echo "=========================================="
echo "LDAP Configuration for Bitbucket"
echo "=========================================="
echo ""
echo "Please configure LDAP manually through the Bitbucket UI:"
echo ""
echo "1. Go to: $BITBUCKET_URL"
echo "2. Log in as admin"
echo "3. Go to: Administration > Authentication > User Directories"
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

