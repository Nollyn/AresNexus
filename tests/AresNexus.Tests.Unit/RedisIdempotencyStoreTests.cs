using System.Text;
using System.Text.Json;
using AresNexus.Services.Settlement.Infrastructure.Idempotency;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Moq;

namespace AresNexus.Tests.Unit;

public class RedisIdempotencyStoreTests
{
    private readonly Mock<IDistributedCache> _cacheMock = new();
    private readonly RedisIdempotencyStore _sut;

    public RedisIdempotencyStoreTests()
    {
        _sut = new RedisIdempotencyStore(_cacheMock.Object);
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenKeyExists()
    {
        // Arrange
        var key = Guid.NewGuid();
        _cacheMock.Setup(x => x.GetAsync(key.ToString(), default))
            .ReturnsAsync(Encoding.UTF8.GetBytes("some data"));

        // Act
        var result = await _sut.ExistsAsync(key);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenKeyDoesNotExist()
    {
        // Arrange
        var key = Guid.NewGuid();
        _cacheMock.Setup(x => x.GetAsync(key.ToString(), default))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _sut.ExistsAsync(key);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task StoreAsync_ShouldSetCacheValue()
    {
        // Arrange
        var key = Guid.NewGuid();
        var result = new { Success = true };

        // Act
        await _sut.StoreAsync(key, result);

        // Assert
        _cacheMock.Verify(x => x.SetAsync(
            key.ToString(),
            It.Is<byte[]>(b => Encoding.UTF8.GetString(b).Contains("true")),
            It.IsAny<DistributedCacheEntryOptions>(),
            default), Times.Once);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnDeserializedObject()
    {
        // Arrange
        var key = Guid.NewGuid();
        var expected = new TestResult { Value = "Test" };
        var json = JsonSerializer.Serialize(expected);
        _cacheMock.Setup(x => x.GetAsync(key.ToString(), default))
            .ReturnsAsync(Encoding.UTF8.GetBytes(json));

        // Act
        var result = await _sut.GetAsync<TestResult>(key);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().Be("Test");
    }

    [Fact]
    public async Task GetAsync_ShouldReturnDefault_WhenKeyNotFound()
    {
        // Arrange
        var key = Guid.NewGuid();
        _cacheMock.Setup(x => x.GetAsync(key.ToString(), default))
            .ReturnsAsync((byte[]?)null);

        // Act
        var result = await _sut.GetAsync<TestResult>(key);

        // Assert
        result.Should().BeNull();
    }

    public class TestResult
    {
        public string Value { get; set; } = string.Empty;
    }
}
