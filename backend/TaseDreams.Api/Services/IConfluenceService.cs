namespace TaseDreams.Api.Services;

public interface IConfluenceService
{
    Task<bool> CreateProjectPageAsync(string projectName, string projectKey, string developmentTeam, Dictionary<string, string> links);
}

