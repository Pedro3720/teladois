using System.Runtime.InteropServices;
using TelaDois.Core.Display;

namespace TelaDois.Tray;

/// <summary>Atalho global Win+Shift+&lt;tecla&gt;; a tecla é achada pelo caractere no layout atual.</summary>
internal sealed class Hotkey : NativeWindow, IDisposable
{
    private const int WmHotkey = 0x0312;
    private const uint ModShift = 0x4;
    private const uint ModWin = 0x8;
    private const uint ModNoRepeat = 0x4000;
    private const int Id = 1;

    private readonly Action _pressed;

    public Hotkey(char key, Action pressed)
    {
        _pressed = pressed;
        CreateHandle(new CreateParams());
        Registered = KeyboardLayout.VirtualKeyFor(key) is { } vk && RegisterHotKey(Handle, Id, ModWin | ModShift | ModNoRepeat, (uint)vk);
    }

    /// <summary>false se outro programa já usa o atalho ou a tecla não existe neste teclado.</summary>
    public bool Registered { get; }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmHotkey && m.WParam == Id)
        {
            _pressed();
        }
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        if (Registered)
        {
            UnregisterHotKey(Handle, Id);
        }
        DestroyHandle();
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint modifiers, uint vk);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
