namespace AresNexus.Settlement.Infrastructure.Security;

/// <summary>
/// Interface for a secret manager to abstract Azure Key Vault vs Local Secrets.
/// </summary>
public interface ISecretManager
{
    /// <summary>
    /// Gets a secret value by its name.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    /// <returns>The secret value.</returns>
    Task<string> GetSecretAsync(string secretName);
}
