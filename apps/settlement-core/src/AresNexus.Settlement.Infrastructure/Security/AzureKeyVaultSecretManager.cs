using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace AresNexus.Settlement.Infrastructure.Security;

/// <summary>
/// Production secret manager using Azure Key Vault.
/// </summary>
public sealed class AzureKeyVaultSecretManager : ISecretManager
{
    private readonly SecretClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureKeyVaultSecretManager"/> class.
    /// </summary>
    /// <param name="configuration">The configuration to load settings from.</param>
    public AzureKeyVaultSecretManager(IConfiguration configuration)
    {
        var vaultUri = configuration["Azure:KeyVault:Uri"] ?? throw new InvalidOperationException("Azure Key Vault URI is not configured.");
        _client = new SecretClient(new Uri(vaultUri), new DefaultAzureCredential());
    }

    /// <summary>
    /// Gets a secret value from Azure Key Vault.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    /// <returns>The secret value.</returns>
    public async Task<string> GetSecretAsync(string secretName)
    {
        KeyVaultSecret secret = await _client.GetSecretAsync(secretName);
        return secret.Value;
    }
}
