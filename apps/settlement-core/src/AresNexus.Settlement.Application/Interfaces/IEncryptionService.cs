namespace AresNexus.Settlement.Application.Interfaces;

/// <summary>
/// Service for encrypting and decrypting sensitive data.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts the specified plain text.
    /// </summary>
    /// <param name="plainText">The data to encrypt.</param>
    /// <returns>The encrypted data.</returns>
    Task<string> EncryptAsync(string plainText);

    /// <summary>
    /// Decrypts the specified cipher text.
    /// </summary>
    /// <param name="cipherText">The data to decrypt.</param>
    /// <returns>The decrypted data.</returns>
    Task<string> DecryptAsync(string cipherText);
}
