using System.Text;

namespace DrinkWater.Core;

public enum ProxyMode { System, Direct, Custom }

public enum ProxyProtocol { Http, Https, Socks5 }

public sealed record ProxySettings
{
    public ProxyMode Mode { get; init; } = ProxyMode.System;
    public ProxyProtocol Protocol { get; init; } = ProxyProtocol.Http;
    public string Host { get; init; } = "127.0.0.1";
    public int Port { get; init; } = 8080;
    public string Username { get; init; } = "";
    public string? ProtectedPassword { get; init; }

    public void Validate()
    {
        if (!Enum.IsDefined(Mode) || !Enum.IsDefined(Protocol))
            throw new ArgumentException("Choose a supported proxy mode and protocol.");
        if (string.IsNullOrWhiteSpace(Host) || Host.Length > 253 || Host.Any(char.IsWhiteSpace)
            || Uri.CheckHostName(Host) == UriHostNameType.Unknown)
            throw new ArgumentException("Enter a hostname or IP address without a URL, username, or port.");
        if (Port is < 1 or > 65535)
            throw new ArgumentException("Enter a proxy port from 1 to 65535.");
        if (Username is null || Username.Length > 256 || Username.Any(char.IsControl)
            || (Protocol == ProxyProtocol.Socks5 && Encoding.UTF8.GetByteCount(Username) > 255))
            throw new ArgumentException("The proxy username is too long or contains unsupported characters.");
        if (ProtectedPassword is { Length: > 8192 } || (ProtectedPassword is not null && Username.Length == 0))
            throw new ArgumentException("Proxy credentials are invalid. Enter them again.");
    }

    public Uri GetAddress()
    {
        Validate();
        string scheme = Protocol switch
        {
            ProxyProtocol.Http => "http",
            ProxyProtocol.Https => "https",
            ProxyProtocol.Socks5 => "socks5",
            _ => throw new ArgumentException("Choose a supported proxy protocol.")
        };
        return new UriBuilder(scheme, Host, Port).Uri;
    }
}
