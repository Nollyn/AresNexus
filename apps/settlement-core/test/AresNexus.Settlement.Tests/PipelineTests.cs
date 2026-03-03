using AresNexus.Settlement.Application.Commands;
using AresNexus.Settlement.Application.Interfaces;
using AresNexus.Settlement.Application.Validation;
using AresNexus.Settlement.Domain;
using AresNexus.Settlement.Infrastructure.Idempotency;
using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AresNexus.Settlement.Tests;

public class PipelineTests
{
    private readonly Mock<IIdempotencyStore> _idempotencyStoreMock = new();
    private readonly Mock<ILogger<CommandIdempotencyBehavior<ProcessTransactionCommand, bool>>> _idempotencyLoggerMock = new();
    private readonly Mock<ILogger<ValidationBehavior<ProcessTransactionCommand, bool>>> _validationLoggerMock = new();

    [Fact]
    public async Task IdempotencyBehavior_Hit_ShouldReturnCachedResult()
    {
        // Arrange
        var command = new ProcessTransactionCommand(Guid.NewGuid(), new Money(100), "DEPOSIT", Guid.NewGuid());
        _idempotencyStoreMock.Setup(x => x.ExistsAsync(command.IdempotencyKey)).ReturnsAsync(true);
        _idempotencyStoreMock.Setup(x => x.GetAsync<bool>(command.IdempotencyKey)).ReturnsAsync(true);

        var behavior = new CommandIdempotencyBehavior<ProcessTransactionCommand, bool>(
            _idempotencyStoreMock.Object, _idempotencyLoggerMock.Object);

        var nextCalled = false;
        Task<bool> Next() { nextCalled = true; return Task.FromResult(true); }
        RequestHandlerDelegate<bool> next = (CancellationToken _) => Next();

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        nextCalled.Should().BeFalse();
        _idempotencyStoreMock.Verify(x => x.GetAsync<bool>(command.IdempotencyKey), Times.Once);
    }

    [Fact]
    public async Task ValidationBehavior_NegativeAmount_ShouldThrowValidationException()
    {
        // Arrange
        // We bypass Money's negative check by using reflection or just testing the validator directly if Money throws.
        // Actually, Money(decimal amount) throws if < 0. 
        // Let's test the validator directly or try to create a command that fails validation.
        
        var validator = new ProcessTransactionCommandValidator();
        // Since Money constructor throws on negative, we can't easily create a Money with negative amount.
        // But we can test 0, which is also invalid according to the validator (GreaterThan(0)).
        
        var command = new ProcessTransactionCommand(Guid.NewGuid(), new Money(0), "DEPOSIT", Guid.NewGuid());
        
        var behavior = new ValidationBehavior<ProcessTransactionCommand, bool>(
            new[] { validator }, _validationLoggerMock.Object);

        Task<bool> Next() => Task.FromResult(true);
        RequestHandlerDelegate<bool> next = (CancellationToken _) => Next();

        // Act
        Func<Task> act = async () => await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
            .Where(e => e.Errors.Any(f => f.PropertyName == "Money.Amount"));
    }

    [Fact]
    public void Logging_TraceIdAndCorrelationId_ShouldBePresentInCommand()
    {
        // Arrange
        var traceId = "trace-123";
        var correlationId = "corr-456";
        var command = new ProcessTransactionCommand(
            Guid.NewGuid(), 
            new Money(100), 
            "DEPOSIT", 
            Guid.NewGuid(), 
            TraceId: traceId, 
            CorrelationId: correlationId);

        // Assert
        command.TraceId.Should().Be(traceId);
        command.CorrelationId.Should().Be(correlationId);
    }
}
