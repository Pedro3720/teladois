using TelaDois.Core.Display;

namespace TelaDois.Core.Tests.Fakes;

/// <summary>
/// Windows de mentira: lista de monitores, modos e posições; <see cref="Apply"/> registra a chamada e liga/desliga.
/// <see cref="Devices"/> devolve uma cópia tirada sob <c>lock (DeviceList)</c>: quem muda a lista de outra thread (ex.: o
/// dispositivo do driver aparecendo depois de um tempo) faz isso sob o mesmo lock.
/// </summary>
internal sealed class FakeDisplayApi : IDisplayApi
{
    public List<DisplayDeviceInfo> DeviceList { get; } = [];
    public Dictionary<string, List<DisplayModeInfo>> ModeList { get; } = [];
    public Dictionary<string, DisplayBounds> BoundsList { get; } = [];
    public List<(string Device, int X, int Y, int Width, int Height, int Frequency)> Applied { get; } = [];

    /// <summary>Código devolvido pelo <see cref="Apply"/> (0 = sucesso).</summary>
    public int Result { get; set; }

    public IReadOnlyList<DisplayDeviceInfo> Devices()
    {
        lock (DeviceList)
        {
            return [.. DeviceList];
        }
    }

    public IReadOnlyList<DisplayModeInfo> Modes(string deviceName) =>
        ModeList.TryGetValue(deviceName, out var modes) ? modes : [];

    public DisplayBounds? Bounds(string deviceName) =>
        BoundsList.TryGetValue(deviceName, out var bounds) ? bounds : null;

    public int Apply(string deviceName, int x, int y, int width, int height, int frequency)
    {
        Applied.Add((deviceName, x, y, width, height, frequency));
        if (Result == 0)
        {
            // Nome desconhecido: o Windows de verdade também não tem o que mudar (e o falso não pode estourar).
            lock (DeviceList)
            {
                var index = DeviceList.FindIndex(d => d.DeviceName == deviceName);
                if (index >= 0)
                {
                    DeviceList[index] = DeviceList[index] with { AttachedToDesktop = width > 0 };
                }
            }
        }
        return Result;
    }
}
