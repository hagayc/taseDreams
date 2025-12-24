#!/bin/bash
# Script to configure LDAP in Artifactory via REST API

ARTIFACTORY_URL="${ARTIFACTORY_URL:-http://localhost:8081/artifactory}"
ADMIN_USER="${ARTIFACTORY_ADMIN_USER:-admin}"
ADMIN_PASS="${ARTIFACTORY_ADMIN_PASSWORD:-Pass1234}"

LDAP_URL="${LDAP_URL:-ldap://infrastructure-ldap:389}"
LDAP_MANAGER_DN="${LDAP_MANAGER_DN:-cn=admin,dc=tasedreams,dc=local}"
LDAP_MANAGER_PASSWORD="${LDAP_MANAGER_PASSWORD:-admin}"

echo "Configuring LDAP in Artifactory..."
echo "Artifactory URL: $ARTIFACTORY_URL"
echo "LDAP URL: $LDAP_URL"

# Wait for Artifactory to be ready
echo "Waiting for Artifactory to be ready..."
for i in {1..60}; do
    RESPONSE=$(curl -s -w "%{http_code}" -u "$ADMIN_USER:$ADMIN_PASS" "$ARTIFACTORY_URL/api/system/ping" 2>&1)
    HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
    if [ "$HTTP_CODE" = "200" ]; then
        echo "Artifactory is ready!"
        sleep 5  # Give it a moment to fully initialize
        break
    fi
    echo "Waiting... ($i/60) - HTTP $HTTP_CODE"
    sleep 10
done

# Get authentication token
echo "Getting authentication token..."
TOKEN=$(curl -s -X POST -u "$ADMIN_USER:$ADMIN_PASS" \
    "$ARTIFACTORY_URL/api/security/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    -d "username=$ADMIN_USER" \
    -d "scope=member-of-groups:administrators" \
    -d "expires_in=3600" | grep -o '"access_token":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
    echo "Failed to get authentication token. Using basic auth instead."
    AUTH_HEADER="-u $ADMIN_USER:$ADMIN_PASS"
else
    echo "Token obtained successfully"
    AUTH_HEADER="-H \"Authorization: Bearer $TOKEN\""
fi

# LDAP Configuration JSON
LDAP_CONFIG=$(cat <<EOF
{
  "key": "ldap-tasedreams",
  "enabled": true,
  "ldapUrl": "$LDAP_URL",
  "managerDn": "$LDAP_MANAGER_DN",
  "managerPassword": "$LDAP_MANAGER_PASSWORD",
  "userDnPattern": "uid={0},ou=users,dc=tasedreams,dc=local",
  "userBaseDn": "ou=users,dc=tasedreams,dc=local",
  "userFilter": "(uid={0})",
  "userSubTree": true,
  "emailAttribute": "mail",
  "ldapAttributeForUsername": "uid",
  "groupBaseDn": "ou=groups,dc=tasedreams,dc=local",
  "groupFilter": "(memberUid={0})",
  "groupNameAttribute": "cn",
  "groupMemberAttribute": "memberUid",
  "groupSubTree": true
}
EOF
)

echo "Creating LDAP configuration..."
RESPONSE=$(curl -s -w "\n%{http_code}" -X PUT \
    -u "$ADMIN_USER:$ADMIN_PASS" \
    -H "Content-Type: application/json" \
    "$ARTIFACTORY_URL/api/security/ldap/ldap-tasedreams" \
    -d "$LDAP_CONFIG")

HTTP_CODE=$(echo "$RESPONSE" | tail -n1)
BODY=$(echo "$RESPONSE" | sed '$d')

if [ "$HTTP_CODE" = "200" ] || [ "$HTTP_CODE" = "201" ]; then
    echo "✓ LDAP configuration created successfully!"
    echo "Response: $BODY"
else
    echo "✗ Failed to create LDAP configuration. HTTP Code: $HTTP_CODE"
    echo "Response: $BODY"
    exit 1
fi

# Test LDAP connection
echo ""
echo "Testing LDAP connection..."
TEST_RESPONSE=$(curl -s -w "\n%{http_code}" -X POST \
    -u "$ADMIN_USER:$ADMIN_PASS" \
    -H "Content-Type: application/json" \
    "$ARTIFACTORY_URL/api/security/ldap/ldap-tasedreams/test" \
    -d '{"testLdapUser": "tomer"}')

TEST_HTTP_CODE=$(echo "$TEST_RESPONSE" | tail -n1)
TEST_BODY=$(echo "$TEST_RESPONSE" | sed '$d')

if [ "$TEST_HTTP_CODE" = "200" ]; then
    echo "✓ LDAP connection test successful!"
    echo "Response: $TEST_BODY"
else
    echo "⚠ LDAP connection test returned HTTP $TEST_HTTP_CODE"
    echo "Response: $TEST_BODY"
fi

echo ""
echo "LDAP configuration complete!"
echo "Next steps:"
echo "1. Go to Administration → User Management → Security → Authentication Settings"
echo "2. Enable LDAP Authentication"
echo "3. Select 'ldap-tasedreams' from the dropdown"
echo "4. Save the configuration"
echo ""
echo "Test users:"
echo "- tomer / tomer123"
echo "- testuser / testpassword123"

