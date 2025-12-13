using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;
using TaseDreams.Api.Configuration;
using TaseDreams.Api.Models.DTOs;
using System.Text;
using System.Text.Json;

namespace TaseDreams.Api.Services;

public class BitbucketService : IBitbucketService
{
    private readonly BitbucketConfig _config;
    private readonly RestClient _client;
    private readonly ILogger<BitbucketService> _logger;

    public BitbucketService(IOptions<BitbucketConfig> config, ILogger<BitbucketService> logger)
    {
        _config = config.Value;
        _logger = logger;
        
        // If BaseUrl is empty or uses localhost, default to Docker service name
        var baseUrl = _config.BaseUrl;
        if (string.IsNullOrEmpty(baseUrl))
        {
            baseUrl = "http://bitbucket:7990";
            _logger.LogInformation("Bitbucket BaseUrl not configured, using default Docker service name: {BaseUrl}", baseUrl);
        }
        else if (baseUrl.Contains("localhost") || baseUrl.Contains("127.0.0.1"))
        {
            // Replace localhost with Docker service name for container-to-container communication
            baseUrl = baseUrl.Replace("localhost", "bitbucket").Replace("127.0.0.1", "bitbucket");
            _logger.LogInformation("Bitbucket BaseUrl contains localhost, replacing with Docker service name: {BaseUrl}", baseUrl);
        }
        
        _client = new RestClient(baseUrl);
    }

    public async Task<bool> CreateProjectAsync(string projectName, string projectKey, string developmentTeam)
    {
        try
        {
            if (string.IsNullOrEmpty(_config.Username) || string.IsNullOrEmpty(_config.Password))
            {
                _logger.LogError("Bitbucket credentials are not configured (Username or Password is empty)");
                return false;
            }

            var request = new RestRequest("/rest/api/1.0/projects", Method.Post);
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.Username}:{_config.Password}"));
            request.AddHeader("Authorization", $"Basic {auth}");
            request.AddHeader("Content-Type", "application/json");

            var body = new
            {
                key = projectKey,
                name = projectName,
                description = $"Project created by TaseDreams for {developmentTeam}"
            };

            request.AddJsonBody(body);

            _logger.LogInformation("Creating Bitbucket project: {ProjectKey} at {BaseUrl}", projectKey, _config.BaseUrl);
            var response = await _client.ExecuteAsync(request);
            
            if (!response.IsSuccessful)
            {
                _logger.LogError("Failed to create Bitbucket project. Status: {StatusCode}, Response: {ResponseContent}", 
                    response.StatusCode, response.Content);
                _logger.LogError("Error: {ErrorMessage}", response.ErrorMessage);
                return false;
            }

            _logger.LogInformation("Successfully created Bitbucket project: {ProjectKey}", projectKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while creating Bitbucket project: {ProjectKey}", projectKey);
            return false;
        }
    }

    public async Task<bool> CreateRepositoryAsync(string projectKey, string repoName, string repoType)
    {
        var request = new RestRequest($"/rest/api/1.0/projects/{projectKey}/repos", Method.Post);
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.Username}:{_config.Password}"));
        request.AddHeader("Authorization", $"Basic {auth}");
        request.AddHeader("Content-Type", "application/json");

        var body = new
        {
            name = repoName,
            scmId = "git",
            forkable = true
        };

        request.AddJsonBody(body);

