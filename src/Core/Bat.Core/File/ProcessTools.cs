using System.Diagnostics;

namespace Bat.Core;

/// <summary>Result of <see cref="ProcessTools.RunAsync(string, IEnumerable{string}, TimeSpan?, CancellationToken)"/>.</summary>
public sealed record ProcessRunResult(int ExitCode, string StandardOutput, string StandardError, bool TimedOut)
{
    public bool Succeeded => !TimedOut && ExitCode == 0;
}

/// <summary>
/// Runs an external program and captures its output with the .NET 11 <c>Process.RunAndCaptureTextAsync</c> API
/// (no deadlocks on full stdout/stderr buffers, no manual event wiring). Works on Windows and Linux.
/// </summary>
public static class ProcessTools
{
    /// <summary>
    /// Runs <paramref name="fileName"/> with <paramref name="arguments"/> (passed as separate arguments, no shell parsing or quoting issues).
    /// The process is killed when <paramref name="timeout"/> elapses (<see cref="ProcessRunResult.TimedOut"/> = <c>true</c>);
    /// cancelling <paramref name="cancellationToken"/> kills it and throws <see cref="OperationCanceledException"/>.
    /// </summary>
    public static async Task<ProcessRunResult> RunAsync(string fileName, IEnumerable<string> arguments = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        using var timeoutCts = timeout is null ? null : new CancellationTokenSource(timeout.Value);
        using var linked = timeoutCts is null
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            var output = await Process.RunAndCaptureTextAsync(fileName, arguments ?? [], linked.Token);
            return new ProcessRunResult(output.ExitStatus.ExitCode, output.StandardOutput, output.StandardError, TimedOut: false);
        }
        catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested == true && !cancellationToken.IsCancellationRequested)
        {
            return new ProcessRunResult(-1, string.Empty, string.Empty, TimedOut: true);
        }
    }
}