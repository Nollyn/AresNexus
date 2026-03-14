using Microsoft.Extensions.Configuration;

namespace AresNexus.Services.Settlement.Infrastructure.Security;

/// <summary>
/// Dev-only secret manager using local configuration (UserSecrets).
/// </summary>
public sealed class DevSecretManager(IConfiguration configuration) : ISecretManager
{
    /// <summary>
    /// Gets a secret value from local configuration.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    /// <returns>The secret value.</returns>
    public Task<string> GetSecretAsync(string secretName)
    {
        var secret = configuration[secretName] ?? throw new InvalidOperationException($"Secret '{secretName}' not found in configuration.");
        return Task.FromResult(secret);
    }
}
