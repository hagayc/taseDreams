namespace TaseDreams.Api.Services;

public interface IOCPService
{
    Task<bool> CreateNamespaceAsync(string projectName, string developmentTeam);
    Task<bool> GrantAdminPermissionsAsync(string projectName, string developmentTeam);
    Task<bool> VerifyTektonBuildAsync(string projectName);
    Task<int> AllocateNodePortAsync(string projectName, string serviceName);
}

