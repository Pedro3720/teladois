using System.Diagnostics;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Video;

/// <summary>Usa processos de verdade do Windows (cmd.exe, ping.exe); o CI roda em windows-latest.</summary>
public class SystemProcessRunnerTests
{
    private static readonly SystemProcessRunner Runner = new();

    [Fact]
    public async Task ReturnsExitCode()
    {
        await using var process = Runner.Start("cmd.exe", ["/c", "exit 3"]);
        Assert.Equal(3, await process.WaitForExitAsync(CancellationToken.None));
    }

    [Fact]
    public async Task KeepsStderrInRecentErrors()
    {
        await using var process = Runner.Start("cmd.exe", ["/c", "echo erro 1>&2 & exit 1"]);
        Assert.Equal(1, await process.WaitForExitAsync(CancellationToken.None));
        Assert.Contains("erro", process.RecentErrors);
    }

    [Fact]
    public async Task DisposeKillsLongRunningProcessQuickly()
    {
        var before = DateTime.Now.AddSeconds(-1);
        var process = Runner.Start("ping.exe", ["-n", "30", "127.0.0.1"]);
        var clock = Stopwatch.StartNew();

        await process.DisposeAsync();

        Assert.True(clock.Elapsed < TimeSpan.FromSeconds(10), $"DisposeAsync demorou {clock.Elapsed}");
        Assert.DoesNotContain(Process.GetProcessesByName("ping"), p => StartedAfter(p, before));
        await process.DisposeAsync(); // idempotente
    }

    private static bool StartedAfter(Process p, DateTime moment)
    {
        try
        {
            return !p.HasExited && p.StartTime >= moment;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
