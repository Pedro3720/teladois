using System.Collections.Concurrent;
using TelaDois.Core.Display;
using TelaDois.Core.Tests.Fakes;
using TelaDois.Core.Video;

namespace TelaDois.Core.Tests.Display;

public class ScheduledTaskSwitchTests
{
    /// <summary>Anota o executável de cada <c>Start</c> (o <see cref="FakeProcessRunner"/> só guarda os argumentos).</summary>
    private sealed class FileRecorder(IProcessRunner inner) : IProcessRunner
    {
        public ConcurrentQueue<string> Files { get; } = new();

        public IRunningProcess Start(string fileName, IReadOnlyList<string> arguments)
        {
            Files.Enqueue(fileName);
            return inner.Start(fileName, arguments);
        }
    }

    private static (ScheduledTaskSwitch Switch, FakeProcessRunner Runner, FileRecorder Files, ConcurrentQueue<string> Log) Create(
        Func<IReadOnlyList<string>, FakeProcess> script)
    {
        var runner = new FakeProcessRunner(script);
        var files = new FileRecorder(runner);
        var log = new ConcurrentQueue<string>();
        return (new ScheduledTaskSwitch(files, log.Enqueue), runner, files, log);
    }

    [Fact]
    public void TaskNamesMatchTheInstaller()
    {
        Assert.Equal(@"\TelaDois\Ligar monitor virtual", ScheduledTaskSwitch.EnableTask);
        Assert.Equal(@"\TelaDois\Desligar monitor virtual", ScheduledTaskSwitch.DisableTask);
    }

    [Fact]
    public async Task AvailableWhenBothQueriesSucceed()
    {
        var (device, runner, files, _) = Create(_ => new FakeProcess(0));

        Assert.True(await device.IsAvailableAsync(CancellationToken.None));

        Assert.Equal(
            new[] { new[] { "/query", "/tn", ScheduledTaskSwitch.EnableTask }, new[] { "/query", "/tn", ScheduledTaskSwitch.DisableTask } },
            runner.Started.Select(a => a.ToArray()).ToArray());
        Assert.All(files.Files, f => Assert.Equal("schtasks.exe", f));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task NotAvailableWhenOneQueryFails(bool enableMissing)
    {
        var missing = enableMissing ? ScheduledTaskSwitch.EnableTask : ScheduledTaskSwitch.DisableTask;
        var (device, _, _, _) = Create(args => new FakeProcess(args.Contains(missing) ? 1 : 0));

        Assert.False(await device.IsAvailableAsync(CancellationToken.None));
    }

    [Fact]
    public async Task NotAvailableWhenSchtasksDoesNotStart()
    {
        var (device, _, _, _) = Create(_ => throw new System.ComponentModel.Win32Exception(2));

        Assert.False(await device.IsAvailableAsync(CancellationToken.None));
    }

    [Theory]
    [InlineData(true, ScheduledTaskSwitch.EnableTask)]
    [InlineData(false, ScheduledTaskSwitch.DisableTask)]
    public async Task RunStartsTheMatchingTask(bool enable, string task)
    {
        var (device, runner, files, log) = Create(_ => new FakeProcess(0));

        Assert.True(await device.RunAsync(enable, CancellationToken.None));

        Assert.Equal(new[] { "/run", "/tn", task }, Assert.Single(runner.Started));
        Assert.Equal("schtasks.exe", Assert.Single(files.Files));
        Assert.Empty(log);
        Assert.Equal(1, runner.Disposed);
    }

    [Fact]
    public async Task RunWithNonZeroCodeIsFalseAndLogsOneLine()
    {
        var (device, _, _, log) = Create(_ => new FakeProcess(1, errors: "ERRO: o sistema não pode encontrar o arquivo especificado."));

        Assert.False(await device.RunAsync(true, CancellationToken.None));

        var line = Assert.Single(log);
        Assert.Contains(ScheduledTaskSwitch.EnableTask, line);
        Assert.Contains("código 1", line);
    }

    [Fact]
    public async Task RunWhenStartThrowsIsFalse()
    {
        var (device, _, _, log) = Create(_ => throw new System.ComponentModel.Win32Exception(2));

        Assert.False(await device.RunAsync(false, CancellationToken.None));

        Assert.Contains(ScheduledTaskSwitch.DisableTask, Assert.Single(log));
    }

    [Fact]
    public async Task RunThatHangsGivesUpAfterTheTimeout()
    {
        var runner = new FakeProcessRunner(_ => new FakeProcess(0, hangs: true));
        var log = new ConcurrentQueue<string>();
        var device = new ScheduledTaskSwitch(runner, log.Enqueue) { Timeout = TimeSpan.FromMilliseconds(200) };

        Assert.False(await device.RunAsync(true, CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)));

        Assert.Single(log);
        Assert.Equal(1, runner.Disposed); // o processo travado é encerrado
    }

    [Fact]
    public async Task CallerCancellationPropagates()
    {
        var (device, _, _, _) = Create(_ => new FakeProcess(0, hangs: true));
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => device.RunAsync(true, cts.Token));
    }
}
