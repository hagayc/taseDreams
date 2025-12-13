namespace TaseDreams.Api.Models;

public class ProjectLane
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string BackendLanguage { get; set; } = string.Empty;
    public string FrontendFramework { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

