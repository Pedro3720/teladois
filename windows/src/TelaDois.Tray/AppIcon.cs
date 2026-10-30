namespace TelaDois.Tray;

/// <summary>Ícone do TelaDois (o mesmo do .exe), no tamanho que o Windows usa na bandeja.</summary>
internal static class AppIcon
{
    public static Icon ForTray()
    {
        using var stream = typeof(AppIcon).Assembly.GetManifestResourceStream("TelaDois.ico");
        return stream is null ? (Icon)SystemIcons.Application.Clone() : new Icon(stream, SystemInformation.SmallIconSize);
    }
}
