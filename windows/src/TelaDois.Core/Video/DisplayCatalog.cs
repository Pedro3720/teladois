using System.Runtime.InteropServices;

namespace TelaDois.Core.Video;

/// <summary>Um monitor como o <c>ddagrab</c> o enxerga: índice de saída DXGI do adaptador 0.</summary>
public sealed record DisplayOutput(int OutputIndex, string DeviceName, int Width, int Height, bool IsPrimary);

/// <summary>
/// Lista os monitores pela DXGI. A ordem das saídas DXGI <b>não</b> segue o <c>\\.\DISPLAYn</c>
/// (neste PC: saída 0 = DISPLAY2, saída 1 = DISPLAY1), por isso o índice vem daqui.
/// Usa só o adaptador 0, que é o que o <c>ddagrab</c> abre por padrão.
/// </summary>
public static unsafe class DisplayCatalog
{
    private static readonly Guid IidDxgiFactory1 = new("770aae78-f26f-4dba-a829-253c83d1b387");

    // IDXGIFactory::EnumAdapters, IDXGIAdapter::EnumOutputs e IDXGIOutput::GetDesc ficam todos no slot 7
    // (3 do IUnknown + 4 do IDXGIObject). Release é o slot 2.
    private const int EnumSlot = 7;
    private const int ReleaseSlot = 2;

    [DllImport("dxgi.dll")]
    private static extern int CreateDXGIFactory1(in Guid riid, out IntPtr factory);

    private struct OutputDesc
    {
        public fixed char DeviceName[32];
        public int Left, Top, Right, Bottom;
        public int AttachedToDesktop;
        public int Rotation;
        public IntPtr Monitor;
    }

    public static IReadOnlyList<DisplayOutput> Enumerate()
    {
        var outputs = new List<DisplayOutput>();
        if (!OperatingSystem.IsWindows() || CreateDXGIFactory1(IidDxgiFactory1, out var factory) < 0)
        {
            return outputs;
        }
        try
        {
            if (Enum(factory, 0, out var adapter) < 0)
            {
                return outputs;
            }
            try
            {
                for (uint index = 0; Enum(adapter, index, out var output) >= 0; index++)
                {
                    try
                    {
                        OutputDesc desc;
                        var getDesc = (delegate* unmanaged[Stdcall]<IntPtr, OutputDesc*, int>)(*(void***)output)[EnumSlot];
                        if (getDesc(output, &desc) >= 0 && desc.AttachedToDesktop != 0)
                        {
                            outputs.Add(new DisplayOutput(
                                (int)index,
                                new string(desc.DeviceName),
                                desc.Right - desc.Left,
                                desc.Bottom - desc.Top,
                                desc.Left == 0 && desc.Top == 0));
                        }
                    }
                    finally
                    {
                        Release(output);
                    }
                }
            }
            finally
            {
                Release(adapter);
            }
        }
        finally
        {
            Release(factory);
        }
        return outputs;
    }

    /// <summary>
    /// O monitor salvo, senão o primeiro não principal, senão o primeiro. Nunca <paramref name="excludedDeviceName"/>
    /// (o monitor virtual: duplicá-lo seria transmitir o próprio iPhone).
    /// </summary>
    public static DisplayOutput? Choose(IReadOnlyList<DisplayOutput> outputs, string? preferredDeviceName, string? excludedDeviceName = null)
    {
        var allowed = Without(outputs, excludedDeviceName);
        return allowed.FirstOrDefault(o => o.DeviceName == preferredDeviceName)
            ?? allowed.FirstOrDefault(o => !o.IsPrimary)
            ?? allowed.FirstOrDefault();
    }

    /// <summary><paramref name="outputs"/> sem o monitor de nome <paramref name="excludedDeviceName"/> (sem diferenciar maiúsculas).</summary>
    public static IReadOnlyList<DisplayOutput> Without(IReadOnlyList<DisplayOutput> outputs, string? excludedDeviceName) =>
        excludedDeviceName is null
            ? outputs
            : outputs.Where(o => !string.Equals(o.DeviceName, excludedDeviceName, StringComparison.OrdinalIgnoreCase)).ToList();

    private static int Enum(IntPtr obj, uint index, out IntPtr result)
    {
        IntPtr value;
        var call = (delegate* unmanaged[Stdcall]<IntPtr, uint, IntPtr*, int>)(*(void***)obj)[EnumSlot];
        var hr = call(obj, index, &value);
        result = hr >= 0 ? value : IntPtr.Zero;
        return hr;
    }

    private static void Release(IntPtr obj) =>
        ((delegate* unmanaged[Stdcall]<IntPtr, uint>)(*(void***)obj)[ReleaseSlot])(obj);
}
