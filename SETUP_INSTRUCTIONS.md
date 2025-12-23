# CI/CD Pipeline Setup Instructions

This document provides step-by-step instructions to set up the complete CI/CD pipeline for TaseDreams.

## Prerequisites

1. ✅ CRC (CodeReady Containers) running with 20GB memory
2. ✅ Red Hat OpenShift GitOps operator installed
3. ✅ Red Hat OpenShift Pipelines operator installed
4. GitHub repository for your source code
5. GitHub repository named `oc-dreams-cd` for CD configurations
6. DockerHub account

## Step 1: Configure DockerHub Credentials

1. Update `tekton-pipeline.yaml` with your DockerHub credentials:
   ```yaml
   stringData:
     username: YOUR_DOCKERHUB_USERNAME
     password: YOUR_DOCKERHUB_PASSWORD
   ```

2. Apply the secrets and service account:
   ```bash
   eval $(crc oc-env)
   oc apply -f tekton-pipeline.yaml
   ```

## Step 2: Configure GitHub Webhook Secret

1. Generate a secure random token for GitHub webhooks:
   ```bash
   openssl rand -hex 20
   ```

2. Update `tekton-trigger.yaml`:
   ```yaml
   stringData:
     secretToken: "YOUR_GENERATED_SECRET"
   ```

3. Update DockerHub username in trigger bindings:
   ```yaml
   - name: dockerhub-username
     value: "YOUR_DOCKERHUB_USERNAME"
   ```

4. Apply the trigger resources:
   ```bash
   oc apply -f tekton-trigger.yaml
   ```

## Step 3: Expose EventListener Service

Get the route for the EventListener:
```bash
oc expose service el-github-listener -n tasedreams-ci
oc get route el-github-listener -n tasedreams-ci
```

The output will show the webhook URL (e.g., `http://el-github-listener-tasedreams-ci.apps-crc.testing`)

## Step 4: Configure GitHub Webhook

1. Go to your GitHub repository
2. Navigate to Settings → Webhooks → Add webhook
3. Configure:
   - **Payload URL**: `http://el-github-listener-tasedreams-ci.apps-crc.testing`
   - **Content type**: `application/json`
   - **Secret**: The secret token you generated in Step 2
   - **Events**: Select "Just the push event" or "Let me select individual events" and choose:
     - Push
     - Pull requests

## Step 5: Create GitHub CD Repository Structure

Create a new GitHub repository named `oc-dreams-cd` with the following structure:

```
oc-dreams-cd/
├── app-of-apps/
│   └── app-of-apps.yaml
├── helm-charts/
│   ├── tasedreams-backend/
│   │   ├── Chart.yaml
│   │   ├── values.yaml
│   │   └── templates/
│   │       ├── deployment.yaml
│   │       ├── service.yaml
│   │       ├── serviceaccount.yaml
│   │       └── _helpers.tpl
│   └── tasedreams-frontend/
│       ├── Chart.yaml
│       ├── values.yaml
│       └── templates/
│           ├── deployment.yaml
│           ├── service.yaml
│           ├── ingress.yaml
│           ├── serviceaccount.yaml
│           └── _helpers.tpl
└── README.md
```

1. Copy the Helm charts:
   ```bash
   cp -r helm-charts/* /path/to/oc-dreams-cd/helm-charts/
   ```

2. Update `values.yaml` files in the CD repo with your DockerHub username:
   ```yaml
   image:
     repository: YOUR_DOCKERHUB_USERNAME/tasedreams-backend
   ```

3. Commit and push to the `oc-dreams-cd` repository

## Step 6: Configure ArgoCD

1. Get ArgoCD admin password:
   ```bash
   oc get secret openshift-gitops-cluster -n openshift-gitops -o jsonpath='{.data.admin\.password}' | base64 -d
   ```

2. Access ArgoCD UI:
   ```bash
   oc get route openshift-gitops-server -n openshift-gitops
   ```
   Open the URL in your browser and login with:
   - Username: `admin`
   - Password: (from step 1)

3. Add GitHub repository to ArgoCD:
   - Go to Settings → Repositories → Connect Repo
   - Type: `git`
   - Repository URL: `https://github.com/YOUR_GITHUB_USERNAME/oc-dreams-cd.git`
   - Authentication: Use your GitHub username and Personal Access Token

4. Update `argocd-app-of-apps.yaml` with your GitHub username:
   ```yaml
   repoURL: https://github.com/YOUR_GITHUB_USERNAME/oc-dreams-cd.git
   ```

5. Apply ArgoCD applications:
   ```bash
   oc apply -f argocd-app-of-apps.yaml
   ```

## Step 7: Test the Pipeline

1. Make a commit to your main GitHub repository:
   ```bash
   git add .
   git commit -m "Test CI/CD pipeline"
   git push
   ```

2. Check pipeline execution:
   ```bash
   oc get pipelineruns -n tasedreams-ci
   oc logs -f <pipelinerun-name> -n tasedreams-ci
   ```

3. Check ArgoCD applications:
   ```bash
   oc get applications -n openshift-gitops
   ```

## Troubleshooting

### Pipeline not triggering
- Check EventListener logs: `oc logs -f deployment/el-github-listener -n tasedreams-ci`
- Verify webhook URL is accessible
- Check GitHub webhook delivery logs

### Build failures
- Verify DockerHub credentials are correct
- Check image pull secrets are properly configured
- Review pipeline logs: `oc get taskruns -n tasedreams-ci`

### ArgoCD sync issues
- Verify repository access in ArgoCD
- Check application status: `oc describe application tasedreams-backend -n openshift-gitops`
- Review ArgoCD logs: `oc logs -f deployment/openshift-gitops-application-controller -n openshift-gitops`

## Next Steps

- Configure ingress for external access
- Set up database (MSSQL) in OpenShift
- Configure monitoring and logging
- Set up staging and production environments

