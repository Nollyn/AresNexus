namespace AresNexus.Shared.Kernel;

/// <summary>
/// Defines standard transaction types.
/// </summary>
public static class TransactionTypes
{
    /// <summary>Deposit transaction.</summary>
    public const string Deposit = "DEPOSIT";
    /// <summary>Withdrawal transaction.</summary>
    public const string Withdraw = "WITHDRAW";
}

/// <summary>
/// Defines standard currency codes.
/// </summary>
public static class CurrencyConstants
{
    /// <summary>Swiss Franc.</summary>
    public const string Chf = "CHF";
}

/// <summary>
/// Defines system-wide constants.
/// </summary>
public static class SystemConstants
{
    /// <summary>The default system user name.</summary>
    public const string SystemUser = "SYSTEM";
}

/// <summary>
/// Defines common security-related constants.
/// </summary>
public static class SecurityConstants
{
    /// <summary>The name of the key used for settlement encryption.</summary>
    public const string SettlementKey = "AresNexus-Settle-Key";
}
