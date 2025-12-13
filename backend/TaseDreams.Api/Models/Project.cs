namespace TaseDreams.Api.Models;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BitbucketProjectKey { get; set; } = string.Empty;
    public string DevelopmentTeam { get; set; } = string.Empty;
    public int ProjectLaneId { get; set; }
    public ProjectLane ProjectLane { get; set; } = null!;
    public string BackendVersion { get; set; } = string.Empty;
    public string FrontendVersion { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
    public List<ProjectInfrastructureService> InfrastructureServices { get; set; } = new();
    public List<NodePortAllocation> NodePorts { get; set; } = new();
}

public enum ProjectStatus
{
    Pending = 0,
    CreatingBitbucket = 1,
    CreatingArtifactory = 2,
    ConfiguringArgoCD = 3,
    ConfiguringOCP = 4,
    CreatingConfluence = 5,
    Completed = 6,
    Failed = 7
}

