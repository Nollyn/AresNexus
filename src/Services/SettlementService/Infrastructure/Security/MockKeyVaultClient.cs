using AresNexus.Services.Settlement.Application.Interfaces;

namespace AresNexus.Services.Settlement.Infrastructure.Security;

/// <summary>
/// Mock implementation of IKeyVaultClient for demonstration.
/// Simulates encrypting data before it hits the database for FINMA/DORA compliance.
/// </summary>
public sealed class MockKeyVaultClient : IKeyVaultClient
{
    private const string Prefix = "KVAULT:";

    /// <inheritdoc />
    public Task<string> EncryptAsync(string plainText, string keyId)
    {
        // Simple mock encryption
        return Task.FromResult($"{Prefix}{keyId}:{plainText}");
    }

    /// <inheritdoc />
    public Task<string> DecryptAsync(string cipherText, string keyId)
    {
        if (cipherText.StartsWith($"{Prefix}{keyId}:"))
        {
            return Task.FromResult(cipherText[(Prefix.Length + keyId.Length + 1)..]);
        }
        return Task.FromResult(cipherText);
    }
}
