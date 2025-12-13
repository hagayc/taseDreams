using Microsoft.Extensions.Options;
using RestSharp;
using TaseDreams.Api.Configuration;
using System.Text;
using System.Text.Json;

namespace TaseDreams.Api.Services;

public class ConfluenceService : IConfluenceService
{
    private readonly ConfluenceConfig _config;
    private readonly RestClient _client;

    public ConfluenceService(IOptions<ConfluenceConfig> config)
    {
        _config = config.Value;
        _client = new RestClient(_config.BaseUrl);
    }

    public async Task<bool> CreateProjectPageAsync(string projectName, string projectKey, string developmentTeam, Dictionary<string, string> links)
    {
        try
        {
            var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_config.Username}:{_config.Password}"));

            // Get space information
            var spaceRequest = new RestRequest($"/rest/api/space/{_config.SpaceKey}", Method.Get);
            spaceRequest.AddHeader("Authorization", $"Basic {auth}");
            var spaceResponse = await _client.ExecuteAsync(spaceRequest);

            if (!spaceResponse.IsSuccessful)
                return false;

            var spaceData = JsonSerializer.Deserialize<JsonElement>(spaceResponse.Content!);
            var spaceId = spaceData.GetProperty("id").GetString();

            // Create page content
            var pageContent = GeneratePageContent(projectName, projectKey, developmentTeam, links);

            // Create page
            var pageRequest = new RestRequest("/rest/api/content", Method.Post);
            pageRequest.AddHeader("Authorization", $"Basic {auth}");
            pageRequest.AddHeader("Content-Type", "application/json");

            var pageBody = new
            {
                type = "page",
                title = projectName,
                space = new { key = _config.SpaceKey },
                body = new
                {
                    storage = new
                    {
                        value = pageContent,
                        representation = "storage"
                    }
                }
            };

            pageRequest.AddJsonBody(pageBody);
            var pageResponse = await _client.ExecuteAsync(pageRequest);

            return pageResponse.IsSuccessful;
        }
        catch
        {
            return false;
        }
    }

    private string GeneratePageContent(string projectName, string projectKey, string developmentTeam, Dictionary<string, string> links)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"<h1>{projectName}</h1>");
        sb.AppendLine($"<p><strong>Project Key:</strong> {projectKey}</p>");
        sb.AppendLine($"<p><strong>Development Team:</strong> {developmentTeam}</p>");
        sb.AppendLine("<h2>Quick Links</h2>");
        sb.AppendLine("<ul>");

        foreach (var link in links)
        {
            sb.AppendLine($"<li><a href=\"{link.Value}\">{link.Key}</a></li>");
        }

        sb.AppendLine("</ul>");
        sb.AppendLine("<h2>Project Information</h2>");
        sb.AppendLine("<p>This project was automatically created by TaseDreams.</p>");

        return sb.ToString();
    }
}

