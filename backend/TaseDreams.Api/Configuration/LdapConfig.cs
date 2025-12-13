namespace TaseDreams.Api.Configuration;

public class LdapConfig
{
    public string Server { get; set; } = string.Empty;
    public int Port { get; set; } = 389;
    public bool UseSSL { get; set; } = false;
    public string BaseDN { get; set; } = string.Empty;
    public string BindDN { get; set; } = string.Empty;
    public string BindPassword { get; set; } = string.Empty;
    public string UserFilter { get; set; } = "(sAMAccountName={0})";
    public string GroupFilter { get; set; } = "(member={0})";
}

