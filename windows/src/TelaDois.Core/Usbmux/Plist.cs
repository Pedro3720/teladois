using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace TelaDois.Core.Usbmux;

/// <summary>Plist XML mínimo (dict, string, integer, true/false, data, array) para falar com o usbmuxd.</summary>
public static class Plist
{
    private const string Header =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
        "<!DOCTYPE plist PUBLIC \"-//Apple//DTD PLIST 1.0//EN\" \"http://www.apple.com/DTDs/PropertyList-1.0.dtd\">\n";

    public static byte[] Serialize(IReadOnlyDictionary<string, object> dict)
    {
        var root = new XElement("plist", new XAttribute("version", "1.0"), ToElement(dict));
        return Encoding.UTF8.GetBytes(Header + root.ToString(SaveOptions.DisableFormatting) + "\n");
    }

    /// <summary>Lê o dict da raiz. Inteiros viram <see cref="long"/>.</summary>
    public static Dictionary<string, object> Parse(byte[] xml)
    {
        // As respostas da Apple trazem DOCTYPE: ignora a DTD em vez de recusar o documento.
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore };
        using var reader = XmlReader.Create(new MemoryStream(xml), settings);
        var root = XDocument.Load(reader).Root?.Elements().FirstOrDefault()
            ?? throw new InvalidDataException("plist vazio");
        return FromElement(root) as Dictionary<string, object>
            ?? throw new InvalidDataException("plist sem dict na raiz");
    }

    private static XElement ToElement(object value) => value switch
    {
        string text => new XElement("string", text),
        bool flag => new XElement(flag ? "true" : "false"),
        int or long or uint or ushort => new XElement("integer", Convert.ToInt64(value, CultureInfo.InvariantCulture)),
        byte[] bytes => new XElement("data", Convert.ToBase64String(bytes)),
        IReadOnlyDictionary<string, object> dict => new XElement("dict",
            dict.SelectMany(pair => new[] { new XElement("key", pair.Key), ToElement(pair.Value) })),
        IEnumerable<object> list => new XElement("array", list.Select(ToElement)),
        _ => throw new ArgumentException($"tipo não suportado no plist: {value.GetType()}"),
    };

    private static object FromElement(XElement element) => element.Name.LocalName switch
    {
        "string" => element.Value,
        "integer" => long.Parse(element.Value, CultureInfo.InvariantCulture),
        "true" => true,
        "false" => false,
        "data" => Convert.FromBase64String(string.Concat(element.Value.Where(c => !char.IsWhiteSpace(c)))),
        "dict" => ParseDict(element),
        "array" => element.Elements().Select(FromElement).ToList(),
        _ => throw new InvalidDataException($"elemento de plist não suportado: {element.Name}"),
    };

    private static Dictionary<string, object> ParseDict(XElement element)
    {
        var result = new Dictionary<string, object>();
        var children = element.Elements().ToList();
        for (var i = 0; i + 1 < children.Count; i += 2)
        {
            result[children[i].Value] = FromElement(children[i + 1]);
        }
        return result;
    }
}
