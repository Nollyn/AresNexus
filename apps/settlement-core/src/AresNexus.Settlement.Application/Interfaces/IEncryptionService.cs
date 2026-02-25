namespace AresNexus.Settlement.Application.Interfaces;

/// <summary>
/// Service for encrypting and decrypting sensitive data.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts the specified plain text.
    /// </summary>
    Task<string> EncryptAsync(string plainText);

    /// <summary>
    /// Decrypts the specified cipher text.
    /// </summary>
    Task<string> DecryptAsync(string cipherText);
}
