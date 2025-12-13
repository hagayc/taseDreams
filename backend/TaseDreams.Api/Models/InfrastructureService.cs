namespace TaseDreams.Api.Models;

public class InfrastructureService
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class ProjectInfrastructureService
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public int InfrastructureServiceId { get; set; }
    public InfrastructureService InfrastructureService { get; set; } = null!;
}

