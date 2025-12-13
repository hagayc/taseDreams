using Microsoft.Extensions.Options;
using TaseDreams.Api.Configuration;
using System.DirectoryServices.Protocols;

namespace TaseDreams.Api.Services;

public class LdapService : ILdapService
{
    private readonly LdapConfig _config;

    public LdapService(IOptions<LdapConfig> config)
    {
        _config = config.Value;
    }

    public async Task<bool> AuthenticateAsync(string username, string password)
    {
        try
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                return false;

            var serverId = new LdapDirectoryIdentifier(_config.Server, _config.Port);
            using var connection = new LdapConnection(serverId);

            if (_config.UseSSL)
            {
                connection.SessionOptions.SecureSocketLayer = true;
            }

            // Bind with user credentials
            var userDN = await GetUserDNAsync(username);
            if (string.IsNullOrEmpty(userDN))
                return false;

            connection.Bind(new System.Net.NetworkCredential(userDN, password));
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<string>> GetUserGroupsAsync(string username)
    {
        try
        {
            var serverId = new LdapDirectoryIdentifier(_config.Server, _config.Port);
            using var connection = new LdapConnection(serverId);

            if (_config.UseSSL)
            {
                connection.SessionOptions.SecureSocketLayer = true;
            }

            // Bind with service account
            connection.Bind(new System.Net.NetworkCredential(_config.BindDN, _config.BindPassword));

            var userDN = await GetUserDNAsync(username);
            if (string.IsNullOrEmpty(userDN))
                return new List<string>();

            // Search for groups
            var searchRequest = new SearchRequest(
                _config.BaseDN,
                _config.GroupFilter.Replace("{0}", userDN),
                SearchScope.Subtree,
                "cn"
            );

            var response = (SearchResponse)await Task.FromResult(connection.SendRequest(searchRequest));
            var groups = new List<string>();

            foreach (SearchResultEntry entry in response.Entries)
            {
                if (entry.Attributes.Contains("cn"))
                {
                    groups.Add(entry.Attributes["cn"][0].ToString() ?? string.Empty);
                }
            }

            return groups;
        }
        catch
        {
            return new List<string>();
        }
    }

    public async Task<bool> IsUserInGroupAsync(string username, string groupName)
    {
        var groups = await GetUserGroupsAsync(username);
        return groups.Contains(groupName, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<string> GetUserDNAsync(string username)
    {
        try
        {
            var serverId = new LdapDirectoryIdentifier(_config.Server, _config.Port);
            using var connection = new LdapConnection(serverId);

            if (_config.UseSSL)
            {
                connection.SessionOptions.SecureSocketLayer = true;
            }

            // Bind with service account
            connection.Bind(new System.Net.NetworkCredential(_config.BindDN, _config.BindPassword));

            // Search for user
            var searchFilter = _config.UserFilter.Replace("{0}", username);
            var searchRequest = new SearchRequest(
                _config.BaseDN,
                searchFilter,
                SearchScope.Subtree,
                "distinguishedName"
            );

            var response = (SearchResponse)await Task.FromResult(connection.SendRequest(searchRequest));

            if (response.Entries.Count > 0)
            {
                var entry = response.Entries[0];
                if (entry.Attributes.Contains("distinguishedName"))
                {
                    return entry.Attributes["distinguishedName"][0].ToString() ?? string.Empty;
                }
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}

