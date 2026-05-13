// AES-CBC crypto service for encrypting and decrypting IP addresses stored in system logs.
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Server.Services;

public interface ICryptoService
{
    string  Encrypt(string plainText);
    string  TryDecrypt(string? value);
    bool    IsPlainText(string? value);
}

public class CryptoService : ICryptoService
{
    private readonly byte[] _key;

    public CryptoService(IConfiguration config)
    {
        var keyStr = config["ConnectionStrings:Security:EncryptionKey"]
            ?? throw new InvalidOperationException("Security:EncryptionKey not configured");
        _key = Convert.FromBase64String(keyStr);
    }

    public string Encrypt(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        using var enc = aes.CreateEncryptor();
        var plain  = Encoding.UTF8.GetBytes(plainText);
        var cipher = enc.TransformFinalBlock(plain, 0, plain.Length);
        var result = new byte[16 + cipher.Length];
        aes.IV.CopyTo(result, 0);
        cipher.CopyTo(result, 16);
        return Convert.ToBase64String(result);
    }

    public string TryDecrypt(string? value)
    {
        if (string.IsNullOrEmpty(value)) return value ?? string.Empty;
        try
        {
            var data = Convert.FromBase64String(value);
            if (data.Length < 17) return value;
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV  = data[..16];
            using var dec = aes.CreateDecryptor();
            var plain = dec.TransformFinalBlock(data, 16, data.Length - 16);
            return Encoding.UTF8.GetString(plain);
        }
        catch { return value; }
    }

    public bool IsPlainText(string? value) =>
        !string.IsNullOrEmpty(value) && (value.Contains('.') || value.Contains(':'));
}
