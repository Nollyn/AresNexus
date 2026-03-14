using System.Security.Cryptography;
using System.Text;
using AresNexus.Services.Settlement.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AresNexus.Services.Settlement.Infrastructure.Security;

/// <summary>
/// PII Encryption Service using AES-256 for Zurich Compliance.
/// </summary>
public sealed class PiiEncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    /// <summary>
    /// Initializes a new instance of the <see cref="PiiEncryptionService"/> class.
    /// </summary>
    /// <param name="secretManager">The secret manager to load the encryption key from.</param>
    public PiiEncryptionService(ISecretManager secretManager)
    {
        var keyString = secretManager.GetSecretAsync("Security:EncryptionKey").GetAwaiter().GetResult() 
                        ?? "SwissBankingSecretKey2026!AresNexus";
        _key = Encoding.UTF8.GetBytes(keyString.PadRight(32)[..32]);
    }

    /// <inheritdoc />
    public async Task<string> EncryptAsync(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;

        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        
        // Write IV first
        await ms.WriteAsync(aes.IV, 0, aes.IV.Length);

        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        using (var sw = new StreamWriter(cs))
        {
            await sw.WriteAsync(plainText);
        }

        return Convert.ToBase64String(ms.ToArray());
    }

    /// <inheritdoc />
    public async Task<string> DecryptAsync(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText)) return cipherText;

        var fullCipher = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = _key;

        var iv = new byte[aes.BlockSize / 8];
        var cipher = new byte[fullCipher.Length - iv.Length];

        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

        aes.IV = iv;

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(cipher);
        using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var sr = new StreamReader(cs);

        return await sr.ReadToEndAsync();
    }
}
