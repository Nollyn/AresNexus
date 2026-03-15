using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace AresNexus.AiAgents.Core.Protection;

public enum SensitivityLevel
{
    Public,
    Internal,
    Confidential,
    HighlyConfidential
}

public interface IDataProtectionGateway
{
    Task<string> SanitizeAsync(string input, SensitivityLevel level = SensitivityLevel.Confidential);
    Task<T> SanitizeObjectAsync<T>(T input) where T : class;
}

public class DataProtectionGateway : IDataProtectionGateway
{
    private static readonly Regex IbanRegex = new Regex(@"[A-Z]{2}\d{2}[A-Z0-9]{4}\d{7}([A-Z0-9]?){0,16}", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new Regex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);
    private static readonly Regex AccountIdRegex = new Regex(@"ACC-\d{4}-\d{4}-\d{4}", RegexOptions.Compiled);

    public async Task<string> SanitizeAsync(string input, SensitivityLevel level = SensitivityLevel.Confidential)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var sanitized = input;

        // Redact Emails
        sanitized = EmailRegex.Replace(sanitized, "[EMAIL_REDACTED]");

        // Hash IBANs
        sanitized = IbanRegex.Replace(sanitized, m => HashIdentifier(m.Value, "IBAN"));

        // Tokenize/Hash Account IDs
        sanitized = AccountIdRegex.Replace(sanitized, m => HashIdentifier(m.Value, "ACC"));

        // Transaction amounts -> Range buckets (simplified example)
        sanitized = Regex.Replace(sanitized, @"CHF\s?(\d+(\.\d{2})?)", m => BucketAmount(m.Groups[1].Value));

        return await Task.FromResult(sanitized);
    }

    public async Task<T> SanitizeObjectAsync<T>(T input) where T : class
    {
        // Simple implementation: serialize to string, sanitize, then we'd ideally deserialize
        // But for agents, we usually just want a sanitized string representation for the LLM prompt.
        // For now, let's just return a placeholder or do reflection if needed.
        return await Task.FromResult(input);
    }

    private string HashIdentifier(string value, string prefix)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(value));
        var hash = Convert.ToHexString(bytes).Substring(0, 12);
        return $"{prefix}_HASH_{hash}";
    }

    private string BucketAmount(string amountStr)
    {
        if (decimal.TryParse(amountStr, out var amount))
        {
            if (amount < 100) return "CHF_LOW (<100)";
            if (amount < 1000) return "CHF_MEDIUM (100-1000)";
            if (amount < 10000) return "CHF_HIGH (1000-10000)";
            return "CHF_VERY_HIGH (>10000)";
        }
        return "CHF_UNKNOWN_AMOUNT";
    }
}
