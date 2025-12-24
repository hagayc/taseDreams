using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestSharp;
using TaseDreams.Api.Configuration;
using System.Text;
using System.Text.Json;

namespace TaseDreams.Api.Services;

public class ArtifactoryService : IArtifactoryService
{
    private readonly ArtifactoryConfig _config;
    private readonly RestClient _devClient;
    private readonly RestClient _prdClient;
    private readonly ILogger<ArtifactoryService> _logger;

    public ArtifactoryService(IOptions<ArtifactoryConfig> config, ILogger<ArtifactoryService> logger)
    {
        _config = config.Value;
        _logger = logger;
        
        // Replace localhost with Docker service name for container-to-container communication
        var devUrl = _config.DevUrl;
        var prdUrl = _config.PrdUrl;
        
        if (!string.IsNullOrEmpty(devUrl) && (devUrl.Contains("localhost") || devUrl.Contains("127.0.0.1")))
        {
            devUrl = devUrl.Replace("localhost", "artifactory").Replace("127.0.0.1", "artifactory");
            // Fix port 8082 to 8081 (8082 is router port, 8081 is Artifactory API port)
            devUrl = devUrl.Replace(":8082/", ":8081/").Replace(":8082\"", ":8081\"");
            _logger.LogInformation("Artifactory DevUrl contains localhost, replacing with Docker service name: {DevUrl}", devUrl);
        }
        
        if (!string.IsNullOrEmpty(prdUrl) && (prdUrl.Contains("localhost") || prdUrl.Contains("127.0.0.1")))
        {
            prdUrl = prdUrl.Replace("localhost", "artifactory").Replace("127.0.0.1", "artifactory");
            // Fix port 8082 to 8081 (8082 is router port, 8081 is Artifactory API port)
            prdUrl = prdUrl.Replace(":8082/", ":8081/").Replace(":8082\"", ":8081\"");
            _logger.LogInformation("Artifactory PrdUrl contains localhost, replacing with Docker service name: {PrdUrl}", prdUrl);
        }
        
        // If URLs are empty, default to Docker service name
        if (string.IsNullOrEmpty(devUrl))
        {
            devUrl = "http://artifactory:8081";
            _logger.LogInformation("Artifactory DevUrl not configured, using default Docker service name: {DevUrl}", devUrl);
        }
        
        if (string.IsNullOrEmpty(prdUrl))
        {
            prdUrl = "http://artifactory:8081";
            _logger.LogInformation("Artifactory PrdUrl not configured, using default Docker service name: {PrdUrl}", prdUrl);
        }
        
        // Ensure URLs don't end with /artifactory (RestSharp will handle it)
        if (devUrl.EndsWith("/artifactory"))
        {
            devUrl = devUrl.Substring(0, devUrl.Length - "/artifactory".Length);
        }
        if (prdUrl.EndsWith("/artifactory"))
        {
            prdUrl = prdUrl.Substring(0, prdUrl.Length - "/artifactory".Length);
        }
        
        // Remove trailing slashes to avoid double slashes in URL construction
        devUrl = devUrl.TrimEnd('/');
        prdUrl = prdUrl.TrimEnd('/');
        
        // Ensure both URLs use port 8081 (not 8082 which is router port)
        if (devUrl.Contains(":8082"))
        {
            devUrl = devUrl.Replace(":8082", ":8081");
            _logger.LogWarning("Artifactory DevUrl was using port 8082 (router port), corrected to 8081: {DevUrl}", devUrl);
        }
        
        if (prdUrl.Contains(":8082"))
        {
            prdUrl = prdUrl.Replace(":8082", ":8081");
            _logger.LogWarning("Artifactory PrdUrl was using port 8082 (router port), corrected to 8081: {PrdUrl}", prdUrl);
        }
        
        _devClient = new RestClient(devUrl);
        _prdClient = new RestClient(prdUrl);
    }

