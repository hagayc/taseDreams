namespace TaseDreams.Api.Services;

public interface IArgoCDService
{
    Task<bool> UpdateArgoCDConfigAsync(string projectName, string projectKey, string backendLanguage, string frontendFramework);
}

