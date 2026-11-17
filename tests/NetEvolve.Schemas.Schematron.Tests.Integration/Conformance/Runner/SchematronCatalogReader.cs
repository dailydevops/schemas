namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Runner;

using System.Xml.Linq;

/// <summary>Reads the test case format of the Schematron conformance suite: suite files, includes, and cases.</summary>
internal static class SchematronCatalogReader
{
    private static readonly XNamespace Ns = "https://doi.org/10.5281/zenodo.5679629#";

    /// <summary>Reads every case reachable from the suite file of a binding.</summary>
    /// <param name="suiteRoot">The suite root directory.</param>
    /// <param name="bindings">The bindings with their suite files.</param>
    /// <returns>Each case once, sorted by id, with every binding that lists it.</returns>
    /// <exception cref="InvalidDataException">A case does not follow the case format.</exception>
    public static IReadOnlyList<SchematronCase> Read(string suiteRoot, IReadOnlyList<BindingRule> bindings)
    {
        ArgumentNullException.ThrowIfNull(suiteRoot);
        ArgumentNullException.ThrowIfNull(bindings);
        var bindingsByCase = new SortedDictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var rule in bindings)
        {
            Collect(suiteRoot, rule.SuiteFile, rule.Binding, bindingsByCase);
        }

        var cases = new List<SchematronCase>(bindingsByCase.Count);
        foreach (var (id, caseBindings) in bindingsByCase)
        {
            cases.Add(ReadCase(suiteRoot, id, caseBindings));
        }

        return cases;
    }

    private static void Collect(
        string suiteRoot,
        string suiteFile,
        string binding,
        SortedDictionary<string, List<string>> bindingsByCase
    )
    {
        var directory = Path.GetDirectoryName(suiteFile) ?? "";
        foreach (var element in Load(Path.Combine(suiteRoot, suiteFile)).Elements())
        {
            var href = (string?)element.Attribute("href");
            if (href is null)
            {
                continue;
            }

            var target = RelativeId(suiteRoot, Path.Combine(directory, href));
            if (element.Name == Ns + "include")
            {
                Collect(suiteRoot, target, binding, bindingsByCase);
            }
            else if (element.Name == Ns + "testcase")
            {
                if (!bindingsByCase.TryGetValue(target, out var list))
                {
                    list = [];
                    bindingsByCase.Add(target, list);
                }

                if (!list.Contains(binding))
                {
                    list.Add(binding);
                }
            }
        }
    }

    private static SchematronCase ReadCase(string suiteRoot, string id, List<string> bindings)
    {
        var root = Load(Path.Combine(suiteRoot, id));
        var expect = (string?)root.Attribute("expect") switch
        {
            "valid" => SchematronOutcome.Valid,
            "invalid" => SchematronOutcome.Invalid,
            "error" => SchematronOutcome.Error,
            var other => throw new InvalidDataException($"Case '{id}': unknown expected outcome '{other}'."),
        };

        var schemas = Child(root, "schemas", id).Elements().ToList();
        if (schemas.Count == 0)
        {
            throw new InvalidDataException($"Case '{id}': no schema in the schemas element.");
        }

        var documents = Child(root, "documents", id)
            .Elements(Ns + "document")
            .Select(d => new SchematronDocument(
                (string?)d.Attribute("filename")
                    ?? throw new InvalidDataException($"Case '{id}': document without filename."),
                Serialize(d.Elements().Single())
            ))
            .ToList();
        var instance = documents.Find(d => string.Equals(d.FileName, "document.xml", StringComparison.Ordinal))
            ?? throw new InvalidDataException($"Case '{id}': no document named document.xml.");

        var separator = id.IndexOf('/', StringComparison.Ordinal);
        if (separator < 0)
        {
            throw new InvalidDataException(
                $"Case '{id}': a case must sit in a test set directory below the suite root."
            );
        }

        return new SchematronCase(
            id,
            id[..separator],
            [.. schemas.Select(Serialize)],
            instance.Xml,
            documents,
            expect,
            (string?)root.Attribute("phase"),
            root.Element(Ns + "assertions")
                ?.Elements(Ns + "assertion")
                .Select(a => (string)a.Attribute("assert")!)
                .ToList()
                ?? [],
            bindings
        );
    }

    private static XElement Child(XElement parent, string name, string id) =>
        parent.Element(Ns + name) ?? throw new InvalidDataException($"Case '{id}': no '{name}' element.");

    private static XElement Load(string path) => XDocument.Load(path, LoadOptions.PreserveWhitespace).Root!;

    private static string Serialize(XElement element) => element.ToString(SaveOptions.DisableFormatting);

    private static string RelativeId(string suiteRoot, string relativePath) =>
        Path.GetRelativePath(suiteRoot, Path.GetFullPath(Path.Combine(suiteRoot, relativePath))).Replace('\\', '/');
}
