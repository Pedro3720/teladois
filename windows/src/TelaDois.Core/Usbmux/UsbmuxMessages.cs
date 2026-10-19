namespace TelaDois.Core.Usbmux;

public abstract record UsbmuxEvent;

/// <summary>O usbmuxd aceitou o Listen (o serviço da Apple está de pé).</summary>
public sealed record ListenStarted : UsbmuxEvent;

public sealed record DeviceAttached(int DeviceId, string SerialNumber, string ConnectionType) : UsbmuxEvent;

public sealed record DeviceDetached(int DeviceId) : UsbmuxEvent;

public static class UsbmuxMessages
{
    public const int ResultOk = 0;
    public const int ResultConnectionRefused = 3;

    public static Dictionary<string, object> Listen() => Request("Listen");

    public static Dictionary<string, object> Connect(int deviceId, ushort port)
    {
        var request = Request("Connect");
        request["DeviceID"] = deviceId;
        request["PortNumber"] = (int)UsbmuxPacket.NetworkPort(port);
        return request;
    }

    /// <summary>Código de uma resposta <c>Result</c>; <c>null</c> se não for uma.</summary>
    public static long? ResultCode(IReadOnlyDictionary<string, object> body) =>
        body.TryGetValue("MessageType", out var type) && type is "Result"
        && body.TryGetValue("Number", out var number) && number is long code
            ? code
            : null;

    public static UsbmuxEvent? ParseEvent(IReadOnlyDictionary<string, object> body)
    {
        if (!body.TryGetValue("MessageType", out var type) || !body.TryGetValue("DeviceID", out var idValue) || idValue is not long id)
        {
            return null;
        }
        return type switch
        {
            "Attached" => new DeviceAttached((int)id, Property(body, "SerialNumber"), Property(body, "ConnectionType")),
            "Detached" => new DeviceDetached((int)id),
            _ => null,
        };
    }

    private static string Property(IReadOnlyDictionary<string, object> body, string key) =>
        body.TryGetValue("Properties", out var value) && value is IReadOnlyDictionary<string, object> properties
        && properties.TryGetValue(key, out var property) && property is string text
            ? text
            : "";

    private static Dictionary<string, object> Request(string type) => new()
    {
        ["MessageType"] = type,
        ["ClientVersionString"] = "TelaDois",
        ["ProgName"] = "TelaDois",
        ["kLibUSBMuxVersion"] = 3,
    };
}
