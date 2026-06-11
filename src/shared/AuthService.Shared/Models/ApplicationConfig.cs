namespace AuthService.Shared.Models;

/// <summary>
/// Strongly-typed model matching configs/applications.example.yaml.
/// Represents a registered client application in the auth platform.
/// </summary>
public class ApplicationConfig
{
    public string Name { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public List<string> RedirectUris { get; set; } = [];
    public List<string> PostLogoutRedirectUris { get; set; } = [];
    public string FrontChannelOrigin { get; set; } = string.Empty;
    public BffConfig Bff { get; set; } = new();
    public ApiConfig Api { get; set; } = new();
}

public class BffConfig
{
    public string SessionCookieName { get; set; } = string.Empty;
    public string LogoutEndpoint { get; set; } = "/bff/logout";
    public string BackchannelLogoutPath { get; set; } = "/backchannel-logout";
}

public class ApiConfig
{
    public string Audience { get; set; } = string.Empty;
    public List<string> Scopes { get; set; } = [];
}

/// <summary>
/// Root configuration containing global settings and all registered applications.
/// </summary>
public class ApplicationsConfig
{
    public GlobalConfig Global { get; set; } = new();
    public List<ApplicationConfig> Applications { get; set; } = [];
}

public class GlobalConfig
{
    public string Authority { get; set; } = string.Empty;
    public string ResponseType { get; set; } = "code";
    public bool RequirePkce { get; set; } = true;
    public bool BackchannelLogoutSupported { get; set; } = true;
}
