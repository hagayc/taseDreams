# Artifactory Pro 7.104.15

## Quick Start

### Accertifactory
- URL: http://localhost:8081/artifactory
- Default Username: `admin`
- Default Password: `password`

**⚠️ Change the password immediately after first login!**

**⚠️ LICENSE REQUIRED:** Artifactory Pro requires a valid license. You'll need to add your license after first login.

## Management Commands

```bash
# Start Artifactory
docker-compose start

# Stop Artifactory
docker-compose stop

# Restart Artifactory
docker-compose restart

# View logs
docker-compose logs -f artifactory

# Check status
docker-compose ps

# Complete shutdown (data persists)
docker-compose down

# Remove everything including data
docker-compose down -v
```

## Troubleshooting

If Artifactory doesn't start:
1. Check Docker resources (4GB RAM minimum recommended)
2. Check logs: `docker-compose logs artifactory`
3. Wait 2-3 minutes for initialization

## Data Location
All data is stored in Docker volumes and persists across restarts.
