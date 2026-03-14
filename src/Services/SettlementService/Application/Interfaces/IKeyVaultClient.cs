namespace AresNexus.Services.Settlement.Application.Interfaces;

/// <summary>
/// Interface for interacting with a Key Vault for encryption and decryption.
/// Used to meet FINMA/DORA compliance for data at rest.
/// </summary>
public interface IKeyVaultClient
{
    /// <summary>
    /// Encrypts the specified plain text using a key stored in the vault.
    /// </summary>
    /// <param name="plainText">The data to encrypt.</param>
    /// <param name="keyId">The identifier of the key to use.</param>
    /// <returns>The base64 encoded cipher text.</returns>
    Task<string> EncryptAsync(string plainText, string keyId);

    /// <summary>
    /// Decrypts the specified cipher text using a key stored in the vault.
    /// </summary>
    /// <param name="cipherText">The base64 encoded cipher text to decrypt.</param>
    /// <param name="keyId">The identifier of the key to use.</param>
    /// <returns>The decrypted plain text.</returns>
    Task<string> DecryptAsync(string cipherText, string keyId);
}
