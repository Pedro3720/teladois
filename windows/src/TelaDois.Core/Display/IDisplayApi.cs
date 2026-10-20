namespace TelaDois.Core.Display;

/// <summary>Um monitor do Windows (<c>EnumDisplayDevices</c>): nome <c>\\.\DISPLAYn</c> e o adaptador que o fornece.</summary>
public sealed record DisplayDeviceInfo(string DeviceName, string Description, bool AttachedToDesktop, bool IsPrimary);

public readonly record struct DisplayModeInfo(int Width, int Height, int Frequency);

/// <summary>Posição e tamanho de um monitor no desktop.</summary>
public readonly record struct DisplayBounds(int X, int Y, int Width, int Height);

/// <summary>As chamadas de monitor do Windows que o TelaDois usa (atrás de uma interface para testar sem Windows).</summary>
public interface IDisplayApi
{
    IReadOnlyList<DisplayDeviceInfo> Devices();

    IReadOnlyList<DisplayModeInfo> Modes(string deviceName);

    /// <summary>Posição e tamanho atuais; <c>null</c> se o monitor não está no desktop.</summary>
    DisplayBounds? Bounds(string deviceName);

    /// <summary>Põe o monitor no desktop nessa posição e modo (largura 0 = tira do desktop). 0 = sucesso; senão o código <c>DISP_CHANGE_*</c>.</summary>
    int Apply(string deviceName, int x, int y, int width, int height, int frequency);
}

public sealed class DisplayChangeException(int code, string action)
    : Exception($"o Windows não deixou {action} o monitor virtual (código {code})")
{
    public int Code { get; } = code;
}
