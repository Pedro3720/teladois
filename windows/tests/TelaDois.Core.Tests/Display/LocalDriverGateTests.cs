namespace TelaDois.Core.Tests.Display;

public class LocalDriverGateTests
{
    [Theory]
    [InlineData("1", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("0", false)]
    [InlineData("true", false)]
    [InlineData(" 1", false)]
    public void OnlyTheValueOneEnablesTheRealDriverTest(string? value, bool expected) =>
        Assert.Equal(expected, LocalDriverGate.IsEnabled(value));
}
