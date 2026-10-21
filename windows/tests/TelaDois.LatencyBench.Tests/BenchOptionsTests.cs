using TelaDois.LatencyBench;

namespace TelaDois.LatencyBench.Tests;

public class BenchOptionsTests
{
    [Fact]
    public void DefaultsMeasureEverythingOnTheVirtualMonitor()
    {
        var options = BenchOptions.Parse([]);

        Assert.Null(options.Monitor);
        Assert.Equal(BenchVariants.All.Select(v => v.Name), options.Variants.Select(v => v.Name));
        Assert.Equal(["parado", "rolagem", "video"], options.Scenarios);
        Assert.Equal(TimeSpan.FromSeconds(15), options.Duration);
        // a pasta padrão fica na raiz do repositório (onde está o .git), não no diretório atual
        var root = BenchOptions.FindRepoRoot(AppContext.BaseDirectory)!;
        Assert.True(Directory.Exists(Path.Combine(root, ".git")) || File.Exists(Path.Combine(root, ".git")));
        Assert.Equal(Path.Combine(root, "builds", "bancada"), options.OutDir);
    }

    [Fact]
    public void DefaultOutDirClimbsToTheFolderWithGit()
    {
        var temp = Path.Combine(Path.GetTempPath(), "bancada-" + Guid.NewGuid().ToString("N"));
        try
        {
            var deep = Directory.CreateDirectory(Path.Combine(temp, "repo", "windows", "bin", "Release")).FullName;
            Directory.CreateDirectory(Path.Combine(temp, "repo", ".git"));

            var options = BenchOptions.Parse([], deep);

            Assert.Equal(Path.Combine(temp, "repo", "builds", "bancada"), options.OutDir);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public void GitAsAFileAlsoMarksTheRoot()
    {
        // worktree: .git é um arquivo
        var temp = Path.Combine(Path.GetTempPath(), "bancada-" + Guid.NewGuid().ToString("N"));
        try
        {
            var deep = Directory.CreateDirectory(Path.Combine(temp, "wt", "a")).FullName;
            File.WriteAllText(Path.Combine(temp, "wt", ".git"), "gitdir: x");

            Assert.Equal(Path.Combine(temp, "wt"), BenchOptions.FindRepoRoot(deep));
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public void WithoutGitTheDefaultIsUnderTheCurrentDirectory()
    {
        var temp = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "bancada-" + Guid.NewGuid().ToString("N"))).FullName;
        try
        {
            Assert.Null(BenchOptions.FindRepoRoot(temp));
            Assert.Equal(Path.GetFullPath(Path.Combine("builds", "bancada")), BenchOptions.Parse([], temp).OutDir);
        }
        finally
        {
            Directory.Delete(temp, recursive: true);
        }
    }

    [Fact]
    public void ReadsEveryOption()
    {
        var options = BenchOptions.Parse(
            ["--monitor", @"\\.\DISPLAY1", "--variants", "lowpower,base", "--scenarios", "rolagem", "--seconds", "5", "--out", "x"]);

        Assert.Equal(@"\\.\DISPLAY1", options.Monitor);
        Assert.Equal(["lowpower", "base"], options.Variants.Select(v => v.Name));
        Assert.Equal(["rolagem"], options.Scenarios);
        Assert.Equal(TimeSpan.FromSeconds(5), options.Duration);
        Assert.Equal(Path.GetFullPath("x"), options.OutDir);
    }

    [Fact]
    public void ListsIgnoreSpacesAndCase()
    {
        var options = BenchOptions.Parse(["--variants", " Base , gop4 ", "--scenarios", "VIDEO, parado"]);

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
