namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf.Output;

using System.Xml;
using System.Xml.Linq;

/// <summary>Reads the <c>OUTPUT</c> reference of the catalog entries of the W3C XML Conformance Test Suite.</summary>
internal static class XmlConfOutputReference
{
    /// <summary>Reads the output file of every <c>TEST</c> element that has an <c>OUTPUT</c> attribute.</summary>
    /// <param name="suiteRoot">The suite root directory that holds <c>xmlconf.xml</c>.</param>
    /// <returns>The path of the output file relative to the suite root, with <c>/</c> separators, by case id (see <see cref="XmlConfCase.Id"/>).</returns>
    public static IReadOnlyDictionary<string, string> Read(string suiteRoot)
    {
        ArgumentNullException.ThrowIfNull(suiteRoot);
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Parse,
            XmlResolver = new XmlUrlResolver(),
        };
        XDocument catalog;
        using (var reader = XmlReader.Create(Path.Combine(suiteRoot, "xmlconf.xml"), settings))
        {
            catalog = XDocument.Load(reader);
        }

        var outputs = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var test in catalog.Descendants("TEST"))
        {
            if ((string?)test.Attribute("OUTPUT") is not { } output)
            {
                continue;
            }

            var baseUri = EffectiveBase(test);
            var id = (string?)test.Attribute("ID") ?? throw new InvalidDataException("A TEST element has no ID.");
            outputs.Add($"{baseUri.TrimEnd('/').Replace('/', '-')}/{id}", baseUri + output);
        }

        return outputs;
    }

    private static string EffectiveBase(XElement test)
    {
        var parts = new List<string>();
        for (var parent = test.Parent; parent is not null; parent = parent.Parent)
        {
            if ((string?)parent.Attribute(XNamespace.Xml + "base") is { } value)
            {
                parts.Insert(0, value);
            }
        }

        return string.Concat(parts);
    }
}
