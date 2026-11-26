namespace NetEvolve.Schemas.Schematron.Tests.Unit.Loading.XInclude.Suite;

using System.Xml;
using System.Xml.Linq;
using NetEvolve.Arguments;

/// <summary>Reads <c>testdescr.xml</c>, the catalog of the W3C XInclude test suite.</summary>
internal static class XIncludeCatalogReader
{
    /// <summary>Reads the catalog of a suite.</summary>
    /// <param name="root">The suite root, the folder that holds <c>testdescr.xml</c>.</param>
    /// <returns>The cases in catalog order.</returns>
    /// <exception cref="InvalidDataException">A case has no id, no input, or an unknown type, or an id is used twice.</exception>
    public static IReadOnlyList<XIncludeCase> Read(string root)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(root);

        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null };
        using var reader = XmlReader.Create(Path.Combine(root, "testdescr.xml"), settings);
        var catalog = XDocument.Load(reader);

        var cases = new List<XIncludeCase>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var set in catalog.Descendants("testcases"))
        {
            var baseDir = Required(set, "basedir").Trim('/');
            var contributor = baseDir.Split('/')[0];
            foreach (var element in set.Elements("testcase"))
            {
                var xcase = ReadCase(element, baseDir, contributor);
                if (!seen.Add(xcase.Id))
                {
                    throw new InvalidDataException($"The case '{xcase.Id}' is listed twice.");
                }

                cases.Add(xcase);
            }
        }

        return cases;
    }

    private static XIncludeCase ReadCase(XElement element, string baseDir, string contributor)
    {
        var id = Required(element, "id");
        var type = Required(element, "type") switch
        {
            "success" => XIncludeCaseType.Success,
            "error" => XIncludeCaseType.Error,
            var other => throw new InvalidDataException($"The case '{id}' has the unknown type '{other}'."),
        };
        var features = ((string?)element.Attribute("features"))?.Split(
            [' ', '\t', '\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries
        );
        var output = (string?)element.Element("output");

        return new XIncludeCase(
            id,
            contributor,
            Combine(baseDir, Required(element, "href")),
            type,
            features ?? [],
            string.IsNullOrWhiteSpace(output) ? null : Combine(baseDir, output.Trim()),
            ((string?)element.Element("description") ?? string.Empty).Trim()
        );
    }

    private static string Required(XElement element, string attribute) =>
        (string?)element.Attribute(attribute) is { Length: > 0 } value
            ? value
            : throw new InvalidDataException($"A {element.Name.LocalName} has no '{attribute}' attribute.");

    // Resolves the reference against the base directory with the URI rules, so '..' segments collapse.
    private static string Combine(string baseDir, string reference) =>
        Uri.UnescapeDataString(new Uri(new Uri("http://h/" + baseDir + "/"), reference).AbsolutePath).TrimStart('/');
}
