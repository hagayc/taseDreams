namespace TaseDreams.Api.Services;

public interface IArtifactoryService
{
    Task<bool> CreateDockerRepositoryAsync(string projectName, string backendLanguage);
    Task<bool> SetupReplicationAsync(string projectName);
    Task<bool> ConfigurePermissionsAsync(string projectName, string developmentTeam);
}

