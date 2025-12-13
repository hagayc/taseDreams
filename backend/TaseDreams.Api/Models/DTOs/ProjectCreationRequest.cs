using TaseDreams.Api.Models;

namespace TaseDreams.Api.Models.DTOs;

public class ProjectCreationRequest
{
    public string ProjectName { get; set; } = string.Empty;
    public string DevelopmentTeam { get; set; } = string.Empty;
    public int ProjectLaneId { get; set; }
    public string BackendVersion { get; set; } = string.Empty;
    public string FrontendVersion { get; set; } = string.Empty;
    public List<int> InfrastructureServiceIds { get; set; } = new();
}

public class ProjectCreationResponse
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public ProjectStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ErrorDetails { get; set; }
}

