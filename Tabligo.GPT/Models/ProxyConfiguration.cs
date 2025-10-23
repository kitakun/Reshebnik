namespace Tabligo.GPT.Models;

public class ProxyConfiguration
{
    public string? ProxyUrl { get; set; }
    public ProxyType ProxyType { get; set; } = ProxyType.Http;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool BypassProxyOnLocal { get; set; } = true;
    public string[]? ProxyAllowList { get; set; }
    public bool UseAllowList { get; set; } = false;
}

public enum ProxyType
{
    None,
    Http,
    Https,
    Socks4,
    Socks5
}