        var response = await _client.ExecuteAsync(request);
        if (!response.IsSuccessful)
        {
            _logger.LogWarning("Bitbucket API call failed. Status: {StatusCode}, Response: {ResponseContent}", 
                response.StatusCode, response.Content);
        }
        return response.IsSuccessful;
    }

    public async Task<bool> InitializeRepositoryAsync(string projectKey, string repoName, string repoType, string backendLanguage, string frontendFramework)
    {
        // This would typically clone, add initial files, commit and push
        // For now, we'll create the repo structure via API
        // In production, you'd use git commands or Bitbucket's file API
        
        // Create main branch
        var branchRequest = new RestRequest($"/rest/api/1.0/projects/{projectKey}/repos/{repoName}/branches/default", Method.Put);
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.Username}:{_config.Password}"));
        branchRequest.AddHeader("Authorization", $"Basic {auth}");
        branchRequest.AddHeader("Content-Type", "application/json");

        var branchBody = new
        {
            id = "refs/heads/main",
            displayId = "main"
        };

        branchRequest.AddJsonBody(branchBody);
        var branchResponse = await _client.ExecuteAsync(branchRequest);

        return branchResponse.IsSuccessful;
    }

    public async Task<bool> ConfigureWebhooksAsync(string projectKey, string cdRepoName)
    {
        var request = new RestRequest($"/rest/api/1.0/projects/{projectKey}/repos/{cdRepoName}/webhooks", Method.Post);
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.Username}:{_config.Password}"));
        request.AddHeader("Authorization", $"Basic {auth}");
        request.AddHeader("Content-Type", "application/json");

        var webhookUrl = Environment.GetEnvironmentVariable("ARGOCD_WEBHOOK_URL") ?? "http://argocd-server:8080/api/webhook";
        
        var body = new
        {
            name = "ArgoCD Webhook",
            url = webhookUrl,
            events = new[] { "repo:refs_changed" },
            active = true
        };

        request.AddJsonBody(body);

        var response = await _client.ExecuteAsync(request);
        if (!response.IsSuccessful)
        {
            _logger.LogWarning("Bitbucket API call failed. Status: {StatusCode}, Response: {ResponseContent}", 
                response.StatusCode, response.Content);
        }
        return response.IsSuccessful;
    }

    public async Task<bool> ConfigurePermissionsAsync(string projectKey, string developmentTeam, string cdRepoName)
    {
        // Add team permissions to project
        var projectPermRequest = new RestRequest($"/rest/api/1.0/projects/{projectKey}/permissions/groups", Method.Put);
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.Username}:{_config.Password}"));
        projectPermRequest.AddHeader("Authorization", $"Basic {auth}");
        projectPermRequest.AddHeader("Content-Type", "application/json");

        var projectPermBody = new
        {
            name = developmentTeam,
            permission = "PROJECT_WRITE"
        };

        projectPermRequest.AddJsonBody(projectPermBody);
        var projectPermResponse = await _client.ExecuteAsync(projectPermRequest);

        // Add team permissions to CD repo (direct push to main)
        var repoPermRequest = new RestRequest($"/rest/api/1.0/projects/{projectKey}/repos/{cdRepoName}/permissions/groups", Method.Put);
        repoPermRequest.AddHeader("Authorization", $"Basic {auth}");
        repoPermRequest.AddHeader("Content-Type", "application/json");

        var repoPermBody = new
        {
            name = developmentTeam,
            permission = "REPO_ADMIN"
        };

        repoPermRequest.AddJsonBody(repoPermBody);
        var repoPermResponse = await _client.ExecuteAsync(repoPermRequest);

        return projectPermResponse.IsSuccessful && repoPermResponse.IsSuccessful;
    }

    public async Task<bool> ConfigureBranchPoliciesAsync(string projectKey, string developmentTeam)
    {
        // Configure default reviewers
        var request = new RestRequest($"/rest/api/1.0/projects/{projectKey}/settings/pull-requests", Method.Put);
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.Username}:{_config.Password}"));
        request.AddHeader("Authorization", $"Basic {auth}");
        request.AddHeader("Content-Type", "application/json");

        var body = new
        {
            requiredApprovers = 1,
            requiredAllApprovers = false,
            defaultReviewers = new[]
            {
                new { name = developmentTeam }
            }
        };

        request.AddJsonBody(body);

        var response = await _client.ExecuteAsync(request);
        if (!response.IsSuccessful)
        {
            _logger.LogWarning("Bitbucket API call failed. Status: {StatusCode}, Response: {ResponseContent}", 
                response.StatusCode, response.Content);
        }
        return response.IsSuccessful;
    }
}

