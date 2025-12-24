# LDAP Configuration Guide for Confluence and Bitbucket

## LDAP Server Information

- **Host:** `ldap` (from within containers) or `localhost` (from host)
- **Port:** `389` (LDAP) or `636` (LDAPS)
- **Base DN:** `dc=tasedreams,dc=local`
- **Bind DN:** `cn=admin,dc=tasedreams,dc=local`
- **Bind Password:** `admin`
- **User DN:** `ou=users,dc=tasedreams,dc=local`
- **Group DN:** `ou=groups,dc=tasedreams,dc=local`

## Available Users

1. **tomer**
   - Username: `tomer`
   - Password: `tomer123`
   - DN: `uid=tomer,ou=users,dc=tasedreams,dc=local`

2. **testuser**
   - Username: `testuser`
   - Password: `testpassword123`
   - DN: `uid=testuser,ou=users,dc=tasedreams,dc=local`

## Configure Bitbucket LDAP

### Steps:

1. **Access Bitbucket Admin Panel**
   - Go to: http://localhost:7990
   - Log in as admin
   - Navigate to: **Administration** → **User Directories**

2. **Add LDAP Directory**
   - Click **"Add Directory"**
   - Select **"LDAP"**

3. **Configure LDAP Settings**
   
   **Connection Settings:**
   - **Directory Type:** OpenLDAP
   - **Host:** `ldap`
   - **Port:** `389`
   - **Use SSL:** No (unchecked)
   - **Timeout:** `60` seconds

   **Authentication:**
   - **Bind DN:** `cn=admin,dc=tasedreams,dc=local`
   - **Bind Password:** `admin`

   **User Settings:**
   - **Base DN:** `ou=users,dc=tasedreams,dc=local`
   - **User Filter:** `(uid={0})`
   - **User DN:** `uid={0},ou=users,dc=tasedreams,dc=local`
   - **Username Attribute:** `uid`
   - **Display Name Attribute:** `cn`
   - **Email Attribute:** `mail`

   **Group Settings (Optional):**
   - **Base DN:** `ou=groups,dc=tasedreams,dc=local`
   - **Group Filter:** `(memberUid={0})`
   - **Group Name Attribute:** `cn`

4. **Test Connection**
   - Click **"Test Connection"** to verify connectivity
   - If successful, click **"Save"**

5. **Synchronize Users**
   - After saving, click **"Synchronize"** to import users from LDAP
   - Users `tomer` and `testuser` should appear in the user list

## Configure Confluence LDAP

### Steps:

1. **Access Confluence Admin Panel**
   - Go to: http://localhost:8090
   - Log in as admin (complete initial setup if needed)
   - Navigate to: **Administration** → **User Directories**

2. **Add LDAP Directory**
   - Click **"Add Directory"**
   - Select **"LDAP"** or **"Internal with LDAP Authentication"**

3. **Configure LDAP Settings**
   
   **Connection Settings:**
   - **Host:** `ldap`
   - **Port:** `389`
   - **Use SSL:** No (unchecked)
   - **Timeout:** `60` seconds

   **Authentication:**
   - **Bind DN:** `cn=admin,dc=tasedreams,dc=local`
   - **Bind Password:** `admin`

   **User Settings:**
   - **Base DN:** `ou=users,dc=tasedreams,dc=local`
   - **User Filter:** `(uid={0})`
   - **User DN:** `uid={0},ou=users,dc=tasedreams,dc=local`
   - **Username Attribute:** `uid`
   - **Display Name Attribute:** `cn`
   - **Email Attribute:** `mail`

   **Group Settings (Optional):**
   - **Base DN:** `ou=groups,dc=tasedreams,dc=local`
   - **Group Filter:** `(memberUid={0})`
   - **Group Name Attribute:** `cn`

4. **Test Connection**
   - Click **"Test Connection"** to verify connectivity
   - If successful, click **"Save"**

5. **Synchronize Users**
   - After saving, click **"Synchronize"** to import users from LDAP
   - Users `tomer` and `testuser` should appear in the user list

## Testing LDAP Connection

### From Host Machine:
```bash
ldapsearch -x -H ldap://localhost:389 \
  -D "cn=admin,dc=tasedreams,dc=local" \
  -w admin \
  -b "dc=tasedreams,dc=local" \
  "(uid=tomer)"
```

### From Bitbucket Container:
```bash
docker exec infrastructure-bitbucket ldapsearch -x \
  -H ldap://ldap:389 \
  -D "cn=admin,dc=tasedreams,dc=local" \
  -w admin \
  -b "ou=users,dc=tasedreams,dc=local" \
  "(uid=tomer)"
```

### From Confluence Container:
```bash
docker exec infrastructure-confluence ldapsearch -x \
  -H ldap://ldap:389 \
  -D "cn=admin,dc=tasedreams,dc=local" \
  -w admin \
  -b "ou=users,dc=tasedreams,dc=local" \
  "(uid=tomer)"
```

## Troubleshooting

### Connection Issues

1. **Verify LDAP is running:**
   ```bash
   docker-compose -f docker-compose.infrastructure.yaml ps ldap
   ```

2. **Test connectivity from containers:**
   ```bash
   docker exec infrastructure-bitbucket ping -c 2 ldap
   docker exec infrastructure-confluence ping -c 2 ldap
   ```

3. **Check LDAP logs:**
   ```bash
   docker logs infrastructure-ldap --tail=50
   ```

### Authentication Issues

1. **Verify user exists:**
   ```bash
   docker exec infrastructure-ldap ldapsearch -x \
     -H ldap://localhost \
     -D "cn=admin,dc=tasedreams,dc=local" \
     -w admin \
     -b "ou=users,dc=tasedreams,dc=local" \
     "(uid=tomer)"
   ```

2. **Test user authentication:**
   ```bash
   docker exec infrastructure-ldap ldapwhoami -x \
     -H ldap://localhost \
     -D "uid=tomer,ou=users,dc=tasedreams,dc=local" \
     -w tomer123
   ```

### Common Issues

- **"No such object" error:** Verify the Base DN is correct
- **"Invalid credentials" error:** Check Bind DN and password
- **"Connection timeout" error:** Verify network connectivity between containers
- **Users not syncing:** Check user filter syntax and ensure users exist in LDAP

## Quick Reference

| Setting | Value |
|---------|-------|
| LDAP Host | `ldap` (from containers) or `localhost` (from host) |
| LDAP Port | `389` |
| Base DN | `dc=tasedreams,dc=local` |
| Bind DN | `cn=admin,dc=tasedreams,dc=local` |
| Bind Password | `admin` |
| User DN | `ou=users,dc=tasedreams,dc=local` |
| User Filter | `(uid={0})` |
| Group DN | `ou=groups,dc=tasedreams,dc=local` |
| Group Filter | `(memberUid={0})` |

## Next Steps

After configuring LDAP:
1. Test login with user `tomer` (password: `tomer123`)
2. Verify user synchronization in both applications
3. Configure group mappings if needed
4. Set up user permissions and access control

