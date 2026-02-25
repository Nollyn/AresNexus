using AresNexus.Settlement.Application.Interfaces;

namespace AresNexus.Settlement.Infrastructure.Security;

/// <summary>
/// Mock implementation of IEncryptionService for demonstration.
/// In production, this would use Azure Key Vault.
/// </summary>
public sealed class MockEncryptionService : IEncryptionService
{
    private const string Prefix = "ENC:";

    /// <inheritdoc />
    public Task<string> EncryptAsync(string plainText)
    {
        // Simple mock encryption
        return Task.FromResult($"{Prefix}{plainText}");
    }

    /// <inheritdoc />
    public Task<string> DecryptAsync(string cipherText)
    {
        return Task.FromResult(cipherText.StartsWith(Prefix) ? cipherText[Prefix.Length..] : cipherText);
    }
}
