namespace TaseDreams.Api.Models;

public class NodePortAllocation
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    public int Port { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public DateTime AllocatedAt { get; set; }
}

