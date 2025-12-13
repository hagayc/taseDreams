# TaseDreams - Project Onboarding System

## Project Location

**C:\TaseDreams**

## Project Structure

```
C:\TaseDreams\
├── backend/                          # .NET Core 8.0 API Service
│   ├── TaseDreams.Api/
│   └── Dockerfile
├── frontend/                         # Angular 19 Frontend Service
│   ├── src/
│   └── Dockerfile
├── docker-compose.yaml               # TaseDreams application only
├── docker-compose.infrastructure.yaml # Infrastructure simulation services
├── .env                              # Environment variables
├── .gitignore                        # Git ignore rules
└── README.md                         # This file
```

## Quick Start

### 1. TaseDreams Application

Run only the TaseDreams app (Frontend, Backend, Database):

```bash
docker-compose up -d --build
```

This starts:
- **MSSQL Database** (port 1433)
- **Backend API** (port 5000)
- **Frontend** (port 4200)

### 2. Infrastructure Simulation (Optional)

Run the infrastructure services separately:

```bash
docker-compose -f docker-compose.infrastructure.yaml up -d
```

This starts:
- **Bitbucket Server** (port 7990)
- **Confluence** (port 8090)
- **Artifactory** (port 8081)
- **LDAP Server** (port 389)
- **LDAP Admin UI** (port 8080)

**Memory Requirements**: ~12-16 GB RAM for infrastructure services

### 3. Running Both Together

You can run both stacks simultaneously:

```bash
# Start infrastructure first
docker-compose -f docker-compose.infrastructure.yaml up -d

# Then start the app
docker-compose up -d --build
```

## Environment Variables

All configuration is in the `.env` file. See `.env.example` for template.

### Application Configuration
- Database settings
- Application settings (project name length, prefixes, etc.)
- Port configurations

### Infrastructure Integration
- Bitbucket Server URL and credentials
- JFrog Artifactory URLs and credentials
- ArgoCD repository URL and credentials
- OpenShift cluster API URL and token
- Confluence URL and credentials
- LDAP/Active Directory server details

## Access Points

### Application
- **Frontend**: http://localhost:4200
- **Backend API**: http://localhost:5000
- **Swagger UI**: http://localhost:5000/swagger
- **Database**: localhost:1433

### Infrastructure Services (when running)
- **Bitbucket**: http://localhost:7990
- **Confluence**: http://localhost:8090
- **Artifactory**: http://localhost:8081
- **LDAP Admin**: http://localhost:8080

## Features

- Automated project creation workflow
- Bitbucket Server integration
- JFrog Artifactory integration
- ArgoCD configuration management
- OCP cluster integration
- Confluence documentation automation
- LDAP/Active Directory SSO authentication
- Extensible project lanes and infrastructure services

## Services Architecture

### Application Services (docker-compose.yaml)
- **backend/** - .NET Core 8.0 API
- **frontend/** - Angular 19 application
- **mssql** - MSSQL Server 2019 database

### Infrastructure Services (docker-compose.infrastructure.yaml)
- **bitbucket** - Bitbucket Server 9.4.9
- **confluence** - Confluence Server
- **artifactory** - JFrog Artifactory
- **ldap** - OpenLDAP server
- **ldap-admin** - phpLDAPadmin UI

### Note on OCP, ArgoCD, and Tekton

These services typically run on Kubernetes/OCP itself, not in docker-compose:

- **Red Hat OCP**: Use CodeReady Containers (CRC), Minikube, or Kind
- **ArgoCD**: Deploy on your Kubernetes/OCP cluster
- **Tekton**: Deploy on your Kubernetes/OCP cluster

See `DOCKER_COMPOSE_MEMORY_REQUIREMENTS.md` for details on setting up OCP.

## Development Workflow

1. **Start infrastructure** (if needed):
   ```bash
   docker-compose -f docker-compose.infrastructure.yaml up -d
   ```

2. **Update .env** with infrastructure service URLs and credentials

3. **Start application**:
   ```bash
   docker-compose up -d --build
   ```

4. **Access the application** at http://localhost:4200

## Stopping Services

```bash
# Stop application
docker-compose down

# Stop infrastructure
docker-compose -f docker-compose.infrastructure.yaml down

# Stop both
docker-compose down && docker-compose -f docker-compose.infrastructure.yaml down
```

## Troubleshooting

- Check logs: `docker-compose logs [service-name]`
- Check status: `docker-compose ps`
- Restart a service: `docker-compose restart [service-name]`
- View resource usage: `docker stats`

## Next Steps

1. Update `.env` with your credentials
2. Start infrastructure services (if simulating full environment)
3. Start the application
4. Begin creating projects!

