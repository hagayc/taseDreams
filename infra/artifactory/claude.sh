#!/bin/bash

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
INSTALL_DIR="$HOME/taseDreams/infra/artifactory"
COMPOSE_FILE="$INSTALL_DIR/docker-compose.yml"

echo -e "${BLUE}=================================${NC}"
echo -e "${BLUE}Artifactory Pro 7.104.15 Deployment${NC}"
echo -e "${BLUE}=================================${NC}\n"

# Step 1: Create directory structure
echo -e "${YELLOW}[1/5] Creating directory structure...${NC}"
mkdir -p "$INSTALL_DIR"
cd "$INSTALL_DIR"
echo -e "${GREEN}✓ Directory created: $INSTALL_DIR${NC}\n"

# Step 2: Create docker-compose.yml
echo -e "${YELLOW}[2/5] Creating docker-compose.yml...${NC}"
cat > "$COMPOSE_FILE" << 'EOF'
services:
  artifactory:
    image: releases-docker.jfrog.io/jfrog/artifactory-pro:7.104.15
    container_name: artifactory
    restart: unless-stopped
    ports:
      - "8081:8081"
      - "8082:8082"
    volumes:
      - artifactory_data:/var/opt/jfrog/artifactory
    environment:
      - JF_SHARED_DATABASE_TYPE=postgresql
      - JF_SHARED_DATABASE_USERNAME=artifactory
      - JF_SHARED_DATABASE_PASSWORD=password
      - JF_SHARED_DATABASE_URL=jdbc:postgresql://postgresql:5432/artifactory
      - JF_SHARED_DATABASE_DRIVER=org.postgresql.Driver
    depends_on:
      - postgresql
    ulimits:
      nproc: 65535
      nofile:
        soft: 32000
        hard: 40000

  postgresql:
    image: postgres:13
    container_name: postgresql
    restart: unless-stopped
    environment:
      - POSTGRES_DB=artifactory
      - POSTGRES_USER=artifactory
      - POSTGRES_PASSWORD=password
    volumes:
      - postgresql_data:/var/lib/postgresql/data
    ports:
      - "5432:5432"

volumes:
  artifactory_data:
  postgresql_data:
EOF
echo -e "${GREEN}✓ docker-compose.yml created${NC}\n"

# Step 3: Create README
echo -e "${YELLOW}[3/5] Creating README.md...${NC}"
cat > "$INSTALL_DIR/README.md" <<'EOF'
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
EOF
echo -e "${GREEN}✓ README.md created${NC}\n"

# Step 4: Pull images and startecho -e "${YELLOW}[4/5] Pulling Docker images and starting services...${NC}"
echo -e "${BLUE}This may take a few minutes...${NC}\n"

docker-compose pull
docker-compose up -d

echo -e "${GREEN}✓ Services started${NC}\n"

# Step 5: Wait and verify
echo -e "${YELLOW}[5/5] Waiting for Artifactory to initialize...${NC}"
echo -e "${BLUE}This typically takes 2-3 minutes. Monitoring logs...${NC}\n"

# Monitor logs for successful startup
timeout=300
elapsed=0
success=false

while [ $elapsed -lt $timeout ]; do
    if docker-compose logs artifactory 2>/dev/null | grep -q "Artifactory successfully started"; then
        success=true
        break
    fi
    
    echo -ne "${BLUE}Waiting... ${elapsed}s / ${timeout}s\r${NC}"
    sleep 5
    elapsed=$((elapsed + 5))
done

echo -e "\n"

if [ "$success" = true ]; then
    echo -e "${GREEN}=================================${NC}"
    echo -e "${GREEN}✓ DEPLOYMENT SUCCESSFUL!${NC}"
    echo -e "${GREEN}=================================${NC}\n"
    
    echo -e "${BLUE}Artiory is ready!${NC}\n"
    
    echo -e "${YELLOW}Access Information:${NC}"
    echo -e "  URL:      ${GREEN}http://localhost:8081/artifactory${NC}"
    echo -e "  Username: ${GREEN}admin${NC}"
    echo -e "  Password: ${GREEN}password${NC}\n"
    
    echo -e "${RED}⚠️  IMPORTANT: Change the admin password immediately!${NC}\n"
    
    echo -e "${YELLOW}Installation Directory:${NC}"
    echo -e "  ${GREEN}$INSTALL_DIR${NC}\n"
    
    echo -e "${YELLOW}Useful Commands:${NC}"
    echo -e "  cd $INSTALL_DIR"
    echo -e "  docker-compose ps              # Check status"
    echo -e "  docker-compose logs -f         # View logs"
    echo -e "  docker-compose stop            # Stop services"
    echo -e "  docker-compose start           # Start services"
    echo -e "  cat README.md                  # View full documentation\n"
else
    echo -e "${RED}=================================${NC}"
    echo -e "${RED}⚠ DEPLOYMENT TIMEOUT${NC}"
    echo -e "${RED}=================================${NC}\n"
    
    e "${YELLOW}Artifactory is still starting up.${NC}\n"
    
    echo -e "You can monitor the startup with:"
    echo -e "  ${GREEN}cd $INSTALL_DIR${NC}"
    echo -e "  ${GREEN}docker-compose logs -f artifactory${NC}\n"
    
    echo -e "Once you see 'Artifactory successfully started', access it at:"
    echo -e "  ${GREEN}http://localhost:8081/artifactory${NC}\n"
fi

# Create a quick access script
cat > "$INSTALL_DIR/manage.sh" <<'ENDSCRIPT'
#!/bin/bash

case "$1" in
    start)
        docker-compose start
        ;;
    stop)
        docker-compose stop
        ;;
    restart)
        docker-compose restart
        ;;
    logs)
        docker-compose logs -f artifactory
        ;;
    status)
        docker-compose ps
        ;;
    *)
        echo "Usage: $0 {start|stop|restart|logs|status}"
        exit 1
        ;;
esac
ENDSCRIPT

chmod +x "$INSTALL_DIR/manage.sh"

echo -e "${GREEN}✓ Quick management script created: $INSTALL_DIR/manage.sh${NC}\n"

echo -e "${BLUE}=================================${NC}"
ec -e "${BLUE}Deployment Complete!${NC}"
echo -e "${BLUE}=================================${NC}"
