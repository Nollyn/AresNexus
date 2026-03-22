namespace AresNexus.Settlement.Domain;

/// <summary>
/// Thrown when an operation is attempted on a locked account.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AccountLockedException"/> class.
/// </remarks>
/// <param name="accountId">The account identifier.</param>
public sealed class AccountLockedException(Guid accountId) : Exception($"Account {accountId} is locked.");
