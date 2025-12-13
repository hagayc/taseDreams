using TaseDreams.Api.Models.DTOs;

namespace TaseDreams.Api.Services;

public interface IBitbucketService
{
    Task<bool> CreateProjectAsync(string projectName, string projectKey, string developmentTeam);
    Task<bool> CreateRepositoryAsync(string projectKey, string repoName, string repoType);
    Task<bool> InitializeRepositoryAsync(string projectKey, string repoName, string repoType, string backendLanguage, string frontendFramework);
    Task<bool> ConfigureWebhooksAsync(string projectKey, string cdRepoName);
    Task<bool> ConfigurePermissionsAsync(string projectKey, string developmentTeam, string cdRepoName);
    Task<bool> ConfigureBranchPoliciesAsync(string projectKey, string developmentTeam);
}

