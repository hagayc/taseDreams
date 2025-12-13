namespace TaseDreams.Api.Services;

public interface ILdapService
{
    Task<bool> AuthenticateAsync(string username, string password);
    Task<List<string>> GetUserGroupsAsync(string username);
    Task<bool> IsUserInGroupAsync(string username, string groupName);
}

