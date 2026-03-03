using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Infrastructure.Security;
using FluentAssertions;
using Moq;
using Xunit;

namespace AresNexus.Tests.Unit;

public class PiiEncryptionServiceTests
{
    private readonly Mock<ISecretManager> _secretManagerMock;
    private readonly PiiEncryptionService _service;

    public PiiEncryptionServiceTests()
    {
        _secretManagerMock = new Mock<ISecretManager>();
        _secretManagerMock.Setup(s => s.GetSecretAsync(It.IsAny<string>()))
            .ReturnsAsync("TestSecretKey123456789012345678");
        
        _service = new PiiEncryptionService(_secretManagerMock.Object);
    }

    [Fact]
    public async Task Encrypt_ShouldReturnBase64String()
    {
        // Arrange
        var plainText = "Sensitive PII Data";

        // Act
        var encrypted = await _service.EncryptAsync(plainText);

        // Assert
        encrypted.Should().NotBeNullOrEmpty();
        encrypted.Should().NotBe(plainText);
        // Verify it's valid base64
        var act = () => Convert.FromBase64String(encrypted);
        act.Should().NotThrow();
    }

    [Fact]
    public async Task Decrypt_ShouldReturnOriginalText()
    {
        // Arrange
        var originalText = "Sensitive PII Data";
        var encrypted = await _service.EncryptAsync(originalText);

        // Act
        var decrypted = await _service.DecryptAsync(encrypted);

        // Assert
        decrypted.Should().Be(originalText);
    }

    [Fact]
    public async Task Encrypt_EmptyString_ShouldReturnEmptyString()
    {
        // Act
        var result = await _service.EncryptAsync(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Decrypt_EmptyString_ShouldReturnEmptyString()
    {
        // Act
        var result = await _service.DecryptAsync(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Encrypt_Null_ShouldReturnNull()
    {
        // Act
        var result = await _service.EncryptAsync(null!);

        // Assert
        result.Should().BeNull();
    }
}
