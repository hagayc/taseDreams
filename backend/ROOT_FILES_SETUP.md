# Root Level Files Setup

Please create these files in the **root directory** of the project:

## 1. docker-compose.yaml

Create `docker-compose.yaml` in the root directory with the following content:

```yaml
version: '3.8'

services:
  mssql:
    image: mcr.microsoft.com/mssql/server:2019-latest
    container_name: tasedreams-mssql
    environment:
      - ACCEPT_EULA=Y
      - SA_PASSWORD=${MSSQL_SA_PASSWORD:-TaseDreams2024!}
      - MSSQL_PID=Developer
    ports:
      - "${MSSQL_PORT:-1433}:1433"
    volumes:
      - mssql_data:/var/opt/mssql
    networks:
      - tasedreams-network
    healthcheck:
      test: /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P ${MSSQL_SA_PASSWORD:-TaseDreams2024!} -Q "SELECT 1" || exit 1
      interval: 10s
      timeout: 3s
      retries: 10

  backend:
    build:
      context: ./backend
      dockerfile: Dockerfile
    container_name: tasedreams-backend
    environment:
      - ASPNETCORE_ENVIRONMENT=${ASPNETCORE_ENVIRONMENT:-Development}
      - ConnectionStrings__DefaultConnection=Server=mssql,1433;Database=TaseDreams;User Id=sa;Password=${MSSQL_SA_PASSWORD:-TaseDreams2024!};TrustServerCertificate=True;
      - AppSettings__MaxProjectNameLength=${MAX_PROJECT_NAME_LENGTH:-12}
      - AppSettings__ProjectPrefix=${PROJECT_PREFIX:-oc-}
      - AppSettings__CDRepoSuffix=${CD_REPO_SUFFIX:--cd}
      - AppSettings__FrontendRepoSuffix=${FRONTEND_REPO_SUFFIX:--Fe}
      - Bitbucket__BaseUrl=${BITBUCKET_BASE_URL}
      - Bitbucket__Username=${BITBUCKET_USERNAME}
      - Bitbucket__Password=${BITBUCKET_PASSWORD}
      - Artifactory__DevUrl=${ARTIFACTORY_DEV_URL}
      - Artifactory__PrdUrl=${ARTIFACTORY_PRD_URL}
      - Artifactory__Username=${ARTIFACTORY_USERNAME}
      - Artifactory__Password=${ARTIFACTORY_PASSWORD}
      - ArgoCD__ConfigRepoUrl=${ARGOCD_CONFIG_REPO_URL}
      - ArgoCD__ConfigRepoBranch=${ARGOCD_CONFIG_REPO_BRANCH:-main}
      - ArgoCD__Username=${ARGOCD_USERNAME}
      - ArgoCD__Password=${ARGOCD_PASSWORD}
      - OCP__ApiUrl=${OCP_API_URL}
      - OCP__Token=${OCP_TOKEN}
      - Confluence__BaseUrl=${CONFLUENCE_BASE_URL}
      - Confluence__Username=${CONFLUENCE_USERNAME}
      - Confluence__Password=${CONFLUENCE_PASSWORD}
      - Confluence__SpaceKey=${CONFLUENCE_SPACE_KEY}
      - LDAP__Server=${LDAP_SERVER}
      - LDAP__Port=${LDAP_PORT:-389}
      - LDAP__UseSSL=${LDAP_USE_SSL:-false}
      - LDAP__BaseDN=${LDAP_BASE_DN}
      - LDAP__BindDN=${LDAP_BIND_DN}
      - LDAP__BindPassword=${LDAP_BIND_PASSWORD}
      - LDAP__UserFilter=${LDAP_USER_FILTER:-(sAMAccountName={0})}
      - LDAP__GroupFilter=${LDAP_GROUP_FILTER:-(member={0})}
    ports:
      - "${BACKEND_PORT:-5000}:8080"
    depends_on:
      mssql:
        condition: service_healthy
    networks:
      - tasedreams-network
    volumes:
      - ./backend:/app
      - /app/bin
      - /app/obj

  frontend:
    build:
      context: ./frontend
      dockerfile: Dockerfile
    container_name: tasedreams-frontend
    environment:
      - API_URL=${API_URL:-http://backend:8080}
    ports:
      - "${FRONTEND_PORT:-4200}:80"
    depends_on:
      - backend
    networks:
      - tasedreams-network
    volumes:
      - ./frontend:/app
      - /app/node_modules

volumes:
  mssql_data:

networks:
  tasedreams-network:
    driver: bridge
```

