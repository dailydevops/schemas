namespace NetEvolve.Schemas.Xsd.Tests.Integration.Hostile;

using System.Globalization;
using System.Text;
using NetEvolve.Schemas.Xsd.Tests.Integration.Differential;

/// <summary>
/// The hostile corpus: the case directories below <c>Corpus</c> (<c>schema.xsd</c>, an optional <c>instance.xml</c>, further documents served
/// to the resolver, and <c>case.txt</c> with the expected code) and the cases that are too large to keep as files.
/// </summary>
internal static class HostileCorpus
{
    public const string CaseFile = "case.txt";

    public const string Xs = "http://www.w3.org/2001/XMLSchema";

    public static string Directory { get; } =
        Path.Combine(RepositoryFiles.Root, "tests", "NetEvolve.Schemas.Xsd.Tests.Integration", "Hostile", "Corpus");

    public static IEnumerable<HostileCase> All() => [.. FromFiles(), .. Generated()];

    /// <summary>Reads the case directories; a directory without an expected code yields the code -1, which no run reports.</summary>
    public static IEnumerable<HostileCase> FromFiles()
    {
        foreach (
            var directory in System
                .IO.Directory.EnumerateDirectories(Directory)
                .OrderBy(static d => d, StringComparer.Ordinal)
        )
        {
            var id = Path.GetFileName(directory);
            var baseUri = "file:///hostile/" + id + "/";
            var documents = new Dictionary<string, string>(StringComparer.Ordinal);
            string? instance = null;
            foreach (var file in System.IO.Directory.EnumerateFiles(directory))
            {
                var name = Path.GetFileName(file);
                if (name is CaseFile)
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                if (name is "instance.xml")
                {
                    instance = text;
                }
                else
                {
                    documents[baseUri + name] = text;
                }
            }

            var options = ReadOptions(Path.Combine(directory, CaseFile));
            _ = options.TryGetValue("expect", out var expect);
            var code = int.TryParse(expect, NumberStyles.None, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : -1;
            yield return new HostileCase(
                id,
                id[..id.IndexOf('-', StringComparison.Ordinal)],
                documents.TryGetValue(baseUri + "schema.xsd", out var schema) ? schema : string.Empty,
                documents,
                instance,
                code,
                options.TryGetValue("hints", out var hints) && hints == "true",
                ValidationLimits.Default,
                id.StartsWith("external-", StringComparison.Ordinal)
            );
        }
    }

    private static Dictionary<string, string> ReadOptions(string path)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!File.Exists(path))
        {
            return options;
        }

        foreach (var line in File.ReadAllLines(path))
        {
            var colon = line.IndexOf(':', StringComparison.Ordinal);
            if (colon > 0)
            {
                options[line[..colon].Trim()] = line[(colon + 1)..].Trim();
            }
        }

        return options;
    }

    /// <summary>The cases that are generated: the sizes are the attack, so a file would only hide them.</summary>
    public static IEnumerable<HostileCase> Generated()
    {
        yield return Many("include-fan-out", 100, chain: false);
        yield return Many("include-chain", 100, chain: true);
        yield return Nested("nesting-sequence-groups", 20_000, "xs:sequence");
        yield return Deep("nesting-instance-elements", 100_000);
        yield return LongValue("value-long-list", 1_000_000, "xs:int", "", list: true);
    }

    private static string Schema(string body, string attributes = "") =>
        $"<xs:schema xmlns:xs=\"{Xs}\"{attributes}>{body}</xs:schema>";

    private static HostileCase Make(
        string id,
        string schema,
        IReadOnlyDictionary<string, string>? documents,
        string? instance,
        int code = -1,
        ValidationLimits? limits = null
    ) =>
        new(
            id,
            id[..id.IndexOf('-', StringComparison.Ordinal)],
            schema,
            documents ?? new Dictionary<string, string>(),
            instance,
            code,
            SchemaLocationHints: false,
            limits ?? ValidationLimits.Default,
            ExternalAccess: false
        );

    private static HostileCase Many(string id, int count, bool chain)
    {
        var documents = new Dictionary<string, string>(StringComparer.Ordinal);
        var root = new StringBuilder();
        for (var i = 0; i < count; i++)
        {
            var next = chain && i + 1 < count ? $"<xs:include schemaLocation=\"d{i + 1}.xsd\"/>" : string.Empty;
            documents[$"file:///hostile/{id}/d{i}.xsd"] = Schema(
                $"{next}<xs:element name=\"e{i}\" type=\"xs:int\"/>",
                " elementFormDefault=\"qualified\""
            );
            if (!chain || i == 0)
            {
                _ = root.Append("<xs:include schemaLocation=\"d").Append(i).Append(".xsd\"/>");
            }
        }

        _ = root.Append("<xs:element name=\"root\" type=\"xs:int\"/>");
        return Make(id, Schema(root.ToString()), documents, null, 6);
    }

    private static HostileCase Nested(string id, int depth, string group)
    {
        var body = new StringBuilder("<xs:element name=\"root\"><xs:complexType>");
        for (var i = 0; i < depth; i++)
        {
            _ = body.Append('<').Append(group).Append('>');
        }

        _ = body.Append("<xs:element name=\"a\" type=\"xs:int\"/>");
        for (var i = 0; i < depth; i++)
        {
            _ = body.Append("</").Append(group).Append('>');
        }

        _ = body.Append("</xs:complexType></xs:element>");
        return Make(id, Schema(body.ToString()), null, "<root><a>1</a></root>", 4117);
    }

    private static HostileCase Deep(string id, int depth)
    {
        var instance = new StringBuilder();
        for (var i = 0; i < depth; i++)
        {
            _ = instance.Append("<e>");
        }

        for (var i = 0; i < depth; i++)
        {
            _ = instance.Append("</e>");
        }

        var schema = Schema(
            "<xs:element name=\"e\"><xs:complexType><xs:sequence><xs:element ref=\"e\" minOccurs=\"0\"/></xs:sequence></xs:complexType></xs:element>"
        );
        return Make(id, schema, null, instance.ToString(), 2);
    }

    private static HostileCase LongValue(string id, int length, string baseType, string facet, bool list = false)
    {
        var item = list
            ? $"<xs:simpleType name=\"i\"><xs:restriction base=\"{baseType}\"/></xs:simpleType>"
            : string.Empty;
        var type = list
            ? $"<xs:simpleType><xs:restriction><xs:simpleType><xs:list itemType=\"i\"/></xs:simpleType>{facet}</xs:restriction></xs:simpleType>"
            : $"<xs:simpleType><xs:restriction base=\"{baseType}\">{facet}</xs:restriction></xs:simpleType>";
        var schema = Schema($"{item}<xs:element name=\"root\">{type}</xs:element>");
        var value = list ? string.Join(' ', Enumerable.Repeat("a", length / 2)) : new string('a', length);
        return Make(id, schema, null, $"<root>{value}</root>", 4616);
    }
}
