namespace TaseDreams.Api.Configuration;

public class AppSettings
{
    public int MaxProjectNameLength { get; set; } = 12;
    public string ProjectPrefix { get; set; } = "oc-";
    public string CDRepoSuffix { get; set; } = "-cd";
    public string FrontendRepoSuffix { get; set; } = "-Fe";
}

public class BitbucketConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ArtifactoryConfig
{
    public string DevUrl { get; set; } = string.Empty;
    public string PrdUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class ArgoCDConfig
{
    public string ConfigRepoUrl { get; set; } = string.Empty;
    public string ConfigRepoBranch { get; set; } = "main";
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class OCPConfig
{
    public string ApiUrl { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}

public class ConfluenceConfig
{
    public string BaseUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SpaceKey { get; set; } = string.Empty;
}