## 2. .env.example

Create `.env.example` in the root directory:

```env
# Database Configuration
MSSQL_SA_PASSWORD=TaseDreams2024!
MSSQL_PORT=1433

# Application Settings
ASPNETCORE_ENVIRONMENT=Development
MAX_PROJECT_NAME_LENGTH=12
PROJECT_PREFIX=oc-
CD_REPO_SUFFIX=-cd
FRONTEND_REPO_SUFFIX=-Fe

# Port Configuration
BACKEND_PORT=5000
FRONTEND_PORT=4200
API_URL=http://localhost:5000

# Bitbucket Server Configuration
BITBUCKET_BASE_URL=http://bitbucket-server:7990
BITBUCKET_USERNAME=your-bitbucket-username
BITBUCKET_PASSWORD=your-bitbucket-password

# JFrog Artifactory Configuration
ARTIFACTORY_DEV_URL=http://artifactory-dev:8081/artifactory
ARTIFACTORY_PRD_URL=http://artifactory-prd:8081/artifactory
ARTIFACTORY_USERNAME=your-artifactory-username
ARTIFACTORY_PASSWORD=your-artifactory-password

# ArgoCD Configuration
ARGOCD_CONFIG_REPO_URL=https://bitbucket-server/scm/devops/argocd-config.git
ARGOCD_CONFIG_REPO_BRANCH=main
ARGOCD_USERNAME=your-argocd-username
ARGOCD_PASSWORD=your-argocd-password

# OpenShift Container Platform Configuration
OCP_API_URL=https://ocp-cluster.example.com:6443
OCP_TOKEN=your-openshift-token

# Confluence Configuration
CONFLUENCE_BASE_URL=http://confluence-server:8090
CONFLUENCE_USERNAME=your-confluence-username
CONFLUENCE_PASSWORD=your-confluence-password
CONFLUENCE_SPACE_KEY=YOUR_SPACE_KEY

# LDAP/Active Directory Configuration for SSO
LDAP_SERVER=ldap://ldap-server.example.com
LDAP_PORT=389
LDAP_USE_SSL=false
LDAP_BASE_DN=DC=example,DC=com
LDAP_BIND_DN=CN=ServiceAccount,OU=ServiceAccounts,DC=example,DC=com
LDAP_BIND_PASSWORD=your-ldap-bind-password
LDAP_USER_FILTER=(sAMAccountName={0})
LDAP_GROUP_FILTER=(member={0})
```

## 3. .env

Copy `.env.example` to `.env` and update with your actual credentials.

## 4. .gitignore

Create `.gitignore` in the root directory:

```
# .NET
bin/
obj/
*.user
*.suo
*.cache
*.dll
*.exe
*.pdb
*.log

# Angular
node_modules/
dist/
.angular/
*.log

# Docker
.env
.env.local
.env.*.local

# IDE
.vs/
.vscode/
.idea/
*.swp
*.swo

# OS
.DS_Store
Thumbs.db

# Database
*.mdf
*.ldf

# Build outputs
**/bin/
**/obj/
**/dist/

# Secrets
*.key
*.pem
*.cert
```

## Usage

After creating these files:

1. Update `.env` with your actual credentials
2. Run: `docker-compose up -d --build`
3. Access:
   - Frontend: http://localhost:4200
   - Backend: http://localhost:5000
   - Swagger: http://localhost:5000/swagger

