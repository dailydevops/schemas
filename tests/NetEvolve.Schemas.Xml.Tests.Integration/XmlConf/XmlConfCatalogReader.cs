namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

using System.Xml;
using System.Xml.Linq;

/// <summary>Reads the catalog <c>xmlconf.xml</c> of the W3C XML Conformance Test Suite.</summary>
internal static class XmlConfCatalogReader
{
    private static readonly char[] Separators = [' ', '\t', '\r', '\n'];

    /// <summary>Reads every <c>TEST</c> element of the catalog, following the external entities that include the test files.</summary>
    /// <param name="suiteRoot">The suite root directory that holds <c>xmlconf.xml</c>.</param>
    /// <returns>Every case in catalog order.</returns>
    /// <exception cref="InvalidDataException">A case does not follow the catalog format or repeats an id.</exception>
    public static IReadOnlyList<XmlConfCase> Read(string suiteRoot)
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

        var cases = new List<XmlConfCase>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var test in catalog.Descendants("TEST"))
        {
            var xmlConfCase = ReadCase(test);
            if (!ids.Add(xmlConfCase.Id))
            {
                throw new InvalidDataException($"Case '{xmlConfCase.Id}' is listed twice.");
            }

            cases.Add(xmlConfCase);
        }

        return cases;
    }

    private static XmlConfCase ReadCase(XElement test)
    {
        var id = Required(test, "ID");
        var baseUri = EffectiveBase(test);
        var testSet = baseUri.TrimEnd('/').Replace('/', '-');
        if (testSet.Length == 0)
        {
            throw new InvalidDataException($"Case '{id}' is not inside a TESTCASES element with an xml:base.");
        }

        return new XmlConfCase(
            $"{testSet}/{id}",
            testSet,
            baseUri + Required(test, "URI"),
            Required(test, "TYPE") switch
            {
                "valid" => XmlConfType.Valid,
                "invalid" => XmlConfType.Invalid,
                "not-wf" => XmlConfType.NotWellFormed,
                "error" => XmlConfType.Error,
                var other => throw new InvalidDataException($"Case '{id}': unknown TYPE '{other}'."),
            },
            (string?)test.Attribute("ENTITIES") switch
            {
                null or "none" => XmlConfEntities.None,
                "general" => XmlConfEntities.General,
                "parameter" => XmlConfEntities.Parameter,
                "both" => XmlConfEntities.Both,
                var other => throw new InvalidDataException($"Case '{id}': unknown ENTITIES '{other}'."),
            },
            Tokens(test, "VERSION"),
            Tokens(test, "EDITION"),
            (string?)test.Attribute("RECOMMENDATION") ?? "XML1.0",
            !string.Equals((string?)test.Attribute("NAMESPACE"), "no", StringComparison.Ordinal),
            string.Join(" ", test.Value.Split(Separators, StringSplitOptions.RemoveEmptyEntries))
        );
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

    private static string Required(XElement test, string name) =>
        (string?)test.Attribute(name)
        ?? throw new InvalidDataException($"A TEST element has no {name} attribute: '{test.Value.Trim()}'.");

    private static string[] Tokens(XElement test, string name) =>
        ((string?)test.Attribute(name))?.Split(Separators, StringSplitOptions.RemoveEmptyEntries) ?? [];
}
