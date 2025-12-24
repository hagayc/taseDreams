#!/usr/bin/env python3
"""
Automated LDAP Configuration Script for Confluence and Bitbucket
This script attempts to configure LDAP via REST APIs
"""

import requests
import json
import sys
import time
from requests.auth import HTTPBasicAuth

# Configuration
BITBUCKET_URL = "http://localhost:7990"
CONFLUENCE_URL = "http://localhost:8090"
ADMIN_USER = "admin"
ADMIN_PASS = "admin"

LDAP_CONFIG = {
    "host": "ldap",
    "port": 389,
    "base_dn": "dc=tasedreams,dc=local",
    "bind_dn": "cn=admin,dc=tasedreams,dc=local",
    "bind_password": "admin",
    "user_dn": "ou=users,dc=tasedreams,dc=local",
    "group_dn": "ou=groups,dc=tasedreams,dc=local",
    "user_filter": "(uid={0})",
    "group_filter": "(memberUid={0})"
}

def wait_for_service(url, service_name, max_attempts=30):
    """Wait for a service to be ready"""
    print(f"Waiting for {service_name} to be ready...")
    for i in range(max_attempts):
        try:
            response = requests.get(f"{url}/status", timeout=5)
            if response.status_code == 200:
                print(f"✓ {service_name} is ready!")
                return True
        except:
            pass
        print(f"  Attempt {i+1}/{max_attempts}...")
        time.sleep(5)
    return False

def configure_bitbucket_ldap():
    """Configure LDAP for Bitbucket"""
    print("\n" + "="*50)
    print("Configuring Bitbucket LDAP")
    print("="*50)
    
    if not wait_for_service(BITBUCKET_URL, "Bitbucket"):
        print("✗ Bitbucket is not ready. Please start it first.")
        return False
    
    auth = HTTPBasicAuth(ADMIN_USER, ADMIN_PASS)
    
    # Test authentication
    try:
        response = requests.get(
            f"{BITBUCKET_URL}/rest/api/1.0/admin/users",
            auth=auth,
            timeout=10
        )
        if response.status_code != 200:
            print(f"✗ Authentication failed. Status: {response.status_code}")
            return False
        print("✓ Authentication successful")
    except Exception as e:
        print(f"✗ Failed to connect to Bitbucket: {e}")
        return False
    
    print("\n⚠️  Bitbucket LDAP configuration must be done manually through the UI.")
    print("\nConfiguration details:")
    print(f"  Host: {LDAP_CONFIG['host']}")
    print(f"  Port: {LDAP_CONFIG['port']}")
    print(f"  Base DN: {LDAP_CONFIG['base_dn']}")
    print(f"  Bind DN: {LDAP_CONFIG['bind_dn']}")
    print(f"  Bind Password: {LDAP_CONFIG['bind_password']}")
    print(f"  User DN: {LDAP_CONFIG['user_dn']}")
    print(f"  User Filter: {LDAP_CONFIG['user_filter']}")
    print(f"\nGo to: {BITBUCKET_URL}/admin/users/directories")
    print("Click 'Add Directory' > 'LDAP' and enter the above settings")
    
    return True

def configure_confluence_ldap():
    """Configure LDAP for Confluence"""
    print("\n" + "="*50)
    print("Configuring Confluence LDAP")
    print("="*50)
    
    if not wait_for_service(CONFLUENCE_URL, "Confluence"):
        print("✗ Confluence is not ready. Please start it first.")
        return False
    
    auth = HTTPBasicAuth(ADMIN_USER, ADMIN_PASS)
    
    # Test authentication
    try:
        response = requests.get(
            f"{CONFLUENCE_URL}/rest/api/user/current",
            auth=auth,
            timeout=10
        )
        if response.status_code != 200:
            print(f"✗ Authentication failed. Status: {response.status_code}")
            print("  Note: Confluence may need initial setup first.")
            return False
        print("✓ Authentication successful")
    except Exception as e:
        print(f"✗ Failed to connect to Confluence: {e}")
        return False
    
    print("\n⚠️  Confluence LDAP configuration must be done manually through the UI.")
    print("\nConfiguration details:")
    print(f"  Host: {LDAP_CONFIG['host']}")
    print(f"  Port: {LDAP_CONFIG['port']}")
    print(f"  Base DN: {LDAP_CONFIG['base_dn']}")
    print(f"  Bind DN: {LDAP_CONFIG['bind_dn']}")
    print(f"  Bind Password: {LDAP_CONFIG['bind_password']}")
    print(f"  User DN: {LDAP_CONFIG['user_dn']}")
    print(f"  User Filter: {LDAP_CONFIG['user_filter']}")
    print(f"\nGo to: {CONFLUENCE_URL}/admin/users/directories.action")
    print("Click 'Add Directory' > 'LDAP' and enter the above settings")
    
    return True

