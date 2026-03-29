using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

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
    Task<string> TokenizeAsync(string value, string scope);
    Task<string> DetokenizeAsync(string token, string scope);
}

public class DataProtectionGateway : IDataProtectionGateway
{
    private static readonly Regex IbanRegex = new Regex(@"[A-Z]{2}\d{2}[A-Z0-9]{4}\d{7}([A-Z0-9]?){0,16}", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new Regex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled);
    private static readonly Regex AccountIdRegex = new Regex(@"ACC-\d{4}-\d{4}-\d{4}", RegexOptions.Compiled);
    
    // In-memory token store for demonstration. In production, use a secure vault.
    private readonly ConcurrentDictionary<string, string> _tokens = new();
    private readonly ConcurrentDictionary<string, string> _vault = new();

    public async Task<string> SanitizeAsync(string input, SensitivityLevel level = SensitivityLevel.Confidential)
    {
        if (string.IsNullOrEmpty(input)) return input;

        var sanitized = input;

        // Redact Emails
        sanitized = EmailRegex.Replace(sanitized, "[EMAIL_REDACTED]");

        // Tokenize IBANs
        sanitized = IbanRegex.Replace(sanitized, m => TokenizeInternal(m.Value, "IBAN"));

        // Tokenize Account IDs
        sanitized = AccountIdRegex.Replace(sanitized, m => TokenizeInternal(m.Value, "ACC"));

        // Transaction amounts -> Range buckets
        sanitized = Regex.Replace(sanitized, @"CHF\s?(\d+(\.\d{2})?)", m => BucketAmount(m.Groups[1].Value));

        return await Task.FromResult(sanitized);
    }

    public async Task<T> SanitizeObjectAsync<T>(T input) where T : class
    {
        // For agents, we usually just want a sanitized string representation for the LLM prompt.
        return await Task.FromResult(input);
    }

    public Task<string> TokenizeAsync(string value, string scope) => Task.FromResult(TokenizeInternal(value, scope));

    public Task<string> DetokenizeAsync(string token, string scope)
    {
        if (_vault.TryGetValue($"{scope}:{token}", out var value))
        {
            return Task.FromResult(value);
        }
        return Task.FromResult("DETOKENIZATION_FAILED");
    }

    private string TokenizeInternal(string value, string scope)
    {
        var key = $"{scope}:{value}";
        if (_tokens.TryGetValue(key, out var token))
        {
            return token;
        }

        var newToken = $"{scope}_TKN_{Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper()}";
        _tokens[key] = newToken;
        _vault[$"{scope}:{newToken}"] = value;
        return newToken;
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
