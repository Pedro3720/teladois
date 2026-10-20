namespace TelaDois.Core.Tests;

public class FileLogTests
{
    [Fact]
    public void WritesTimestampedLinesToTheFile()
    {
        var folder = Directory.CreateTempSubdirectory("teladois-log-");
        try
        {
            var path = Path.Combine(folder.FullName, "tela-dois.log");
            var log = new FileLog(path);
            log.Write("primeira");
            log.Write("segunda");
            var lines = File.ReadAllLines(path);
            Assert.Equal(2, lines.Length);
            Assert.EndsWith(" primeira", lines[0]);
            Assert.EndsWith(" segunda", lines[1]);
        }
        finally
        {
            folder.Delete(true);
        }
    }

    [Fact]
    public void NeverThrowsWhenTheFolderDoesNotExist()
    {
        var missing = Path.Combine(Path.GetTempPath(), "teladois-nao-existe-" + Guid.NewGuid().ToString("N"), "sub", "tela-dois.log");
        var log = new FileLog(missing);
        log.Write("perdida"); // a linha é descartada, o chamador (laço de envio, atalho, interface) segue
        Assert.False(File.Exists(missing));
    }

    [Fact]
    public void NeverThrowsWhenTheFileIsLockedByAnotherProgram()
    {
        var folder = Directory.CreateTempSubdirectory("teladois-log-");
        try
        {
            var path = Path.Combine(folder.FullName, "tela-dois.log");
            using (new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
            {
                new FileLog(path).Write("perdida");
            }
            Assert.Empty(File.ReadAllText(path));
            new FileLog(path).Write("volta a gravar");
            Assert.Contains("volta a gravar", File.ReadAllText(path));
        }
        finally
        {
            folder.Delete(true);
        }
    }

    [Fact]
    public void NeverThrowsWhenThePathIsADirectory()
    {
        var folder = Directory.CreateTempSubdirectory("teladois-log-");
        try
        {
            new FileLog(folder.FullName).Write("perdida"); // UnauthorizedAccessException
        }
        finally
        {
            folder.Delete(true);
        }
    }
}