def test_ldap_connectivity():
    """Test LDAP connectivity from containers"""
    print("\n" + "="*50)
    print("Testing LDAP Connectivity")
    print("="*50)
    
    import subprocess
    
    # Test from Bitbucket container
    print("\nTesting from Bitbucket container...")
    try:
        result = subprocess.run(
            ["docker", "exec", "infrastructure-bitbucket", "bash", "-c",
             "command -v ldapsearch > /dev/null 2>&1 || (apt-get update -qq && apt-get install -y -qq ldap-utils > /dev/null 2>&1)"],
            capture_output=True,
            timeout=60
        )
        
        result = subprocess.run(
            ["docker", "exec", "infrastructure-bitbucket", "ldapsearch",
             "-x", "-H", f"ldap://{LDAP_CONFIG['host']}:{LDAP_CONFIG['port']}",
             "-D", LDAP_CONFIG['bind_dn'],
             "-w", LDAP_CONFIG['bind_password'],
             "-b", LDAP_CONFIG['base_dn'],
             "-s", "base", "(objectclass=*)", "dn"],
            capture_output=True,
            timeout=10
        )
        
        if result.returncode == 0:
            print("✓ Bitbucket can connect to LDAP")
        else:
            print("✗ Bitbucket cannot connect to LDAP")
            print(f"  Error: {result.stderr.decode()}")
    except Exception as e:
        print(f"✗ Failed to test from Bitbucket: {e}")
    
    # Test from Confluence container
    print("\nTesting from Confluence container...")
    try:
        result = subprocess.run(
            ["docker", "exec", "infrastructure-confluence", "bash", "-c",
             "command -v ldapsearch > /dev/null 2>&1 || (apt-get update -qq && apt-get install -y -qq ldap-utils > /dev/null 2>&1)"],
            capture_output=True,
            timeout=60
        )
        
        result = subprocess.run(
            ["docker", "exec", "infrastructure-confluence", "ldapsearch",
             "-x", "-H", f"ldap://{LDAP_CONFIG['host']}:{LDAP_CONFIG['port']}",
             "-D", LDAP_CONFIG['bind_dn'],
             "-w", LDAP_CONFIG['bind_password'],
             "-b", LDAP_CONFIG['base_dn'],
             "-s", "base", "(objectclass=*)", "dn"],
            capture_output=True,
            timeout=10
        )
        
        if result.returncode == 0:
            print("✓ Confluence can connect to LDAP")
        else:
            print("✗ Confluence cannot connect to LDAP")
            print(f"  Error: {result.stderr.decode()}")
    except Exception as e:
        print(f"✗ Failed to test from Confluence: {e}")

def main():
    print("="*50)
    print("LDAP Configuration Script")
    print("="*50)
    
    test_ldap_connectivity()
    
    print("\n" + "="*50)
    print("Summary")
    print("="*50)
    print("\nLDAP Users available:")
    print("  - tomer (password: tomer123)")
    print("  - testuser (password: testpassword123)")
    print("\nLDAP Configuration:")
    print(f"  Server: {LDAP_CONFIG['host']}:{LDAP_CONFIG['port']}")
    print(f"  Base DN: {LDAP_CONFIG['base_dn']}")
    print(f"  Bind DN: {LDAP_CONFIG['bind_dn']}")
    print(f"  Bind Password: {LDAP_CONFIG['bind_password']}")
    
    configure_bitbucket_ldap()
    configure_confluence_ldap()
    
    print("\n" + "="*50)
    print("Configuration Complete")
    print("="*50)
    print("\nPlease complete the LDAP configuration through the web UIs:")
    print(f"  - Bitbucket: {BITBUCKET_URL}/admin/users/directories")
    print(f"  - Confluence: {CONFLUENCE_URL}/admin/users/directories.action")

if __name__ == "__main__":
    main()

