using TelaDois.LatencyBench;

namespace TelaDois.LatencyBench.Tests;

public class BenchOptionsTests
{
    // Pasta temporária com um .git falso: os testes nunca dependem do repositório de verdade.
    private sealed class TempRepo : IDisposable
    {
        public TempRepo(bool withGit = true)
        {
            Root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "bancada-" + Guid.NewGuid().ToString("N"))).FullName;
            if (withGit)
            {
                Directory.CreateDirectory(Path.Combine(Root, ".git"));
            }
            Deep = Directory.CreateDirectory(Path.Combine(Root, "windows", "bin", "Release")).FullName;
        }

        public string Root { get; }

        public string Deep { get; }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }

    [Fact]
    public void DefaultsMeasureEverythingOnTheVirtualMonitor()
    {
        using var repo = new TempRepo();

        var options = BenchOptions.Parse([], repo.Deep);

        Assert.Null(options.Monitor);
        Assert.Equal(BenchVariants.All.Select(v => v.Name), options.Variants.Select(v => v.Name));
        Assert.Equal(["parado", "rolagem", "video"], options.Scenarios);
        Assert.Equal(TimeSpan.FromSeconds(15), options.Duration);
        // a pasta padrão fica na raiz do repositório (onde está o .git), não no diretório atual
        Assert.Equal(Path.Combine(repo.Root, "builds", "bancada"), options.OutDir);
    }

    [Fact]
    public void TheVirtualMonitorDefaultsToTheAppSize()
    {
        var options = BenchOptions.Parse(["--out", "x"]);

        // Igual ao app (Etapa 5d): iPhone 2556×1179 ÷ 1,75 = 1460×674, em escala 100%.
        Assert.Equal((1460, 674), options.MonitorSize);
        Assert.Equal(TelaDois.Core.Display.VirtualMonitorSize.For(2556, 1179), options.MonitorSize);
    }

    [Theory]
    [InlineData("2556x1179", 2556, 1179)]
    [InlineData("1920×886", 1920, 886)]
    [InlineData("1460X674", 1460, 674)]
    public void MonitorSizeAcceptsWidthByHeight(string value, int width, int height)
    {
        Assert.Equal((width, height), BenchOptions.Parse(["--monitor-size", value, "--out", "x"]).MonitorSize);
    }

    [Fact]
    public void MonitorSizeOnlyAppliesToTheVirtualMonitor()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            BenchOptions.Parse(["--monitor", @"\\.\DISPLAY1", "--monitor-size", "1460x674", "--out", "x"]));
        Assert.Contains("--monitor-size", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void EachScenarioHasItsOwnDefaultDuration()
    {
        using var repo = new TempRepo();

        var options = BenchOptions.Parse([], repo.Deep);

        Assert.False(options.DurationGiven);
        Assert.Equal(TimeSpan.FromSeconds(60), options.DurationFor("parado"));
        Assert.Equal(TimeSpan.FromSeconds(15), options.DurationFor("rolagem"));
        Assert.Equal(TimeSpan.FromSeconds(15), options.DurationFor("video"));
    }

    [Fact]
    public void ExplicitSecondsApplyToEveryScenario()
    {
        var options = BenchOptions.Parse(["--seconds", "5", "--out", "x"]);

        Assert.True(options.DurationGiven);
        Assert.Equal(TimeSpan.FromSeconds(5), options.DurationFor("parado"));
        Assert.Equal(TimeSpan.FromSeconds(5), options.DurationFor("rolagem"));
    }

    [Fact]
    public void GitAsAFileAlsoMarksTheRoot()
    {
        // worktree: .git é um arquivo
        using var repo = new TempRepo(withGit: false);
        File.WriteAllText(Path.Combine(repo.Root, ".git"), "gitdir: x");

        Assert.Equal(repo.Root, BenchOptions.FindRepoRoot(repo.Deep));
    }

    [Fact]
    public void WithoutARepositoryTheDefaultFolderIsRefused()
    {
        using var outside = new TempRepo(withGit: false);

        Assert.Null(BenchOptions.FindRepoRoot(outside.Deep));
        var error = Assert.Throws<ArgumentException>(() => BenchOptions.Parse([], outside.Deep));
        Assert.Contains("--out", error.Message);
    }

    [Fact]
    public void WithoutARepositoryAnExplicitOutIsFine()
    {
        using var outside = new TempRepo(withGit: false);
        var target = Path.Combine(outside.Root, "medidas");

        Assert.Equal(target, BenchOptions.Parse(["--out", target], outside.Deep).OutDir);
    }

    [Fact]
    public void ReadsEveryOption()
    {
        var options = BenchOptions.Parse(
            ["--monitor", @"\\.\DISPLAY1", "--variants", "lowpower,base", "--scenarios", "rolagem", "--seconds", "5", "--out", "x", "--seed", "123"]);

        Assert.Equal(@"\\.\DISPLAY1", options.Monitor);
        Assert.Equal(["lowpower", "base"], options.Variants.Select(v => v.Name));
        Assert.Equal(["rolagem"], options.Scenarios);
        Assert.Equal(TimeSpan.FromSeconds(5), options.Duration);
        Assert.Equal(Path.GetFullPath("x"), options.OutDir);
        Assert.Equal(123, options.Seed);
    }

    [Fact]
    public void WithoutSeedTheBenchChoosesOne()
    {
        Assert.Null(BenchOptions.Parse(["--out", "x"]).Seed);
    }

    [Fact]
    public void ListsIgnoreSpacesAndCase()
    {
        var options = BenchOptions.Parse(["--variants", " Base , gop4 ", "--scenarios", "VIDEO, parado", "--out", "x"]);

        Assert.Equal(["base", "gop4"], options.Variants.Select(v => v.Name));
        Assert.Equal(["video", "parado"], options.Scenarios);
    }

    [Theory]
    [InlineData("--variants", "nada")]
    [InlineData("--scenarios", "corrida")]
    [InlineData("--seconds", "0")]
    [InlineData("--seconds", "abc")]
    [InlineData("--seconds", "601")]
    [InlineData("--monitor", "DISPLAY1")]
    [InlineData("--variants", "")]
    [InlineData("--seed", "-1")]
    [InlineData("--seed", "abc")]
    [InlineData("--monitor-size", "1460")]
    [InlineData("--monitor-size", "abcxdef")]
    [InlineData("--monitor-size", "0x674")]
    [InlineData("--monitor-size", "1460x-2")]
    [InlineData("--monitor-size", "99999x674")]
    [InlineData("--desconhecida", "1")]
    public void RejectsBadValuesWithAMessageInPortuguese(string flag, string value)
    {
        var error = Assert.Throws<ArgumentException>(() => BenchOptions.Parse([flag, value]));
        Assert.False(string.IsNullOrWhiteSpace(error.Message));
    }

    [Fact]
    public void RejectsAFlagWithoutValue()
    {
        Assert.Throws<ArgumentException>(() => BenchOptions.Parse(["--seconds"]));
    }
}
