namespace Allog.Cli;

/// <summary>Process exit codes, documented in <c>allog --help</c>.</summary>
internal static class ExitCodes
{
    public const int Ok = 0;
    public const int Error = 1;
    public const int Usage = 2;
    public const int CompletedWithCsvFailures = 3;
    public const int Cancelled = 130;
}
