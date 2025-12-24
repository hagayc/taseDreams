# Artifactory LDAP Configuration Guide

## Current Status

Artifactory is configured and starting up. The "Database connection check failed" message is **normal** during initial startup - Artifactory uses an embedded Derby database that will be created automatically.

**Note:** Artifactory can take 3-5 minutes to fully initialize on first startup.

## Access Artifactory

Once Artifactory is running:
- **URL:** http://localhost:8081
- **Default Admin:** `admin` / `admin` (or as configured in environment variables)

## Configure LDAP in Artifactory

### Step 1: Access Artifactory Admin

1. Navigate to: http://localhost:8081
2. Log in with admin credentials
3. Go to: **Administration** → **User Management** → **Security** → **LDAP Settings**

### Step 2: Add LDAP Server

1. Click **"New"** to add a new LDAP server
2. Fill in the following settings:

**General Settings:**
- **LDAP URL:** `ldap://ldap:389`
- **Manager DN:** `cn=admin,dc=tasedreams,dc=local`
- **Manager Password:** `admin`

**User Settings:**
- **User DN Pattern:** `uid={0},ou=users,dc=tasedreams,dc=local`
- **User Base DN:** `ou=users,dc=tasedreams,dc=local`
- **User Filter:** `(uid={0})`
- **User ID Attribute:** `uid`
- **Email Attribute:** `mail`
- **LDAP Attribute for Username:** `uid`

**Group Settings (Optional):**
- **Group Base DN:** `ou=groups,dc=tasedreams,dc=local`
- **Group Filter:** `(memberUid={0})`
- **Group Name Attribute:** `cn`

### Step 3: Test and Save

1. Click **"Test LDAP Connection"** to verify connectivity
2. Click **"Test LDAP User"** with username `tomer` to verify user lookup
3. If tests pass, click **"Save"**

### Step 4: Enable LDAP Authentication

1. Go to: **Administration** → **User Management** → **Security** → **Authentication Settings**
2. Enable **"LDAP Authentication"**
3. Select your LDAP server from the dropdown
4. Save the configuration

## LDAP Configuration Details

### Connection Information
- **LDAP Host:** `ldap` (from within Docker network)
- **LDAP Port:** `389`
- **Base DN:** `dc=tasedreams,dc=local`
- **Bind DN:** `cn=admin,dc=tasedreams,dc=local`
- **Bind Password:** `admin`

### Available Users
- **tomer** - Password: `tomer123`
- **testuser** - Password: `testpassword123`

### Available Groups
- **developers** - Contains both `tomer` and `testuser`

## Troubleshooting

### Artifactory Not Starting

If Artifactory keeps restarting:
1. Check logs: `docker-compose -f docker-compose.infrastructure.yaml logs artifactory`
2. Ensure sufficient disk space
3. Wait 3-5 minutes for initial database creation
4. Check if port 8081 is available

### LDAP Connection Issues

1. **Verify LDAP is accessible:**
   ```bash
   docker exec infrastructure-artifactory ping -c 2 ldap
   ```

2. **Test LDAP from Artifactory container:**
   ```bash
   docker exec infrastructure-artifactory bash -c "apt-get update && apt-get install -y ldap-utils && ldapsearch -x -H ldap://ldap:389 -D 'cn=admin,dc=tasedreams,dc=local' -w admin -b 'dc=tasedreams,dc=local' -s base '(objectclass=*)' dn"
   ```

3. **Check LDAP logs:**
   ```bash
   docker logs infrastructure-ldap --tail=50
   ```

### Common Issues

- **"LDAP URL not accessible"** - Ensure Artifactory and LDAP are on the same Docker network (`infrastructure-network`)
- **"Invalid credentials"** - Verify Bind DN and password are correct
- **"User not found"** - Check User DN Pattern and User Filter settings

## Quick Reference

| Setting | Value |
|---------|-------|
| LDAP URL | `ldap://ldap:389` |
| Manager DN | `cn=admin,dc=tasedreams,dc=local` |
| Manager Password | `admin` |
| User Base DN | `ou=users,dc=tasedreams,dc=local` |
| User DN Pattern | `uid={0},ou=users,dc=tasedreams,dc=local` |
| User Filter | `(uid={0})` |
| Group Base DN | `ou=groups,dc=tasedreams,dc=local` |
| Group Filter | `(memberUid={0})` |

## Next Steps

After configuring LDAP:
1. Test login with user `tomer` (password: `tomer123`)
2. Verify user permissions and groups
3. Configure repository permissions if needed
4. Set up build integration if required