    public async Task<bool> CreateDockerRepositoryAsync(string projectName, string backendLanguage)
    {
        try
        {
            if (string.IsNullOrEmpty(_config.Username) || string.IsNullOrEmpty(_config.Password))
            {
                _logger.LogError("Artifactory credentials are not configured (Username or Password is empty)");
                return false;
            }

            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.Username}:{_config.Password}"));

            // Create dev repository
            var devRequest = new RestRequest("/artifactory/api/repositories/{repoKey}", Method.Put);
            devRequest.AddHeader("Authorization", $"Basic {auth}");
            devRequest.AddHeader("Content-Type", "application/json");
            // Remove Accept header that RestSharp might add automatically (causes 406 error)
            devRequest.AddOrUpdateHeader("Accept", "*/*");
            devRequest.AddUrlSegment("repoKey", $"oc-{projectName}");

            var devBody = new
            {
                key = $"oc-{projectName}",
                rclass = "local",
                packageType = "docker",
                description = $"Docker repository for {projectName}"
            };

            // Use StringBody instead of AddJsonBody to avoid RestSharp adding Accept: application/json
            var jsonBody = JsonSerializer.Serialize(devBody);
            devRequest.AddStringBody(jsonBody, ContentType.Json);
            
            _logger.LogInformation("Creating Artifactory dev repository: oc-{ProjectName} at {DevUrl}", projectName, _devClient.Options.BaseUrl);
            _logger.LogInformation("Request URL: {RequestUrl}, Method: {Method}", _devClient.BuildUri(devRequest), devRequest.Method);
            var devResponse = await _devClient.ExecuteAsync(devRequest);
            _logger.LogInformation("Response Status: {StatusCode}, Content: {Content}", devResponse.StatusCode, devResponse.Content?.Substring(0, Math.Min(200, devResponse.Content?.Length ?? 0)));
            
            if (!devResponse.IsSuccessful)
            {
                // Check if this is Artifactory OSS (which doesn't support repository creation API)
                if (devResponse.Content?.Contains("Artifactory Pro") == true || 
                    devResponse.Content?.Contains("available only in Artifactory Pro") == true)
                {
                    _logger.LogWarning("Artifactory OSS detected - Repository creation API is not available. Repositories must be created manually via UI. Status: {StatusCode}, Response: {ResponseContent}", 
                        devResponse.StatusCode, devResponse.Content);
                    // For OSS, we'll skip repository creation but continue with project creation
                    return true; // Return true to allow project creation to continue
                }
                
                _logger.LogError("Failed to create Artifactory dev repository. Status: {StatusCode}, Response: {ResponseContent}", 
                    devResponse.StatusCode, devResponse.Content);
                _logger.LogError("Error: {ErrorMessage}", devResponse.ErrorMessage);
                return false;
            }

            // Create production repository
            var prdRequest = new RestRequest("/artifactory/api/repositories/{repoKey}", Method.Put);
            prdRequest.AddHeader("Authorization", $"Basic {auth}");
            prdRequest.AddHeader("Content-Type", "application/json");
            // Remove Accept header that RestSharp might add automatically (causes 406 error)
            prdRequest.AddOrUpdateHeader("Accept", "*/*");
            prdRequest.AddUrlSegment("repoKey", $"oc-{projectName}-prd");

            var prdBody = new
            {
                key = $"oc-{projectName}-prd",
                rclass = "local",
                packageType = "docker",
                description = $"Production Docker repository for {projectName}"
            };

            // Use StringBody instead of AddJsonBody to avoid RestSharp adding Accept: application/json
            var prdJsonBody = JsonSerializer.Serialize(prdBody);
            prdRequest.AddStringBody(prdJsonBody, ContentType.Json);
            
            _logger.LogInformation("Creating Artifactory production repository: oc-{ProjectName}-prd at {PrdUrl}", projectName, _prdClient.Options.BaseUrl);
            var prdResponse = await _prdClient.ExecuteAsync(prdRequest);
            
            if (!prdResponse.IsSuccessful)
            {
                // Check if this is Artifactory OSS (which doesn't support repository creation API)
                if (prdResponse.Content?.Contains("Artifactory Pro") == true || 
                    prdResponse.Content?.Contains("available only in Artifactory Pro") == true)
                {
                    _logger.LogWarning("Artifactory OSS detected - Repository creation API is not available. Repositories must be created manually via UI. Status: {StatusCode}, Response: {ResponseContent}", 
                        prdResponse.StatusCode, prdResponse.Content);
                    // For OSS, we'll skip repository creation but continue with project creation
                    return true; // Return true to allow project creation to continue
                }
                
                _logger.LogError("Failed to create Artifactory production repository. Status: {StatusCode}, Response: {ResponseContent}", 
                    prdResponse.StatusCode, prdResponse.Content);
                _logger.LogError("Error: {ErrorMessage}", prdResponse.ErrorMessage);
                return false;
            }

            _logger.LogInformation("Successfully created Artifactory repositories for project: {ProjectName}", projectName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while creating Artifactory repositories for project: {ProjectName}", projectName);
            return false;
        }
    }

    public async Task<bool> SetupReplicationAsync(string projectName)
    {
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.Username}:{_config.Password}"));

        var request = new RestRequest("/artifactory/api/replications/{repoKey}", Method.Put);
        request.AddHeader("Authorization", $"Basic {auth}");
        request.AddHeader("Content-Type", "application/json");
        request.AddUrlSegment("repoKey", $"oc-{projectName}");

        var body = new
        {
            url = $"{_config.PrdUrl}/artifactory/oc-{projectName}-prd",
            socketTimeoutMillis = 15000,
            username = _config.Username,
            password = _config.Password,
            enableEventReplication = true,
            enabled = true,
            syncDeletes = true,
            syncProperties = true,
            syncStatistics = false,
            repoKey = $"oc-{projectName}",
            cronExp = "0 0/60 * * * ?"
        };

        request.AddJsonBody(body);

        var response = await _devClient.ExecuteAsync(request);
        return response.IsSuccessful;
    }

    public async Task<bool> ConfigurePermissionsAsync(string projectName, string developmentTeam)
    {
        var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.Username}:{_config.Password}"));

        // Grant permissions to replication user
        var request = new RestRequest("/artifactory/api/security/permissions/{permissionName}", Method.Put);
        request.AddHeader("Authorization", $"Basic {auth}");
        request.AddHeader("Content-Type", "application/json");
        request.AddUrlSegment("permissionName", $"oc-{projectName}-replication");

        var body = new
        {
            name = $"oc-{projectName}-replication",
            repositories = new[] { $"oc-{projectName}", $"oc-{projectName}-prd" },
            principals = new
            {
                users = new Dictionary<string, string[]>
                {
                    { _config.Username, new[] { "read", "write", "annotate", "delete" } }
                }
            }
        };

        request.AddJsonBody(body);

        var response = await _devClient.ExecuteAsync(request);
        return response.IsSuccessful;
    }
}

