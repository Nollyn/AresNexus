using AresNexus.Services.Settlement.Application.Commands;
using AresNexus.Services.Settlement.Application.Validation;
using AresNexus.Services.Settlement.Domain;
using FluentValidation.TestHelper;
using Xunit;

namespace AresNexus.Tests.Unit;

public class ProcessTransactionCommandValidatorTests
{
    private readonly ProcessTransactionCommandValidator _validator = new();

    [Fact]
    public void Should_HaveError_When_AccountIdIsEmpty()
    {
        var command = new ProcessTransactionCommand(Guid.Empty, new Money(100), "DEPOSIT", Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AccountId);
    }

    [Fact]
    public void Should_HaveError_When_AmountIsZero()
    {
        var command = new ProcessTransactionCommand(Guid.NewGuid(), new Money(0), "DEPOSIT", Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Money.Amount);
    }

    [Fact]
    public void Should_HaveError_When_TransactionTypeIsEmpty()
    {
        var command = new ProcessTransactionCommand(Guid.NewGuid(), new Money(100), "", Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TransactionType);
    }

    [Fact]
    public void Should_HaveError_When_TransactionTypeIsInvalid()
    {
        var command = new ProcessTransactionCommand(Guid.NewGuid(), new Money(100), "INVALID", Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.TransactionType)
            .WithErrorMessage("TransactionType must be DEPOSIT or WITHDRAW");
    }

    [Theory]
    [InlineData("DEPOSIT")]
    [InlineData("WITHDRAW")]
    [InlineData("deposit")]
    [InlineData("withdraw")]
    public void Should_NotHaveError_When_TransactionTypeIsValid(string type)
    {
        var command = new ProcessTransactionCommand(Guid.NewGuid(), new Money(100), type, Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveValidationErrorFor(x => x.TransactionType);
    }

    [Fact]
    public void Should_NotHaveError_When_CommandIsValid()
    {
        var command = new ProcessTransactionCommand(Guid.NewGuid(), new Money(100), "DEPOSIT", Guid.NewGuid());
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
