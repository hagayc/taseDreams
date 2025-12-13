using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using TaseDreams.Api.Configuration;
using TaseDreams.Api.Data;
using TaseDreams.Api.Models;
using TaseDreams.Api.Models.DTOs;
using System.Text;

namespace TaseDreams.Api.Services;

public class ProjectCreationService : IProjectCreationService
{
    private readonly ApplicationDbContext _context;
    private readonly IBitbucketService _bitbucketService;
    private readonly IArtifactoryService _artifactoryService;
    private readonly IArgoCDService _argoCDService;
    private readonly IOCPService _ocpService;
    private readonly IConfluenceService _confluenceService;
    private readonly AppSettings _appSettings;

    public ProjectCreationService(
        ApplicationDbContext context,
        IBitbucketService bitbucketService,
        IArtifactoryService artifactoryService,
        IArgoCDService argoCDService,
        IOCPService ocpService,
        IConfluenceService confluenceService,
        IOptions<AppSettings> appSettings)
    {
        _context = context;
        _bitbucketService = bitbucketService;
        _artifactoryService = artifactoryService;
        _argoCDService = argoCDService;
        _ocpService = ocpService;
        _confluenceService = confluenceService;
        _appSettings = appSettings.Value;
    }

    public async Task<ProjectCreationResponse> CreateProjectAsync(ProjectCreationRequest request)
    {
        try
        {
            // Validate project name
            var projectName = request.ProjectName.ToLower();
            if (projectName.Length > _appSettings.MaxProjectNameLength)
            {
                return new ProjectCreationResponse
                {
                    ProjectName = projectName,
                    Status = ProjectStatus.Failed,
                    Message = $"Project name exceeds maximum length of {_appSettings.MaxProjectNameLength} characters"
                };
            }

            // Get project lane
            var projectLane = await _context.ProjectLanes.FindAsync(request.ProjectLaneId);
            if (projectLane == null)
            {
                return new ProjectCreationResponse
                {
                    ProjectName = projectName,
                    Status = ProjectStatus.Failed,
                    Message = "Invalid project lane selected"
                };
            }

            // Create project record
            var project = new Project
            {
                Name = projectName,
                BitbucketProjectKey = $"{_appSettings.ProjectPrefix}{projectName}".ToUpper(),
                DevelopmentTeam = request.DevelopmentTeam,
                ProjectLaneId = request.ProjectLaneId,
                BackendVersion = request.BackendVersion,
                FrontendVersion = request.FrontendVersion,
                Status = Models.ProjectStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            // Generate repository names
            var backendRepoName = CapitalizeFirst(projectName);
            var frontendRepoName = $"{CapitalizeFirst(projectName)}{_appSettings.FrontendRepoSuffix}";
            var cdRepoName = $"{_appSettings.ProjectPrefix}{projectName}{_appSettings.CDRepoSuffix}";

            // Step 1: Create Bitbucket project and repositories
            project.Status = Models.ProjectStatus.CreatingBitbucket;
            await _context.SaveChangesAsync();

            var bitbucketProjectKey = project.BitbucketProjectKey;
            if (!await _bitbucketService.CreateProjectAsync(projectName, bitbucketProjectKey, request.DevelopmentTeam))
            {
                project.Status = Models.ProjectStatus.Failed;
                project.ErrorMessage = "Failed to create Bitbucket project. Check backend logs for details.";
                await _context.SaveChangesAsync();
                return new ProjectCreationResponse
                {
                    ProjectId = project.Id,
                    ProjectName = projectName,
                    Status = ProjectStatus.Failed,
                    Message = "Failed to create Bitbucket project. Please check: 1) Bitbucket is running and accessible, 2) Bitbucket credentials are configured in environment variables, 3) Check backend logs for detailed error information."
                };
            }

            // Create repositories
            await _bitbucketService.CreateRepositoryAsync(bitbucketProjectKey, backendRepoName, "backend");
            await _bitbucketService.CreateRepositoryAsync(bitbucketProjectKey, frontendRepoName, "frontend");
            await _bitbucketService.CreateRepositoryAsync(bitbucketProjectKey, cdRepoName, "cd");

            // Initialize repositories with template content
            await _bitbucketService.InitializeRepositoryAsync(bitbucketProjectKey, backendRepoName, "backend", projectLane.BackendLanguage, projectLane.FrontendFramework);
            await _bitbucketService.InitializeRepositoryAsync(bitbucketProjectKey, frontendRepoName, "frontend", projectLane.BackendLanguage, projectLane.FrontendFramework);
            await _bitbucketService.InitializeRepositoryAsync(bitbucketProjectKey, cdRepoName, "cd", projectLane.BackendLanguage, projectLane.FrontendFramework);

            // Configure webhooks and permissions
            await _bitbucketService.ConfigureWebhooksAsync(bitbucketProjectKey, cdRepoName);
            await _bitbucketService.ConfigurePermissionsAsync(bitbucketProjectKey, request.DevelopmentTeam, cdRepoName);
            await _bitbucketService.ConfigureBranchPoliciesAsync(bitbucketProjectKey, request.DevelopmentTeam);

            // Step 2: Create Artifactory repositories
            project.Status = Models.ProjectStatus.CreatingArtifactory;
            await _context.SaveChangesAsync();

            if (!await _artifactoryService.CreateDockerRepositoryAsync(projectName, projectLane.BackendLanguage))
            {
                project.Status = Models.ProjectStatus.Failed;
                project.ErrorMessage = "Failed to create Artifactory repositories. Check backend logs for details.";
                await _context.SaveChangesAsync();
                return new ProjectCreationResponse
                {
                    ProjectId = project.Id,
                    ProjectName = projectName,
                    Status = ProjectStatus.Failed,
                    Message = "Failed to create Artifactory repositories. Please check: 1) Artifactory is running and accessible, 2) Artifactory credentials are configured in environment variables, 3) Check backend logs for detailed error information.",
                    ErrorDetails = "Check backend logs for detailed Artifactory API error messages."
                };
            }

            await _artifactoryService.SetupReplicationAsync(projectName);
            await _artifactoryService.ConfigurePermissionsAsync(projectName, request.DevelopmentTeam);

            // Step 3: Configure ArgoCD
            project.Status = Models.ProjectStatus.ConfiguringArgoCD;
            await _context.SaveChangesAsync();

            if (!await _argoCDService.UpdateArgoCDConfigAsync(projectName, bitbucketProjectKey, projectLane.BackendLanguage, projectLane.FrontendFramework))
            {
                project.Status = Models.ProjectStatus.Failed;
                project.ErrorMessage = "Failed to configure ArgoCD";
                await _context.SaveChangesAsync();
                return new ProjectCreationResponse
                {
                    ProjectId = project.Id,
                    ProjectName = projectName,
                    Status = ProjectStatus.Failed,
                    Message = "Failed to configure ArgoCD"
                };
            }

            // Step 4: Configure OCP
            project.Status = Models.ProjectStatus.ConfiguringOCP;
            await _context.SaveChangesAsync();

            await _ocpService.CreateNamespaceAsync(projectName, request.DevelopmentTeam);
            await _ocpService.GrantAdminPermissionsAsync(projectName, request.DevelopmentTeam);

            // Allocate NodePorts if needed
            var nodePorts = new List<NodePortAllocation>();
            if (request.InfrastructureServiceIds.Any())
            {
                foreach (var serviceId in request.InfrastructureServiceIds)
                {
                    var infraService = await _context.InfrastructureServices.FindAsync(serviceId);
                    if (infraService != null)
                    {
                        var port = await _ocpService.AllocateNodePortAsync(projectName, infraService.Name);
                        if (port > 0)
                        {
                            nodePorts.Add(new NodePortAllocation
                            {
                                ProjectId = project.Id,
                                Port = port,
                                ServiceName = infraService.Name,
                                AllocatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
                _context.NodePortAllocations.AddRange(nodePorts);
            }

            // Verify Tekton build
            await _ocpService.VerifyTektonBuildAsync(projectName);

            // Step 5: Create Confluence page
            project.Status = Models.ProjectStatus.CreatingConfluence;
            await _context.SaveChangesAsync();

            var links = new Dictionary<string, string>
            {
                { "OCP Project Home", $"https://ocp-cluster/console/project/oc-{projectName}" },
                { "ArgoCD Project Home", $"https://argocd-server/applications/{projectName}" }
            };

            // Add infrastructure service links
            foreach (var serviceId in request.InfrastructureServiceIds)
            {
                var infraService = await _context.InfrastructureServices.FindAsync(serviceId);
                if (infraService != null)
                {
                    var nodePort = nodePorts.FirstOrDefault(np => np.ServiceName == infraService.Name);
                    if (nodePort != null)
                    {
                        links.Add($"{infraService.Name} Management", $"http://ocp-cluster:{nodePort.Port}");
                    }
                }
            }

            await _confluenceService.CreateProjectPageAsync(projectName, bitbucketProjectKey, request.DevelopmentTeam, links);

            // Complete
            project.Status = Models.ProjectStatus.Completed;
            project.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new ProjectCreationResponse
            {
                ProjectId = project.Id,
                ProjectName = projectName,
                Status = ProjectStatus.Completed,
                Message = "Project created successfully"
            };
        }
        catch (DbUpdateException dbEx)
        {
            // Extract the root cause from Entity Framework exceptions
            var rootCause = dbEx.InnerException?.Message ?? dbEx.Message;
            var detailedError = dbEx.InnerException?.ToString() ?? dbEx.ToString();
            
            return new ProjectCreationResponse
            {
                ProjectName = request.ProjectName,
                Status = ProjectStatus.Failed,
                Message = $"Database Error: {rootCause}",
                ErrorDetails = detailedError
            };
        }
        catch (Exception ex)
        {
            // Extract inner exception details for better error messages
            var rootCause = ex.InnerException?.Message ?? ex.Message;
            var detailedError = ex.InnerException?.ToString() ?? ex.ToString();
            
            return new ProjectCreationResponse
            {
                ProjectName = request.ProjectName,
                Status = ProjectStatus.Failed,
                Message = $"Error: {rootCause}",
                ErrorDetails = detailedError
            };
        }
    }

    private string CapitalizeFirst(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
        return char.ToUpper(input[0]) + input.Substring(1);
    }
}

