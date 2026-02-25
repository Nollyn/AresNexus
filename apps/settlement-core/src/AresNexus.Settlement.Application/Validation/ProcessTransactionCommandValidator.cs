using AresNexus.Settlement.Application.Commands;
using FluentValidation;

namespace AresNexus.Settlement.Application.Validation;

/// <summary>
/// Validates <see cref="ProcessTransactionCommand"/>.
/// </summary>
public sealed class ProcessTransactionCommandValidator : AbstractValidator<ProcessTransactionCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProcessTransactionCommandValidator"/> class.
    /// </summary>
    public ProcessTransactionCommandValidator()
    {
        RuleFor(x => x.AccountId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.TransactionType)
            .NotEmpty()
            .Must(t => t.Equals("DEPOSIT", StringComparison.OrdinalIgnoreCase) || t.Equals("WITHDRAW", StringComparison.OrdinalIgnoreCase))
            .WithMessage("TransactionType must be DEPOSIT or WITHDRAW");
    }
}
