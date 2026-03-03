namespace AresNexus.Settlement.Domain;

/// <summary>
/// Thrown when an operation is attempted on a locked account.
/// </summary>
public sealed class AccountLockedException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AccountLockedException"/> class.
    /// </summary>
    /// <param name="accountId">The account identifier.</param>
    public AccountLockedException(Guid accountId)
        : base($"Account {accountId} is locked.")
    {
    }
}
