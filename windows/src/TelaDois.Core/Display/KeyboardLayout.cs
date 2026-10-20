using System.Runtime.InteropServices;

namespace TelaDois.Core.Display;

public static class KeyboardLayout
{
    /// <summary>
    /// Tecla virtual que digita <paramref name="key"/> no layout de teclado atual sem Shift/Ctrl/Alt
    /// (no ABNT2 o "]" fica numa tecla diferente da do US); <c>null</c> se não houver.
    /// </summary>
    public static int? VirtualKeyFor(char key)
    {
        var result = VkKeyScanEx(key, GetKeyboardLayout(0));
        if (result == -1)
        {
            return null;
        }
        var modifiers = (result >> 8) & 0xFF;
        return modifiers == 0 ? result & 0xFF : null;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "VkKeyScanExW")]
    private static extern short VkKeyScanEx(char ch, IntPtr layout);

    [DllImport("user32.dll")]
    private static extern IntPtr GetKeyboardLayout(uint threadId);
}
