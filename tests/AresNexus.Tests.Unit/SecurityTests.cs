using System.Security.Cryptography;
using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Infrastructure.Security;
using Azure;
using Azure.Security.KeyVault.Secrets;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace AresNexus.Tests.Unit;

public class SecurityTests
{
    [Fact]
    public async Task PiiEncryptionService_Encrypt_ShouldReturnEncryptedString()
    {
        // Arrange
        var secretManagerMock = new Mock<ISecretManager>();
        secretManagerMock.Setup(s => s.GetSecretAsync("Security:EncryptionKey"))
            .ReturnsAsync("AVerySecureKey123!@#4567890123456");
        var service = new PiiEncryptionService(secretManagerMock.Object);
        var plainText = "Sensitive Swiss Data";

        // Act
        var encrypted = await service.EncryptAsync(plainText);

        // Assert
        encrypted.Should().NotBeNullOrEmpty();
        encrypted.Should().NotBe(plainText);
    }

    [Fact]
    public async Task PiiEncryptionService_Decrypt_ShouldReturnOriginalString()
    {
        // Arrange
        var secretManagerMock = new Mock<ISecretManager>();
        secretManagerMock.Setup(s => s.GetSecretAsync("Security:EncryptionKey"))
            .ReturnsAsync("AVerySecureKey123!@#4567890123456");
        var service = new PiiEncryptionService(secretManagerMock.Object);
        var plainText = "Sensitive Swiss Data";
        var encrypted = await service.EncryptAsync(plainText);

        // Act
        var decrypted = await service.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task PiiEncryptionService_Encrypt_WhenNullOrEmpty_ShouldReturnSame(string? input)
    {
        // Arrange
        var secretManagerMock = new Mock<ISecretManager>();
        var service = new PiiEncryptionService(secretManagerMock.Object);

        // Act
        var result = await service.EncryptAsync(input!);

        // Assert
        result.Should().Be(input);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task PiiEncryptionService_Decrypt_WhenNullOrEmpty_ShouldReturnSame(string? input)
    {
        // Arrange
        var secretManagerMock = new Mock<ISecretManager>();
        var service = new PiiEncryptionService(secretManagerMock.Object);

        // Act
        var result = await service.DecryptAsync(input!);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public async Task PiiEncryptionService_Constructor_WhenSecretNotFound_ShouldUseDefaultKey()
    {
        // Arrange
        var secretManagerMock = new Mock<ISecretManager>();
        secretManagerMock.Setup(s => s.GetSecretAsync(It.IsAny<string>()))
            .ReturnsAsync((string)null!);

        // Act
        var service = new PiiEncryptionService(secretManagerMock.Object);
        var plainText = "Test";
        var encrypted = await service.EncryptAsync(plainText);
        var decrypted = await service.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public async Task PiiEncryptionService_Decrypt_WithInvalidBase64_ShouldThrow()
    {
        // Arrange
        var secretManagerMock = new Mock<ISecretManager>();
        var service = new PiiEncryptionService(secretManagerMock.Object);
        var invalidBase64 = "NotBase64!!!";

        // Act & Assert
        await Assert.ThrowsAsync<FormatException>(() => service.DecryptAsync(invalidBase64));
    }

    [Fact]
    public async Task DevSecretManager_GetSecretAsync_WhenSecretExists_ShouldReturnValue()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?> {
            {"TestSecret", "SecretValue"}
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        var manager = new DevSecretManager(configuration);

        // Act
        var secret = await manager.GetSecretAsync("TestSecret");

        // Assert
        secret.Should().Be("SecretValue");
    }

    [Fact]
    public async Task DevSecretManager_GetSecretAsync_WhenSecretMissing_ShouldThrow()
    {
        // Arrange
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var manager = new DevSecretManager(configuration);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => manager.GetSecretAsync("Missing"));
    }

    [Fact]
    public async Task AzureKeyVaultSecretManager_GetSecretAsync_ShouldReturnFromClient()
    {
        // Arrange
        var clientMock = new Mock<SecretClient>();
        var secretName = "MySecret";
        var secretValue = "SecretValue";
        var keyVaultSecret = SecretModelFactory.KeyVaultSecret(new SecretProperties(secretName), secretValue);
        
        var response = Response.FromValue(keyVaultSecret, new Mock<Response>().Object);

        clientMock.Setup(c => c.GetSecretAsync(secretName, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var manager = new AzureKeyVaultSecretManager(clientMock.Object);

        // Act
        var result = await manager.GetSecretAsync(secretName);

        // Assert
        result.Should().Be(secretValue);
    }
}
