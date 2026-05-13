// Interface defining security service operations: password hashing, encryption, and token generation.
namespace Server.Services;

public interface ISecurityService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    string Encrypt(string plaintext);
    string Decrypt(string ciphertext);
    string GenerateSecureCode(int digits = 6);
    string GenerateRefreshToken();
}
