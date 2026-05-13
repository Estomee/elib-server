// Security service providing BCrypt password hashing, AES-GCM encryption, and secure random token generation.
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Server.Services;

public class SecurityOptions
{
    public string EncryptionKey { get; set; } = string.Empty;
}

public class SecurityService : ISecurityService
{
    private readonly byte[] _key;

    public SecurityService(IOptions<SecurityOptions> options)
    {
        var keyBase64 = options.Value.EncryptionKey;
        if (string.IsNullOrEmpty(keyBase64))
            throw new InvalidOperationException("SecurityOptions:EncryptionKey is not configured.");

        _key = Convert.FromBase64String(keyBase64);
        if (_key.Length != 32)
            throw new InvalidOperationException("EncryptionKey must be exactly 32 bytes (256-bit AES key).");
    }

    public string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    public bool VerifyPassword(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);

    public string Encrypt(string plaintext)
    {
        var plainBytes = Encoding.UTF8.GetBytes(plaintext);
        var nonce = new byte[AesGcm.NonceByteSizes.MaxSize];
        RandomNumberGenerator.Fill(nonce);

        var cipherBytes = new byte[plainBytes.Length];
        var tag = new byte[AesGcm.TagByteSizes.MaxSize];

        using var aes = new AesGcm(_key, AesGcm.TagByteSizes.MaxSize);
        aes.Encrypt(nonce, plainBytes, cipherBytes, tag);

        var result = new byte[nonce.Length + cipherBytes.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, nonce.Length, cipherBytes.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length + cipherBytes.Length, tag.Length);

        return Convert.ToBase64String(result);
    }

    public string Decrypt(string ciphertext)
    {
        var data = Convert.FromBase64String(ciphertext);
        var nonceSize = AesGcm.NonceByteSizes.MaxSize;
        var tagSize = AesGcm.TagByteSizes.MaxSize;
        var cipherSize = data.Length - nonceSize - tagSize;

        if (cipherSize < 0)
            throw new CryptographicException("Invalid ciphertext length.");

        var nonce = data[..nonceSize];
        var cipher = data[nonceSize..(nonceSize + cipherSize)];
        var tag = data[(nonceSize + cipherSize)..];
        var plain = new byte[cipherSize];

        using var aes = new AesGcm(_key, tagSize);
        aes.Decrypt(nonce, cipher, tag, plain);

        return Encoding.UTF8.GetString(plain);
    }

    public string GenerateSecureCode(int digits = 6)
    {
        var min = (int)Math.Pow(10, digits - 1);
        var max = (int)Math.Pow(10, digits);
        return RandomNumberGenerator.GetInt32(min, max).ToString($"D{digits}");
    }

    public string GenerateRefreshToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }
}
