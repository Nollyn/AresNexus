using AresNexus.Settlement.Application.Commands;
using AresNexus.Settlement.Application.Handlers;
using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Domain;
using AresNexus.Settlement.Domain.Aggregates;
using AresNexus.Shared.Kernel;
using FluentAssertions;
using Moq;
using System.Diagnostics.Metrics;
using Xunit;

namespace AresNexus.Tests.Unit;

public class ProcessTransactionCommandHandlerTests
{
    private readonly Mock<IAccountRepository> _repositoryMock;
    private readonly Mock<IEncryptionService> _encryptionServiceMock;
    private readonly Mock<IKeyVaultClient> _keyVaultClientMock;
    private readonly Meter _meter;
    private readonly ProcessTransactionCommandHandler _handler;

    public ProcessTransactionCommandHandlerTests()
    {
        _repositoryMock = new Mock<IAccountRepository>();
        _encryptionServiceMock = new Mock<IEncryptionService>();
        _keyVaultClientMock = new Mock<IKeyVaultClient>();
        _meter = new Meter("TestMeter");
        _handler = new ProcessTransactionCommandHandler(
            _repositoryMock.Object,
            _encryptionServiceMock.Object,
            _keyVaultClientMock.Object,
            _meter);
    }

    [Fact]
    public async Task Handle_Deposit_ShouldCallRepositorySave()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var command = new ProcessTransactionCommand(
            accountId,
            new Money(100),
            TransactionTypes.Deposit,
            Guid.NewGuid(),
            "Test Reference");

        var account = new Account(accountId, "Test User");
        _repositoryMock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        _encryptionServiceMock.Setup(e => e.EncryptAsync(It.IsAny<string>()))
            .ReturnsAsync("encrypted_1");
        _keyVaultClientMock.Setup(k => k.EncryptAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("encrypted_2");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.SaveAsync(
            It.Is<Account>(a => a.Id == accountId && a.Balance.Amount == 100),
            It.IsAny<IEnumerable<object>>(),
            It.IsAny<CancellationToken>()), Times.Once);
        
        _encryptionServiceMock.Verify(e => e.EncryptAsync("Test Reference"), Times.Once);
        _keyVaultClientMock.Verify(k => k.EncryptAsync("encrypted_1", SecurityConstants.SettlementKey), Times.Once);
    }

    [Fact]
    public async Task Handle_Withdraw_ShouldCallRepositorySave()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var command = new ProcessTransactionCommand(
            accountId,
            new Money(40),
            TransactionTypes.Withdraw,
            Guid.NewGuid());

        var account = new Account(accountId, "Test User");
        account.Deposit(new Money(100)); // Ensure enough funds
        
        _repositoryMock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(account);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.SaveAsync(
            It.Is<Account>(a => a.Id == accountId && a.Balance.Amount == 60),
            It.IsAny<IEnumerable<object>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownTransactionType_ShouldThrow()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var command = new ProcessTransactionCommand(
            accountId,
            new Money(100),
            "UNKNOWN",
            Guid.NewGuid());

        _repositoryMock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Account(accountId, "Test User"));

        // Act
        var act = () => _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task Handle_WhenAccountNotFound_ShouldCreateNewAccountAndReturnTrue()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var command = new ProcessTransactionCommand(
            accountId,
            new Money(100),
            TransactionTypes.Deposit,
            Guid.NewGuid());

        _repositoryMock.Setup(r => r.GetByIdAsync(accountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Account?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _repositoryMock.Verify(r => r.SaveAsync(
            It.Is<Account>(a => a.Id == accountId && a.Balance.Amount == 100),
            It.IsAny<IEnumerable<object>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
