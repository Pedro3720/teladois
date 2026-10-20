using TelaDois.Core.Display;

namespace TelaDois.Core.Tests.Display;

[Trait("Category", "Local")]
public class KeyboardLayoutLocalTests
{
    [Fact]
    public void ClosingBracketHasAKeyWithoutModifiersOnThisKeyboard() =>
        // ABNT2 e US têm "]" numa tecla própria (em posições diferentes); o atalho usa a tecla certa de cada um.
        Assert.NotNull(KeyboardLayout.VirtualKeyFor(']'));
}
