using System.Text;
using TelaDois.Core.Usbmux;

namespace TelaDois.Core.Tests;

public class PlistTests
{
    [Fact]
    public void SerializesNestedDictionary()
    {
        var xml = Encoding.UTF8.GetString(Plist.Serialize(new Dictionary<string, object>
        {
            ["MessageType"] = "Connect",
            ["DeviceID"] = 3,
            ["Props"] = new Dictionary<string, object> { ["A"] = "b" },
        }));
        Assert.StartsWith("<?xml", xml);
        Assert.Contains("<key>MessageType</key><string>Connect</string>", xml);
        Assert.Contains("<key>DeviceID</key><integer>3</integer>", xml);
        Assert.Contains("<key>Props</key><dict><key>A</key><string>b</string></dict>", xml);
    }

    [Fact]
    public void ParsesApplePlistWithDoctype()
    {
        var xml = """
            <?xml version="1.0" encoding="UTF-8"?>
            <!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
            <plist version="1.0">
            <dict>
            	<key>MessageType</key>
            	<string>Attached</string>
            	<key>DeviceID</key>
            	<integer>7</integer>
            	<key>Properties</key>
            	<dict>
            		<key>ConnectionType</key>
            		<string>USB</string>
            	</dict>
            </dict>
            </plist>
            """;
        var body = Plist.Parse(Encoding.UTF8.GetBytes(xml));
        Assert.Equal("Attached", body["MessageType"]);
        Assert.Equal(7L, body["DeviceID"]);
        var properties = Assert.IsType<Dictionary<string, object>>(body["Properties"]);
        Assert.Equal("USB", properties["ConnectionType"]);
    }

    [Fact]
    public void RoundTripsBooleansDataAndArrays()
    {
        var body = Plist.Parse(Plist.Serialize(new Dictionary<string, object>
        {
            ["Yes"] = true,
            ["Bytes"] = new byte[] { 1, 2, 3 },
            ["List"] = new List<object> { "a", 2 },
        }));
        Assert.True((bool)body["Yes"]);
        Assert.Equal(new byte[] { 1, 2, 3 }, body["Bytes"]);
        Assert.Equal(new List<object> { "a", 2L }, body["List"]);
    }
}
