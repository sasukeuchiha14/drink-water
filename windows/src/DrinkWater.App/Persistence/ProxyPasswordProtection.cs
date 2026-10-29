using System.Security.Cryptography;
using System.Text;
using DrinkWater.Core;

namespace DrinkWater.App.Persistence;

public static class ProxyPasswordProtection
{
    public static string Protect(string password, ProxySettings proxy)
    {
        ValidatePassword(password, proxy);
        byte[] bytes = Encoding.UTF8.GetBytes(password);
        try
        {
            return Convert.ToBase64String(ProtectedData.Protect(bytes, Entropy(proxy), DataProtectionScope.CurrentUser));
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    public static string Unprotect(ProxySettings proxy)
    {
        if (proxy.ProtectedPassword is null) return "";
        byte[] bytes = ProtectedData.Unprotect(Convert.FromBase64String(proxy.ProtectedPassword), Entropy(proxy), DataProtectionScope.CurrentUser);
        try
        {
            string password = new UTF8Encoding(false, true).GetString(bytes);
            ValidatePassword(password, proxy);
            return password;
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    private static void ValidatePassword(string password, ProxySettings proxy)
    {
        proxy.Validate();
        int limit = proxy.Protocol == ProxyProtocol.Socks5 ? 255 : 1024;
        if (Encoding.UTF8.GetByteCount(password) > limit || password.Any(char.IsControl))
            throw new ArgumentException("The proxy password is too long or contains unsupported characters.");
        if (password.Length > 0 && proxy.Username.Length == 0)
            throw new ArgumentException("Enter a username when using a proxy password.");
    }

    private static byte[] Entropy(ProxySettings proxy)
    {
        Uri address = proxy.GetAddress();
        return SHA256.HashData(Encoding.UTF8.GetBytes($"DrinkWater.Proxy.v1\n{address.Scheme}\n{address.IdnHost}\n{address.Port}\n{proxy.Username}"));
    }
}
