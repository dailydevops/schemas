namespace NetEvolve.Schemas.Xsd.Tests.Integration.Xsts;

using System.Xml.Linq;

/// <summary>
/// Reads the test set files of the W3C XML Schema Test Suite: schema tests, instance tests, status, version
/// tokens, and expected validity. Test sets that only <c>extra-suite.xml</c> lists carry the precisionDecimal tests.
/// </summary>
internal static class XstsReader
{
    /// <summary>The reason of every case that only <c>extra-suite.xml</c> lists.</summary>
    public const string PrecisionDecimalReason =
        "The case needs the precisionDecimal datatype, which is not implemented (DEF-52, #64).";

    private static readonly XNamespace Ts = "http://www.w3.org/XML/2004/xml-schema-test-suite/";
    private static readonly XNamespace Xlink = "http://www.w3.org/1999/xlink";
    private static readonly char[] Whitespace = [' ', '\t', '\r', '\n'];

    /// <summary>Reads every test of the suite.</summary>
    /// <param name="suiteRoot">The suite root directory.</param>
    /// <returns>The tests in suite order, the precisionDecimal tests last.</returns>
    public static IReadOnlyList<XstsCase> Read(string suiteRoot)
    {
        ArgumentNullException.ThrowIfNull(suiteRoot);
        var cases = new List<XstsCase>();
        var main = TestSetReferences(Path.Combine(suiteRoot, "suite.xml"));
        foreach (var href in main)
        {
            ReadTestSet(suiteRoot, href, null, cases);
        }

        var extraPath = Path.Combine(suiteRoot, "extra-suite.xml");
        if (File.Exists(extraPath))
        {
            var known = new HashSet<string>(main, StringComparer.Ordinal);
            foreach (var href in TestSetReferences(extraPath))
            {
                if (known.Add(href))
                {
                    ReadTestSet(suiteRoot, href, PrecisionDecimalReason, cases);
                }
            }
        }

        return cases;
    }

    private static List<string> TestSetReferences(string suitePath)
    {
        var references = new List<string>();
        foreach (var reference in XDocument.Load(suitePath).Root!.Elements(Ts + "testSetRef"))
        {
            references.Add((string)reference.Attribute(Xlink + "href")!);
        }

        return references;
    }

    private static void ReadTestSet(string suiteRoot, string href, string? exclusion, List<XstsCase> cases)
    {
        var setRoot = XDocument.Load(Path.Combine(suiteRoot, href)).Root!;
        var testSet = Path.ChangeExtension(href, null).Replace('/', '-');
        var directory = Path.GetDirectoryName(href) ?? "";
        var setScope = Tokens(setRoot);
        foreach (var group in setRoot.Elements(Ts + "testGroup"))
        {
            var groupName = (string)group.Attribute("name")!;
            var groupScope = Tokens(group);
            var schemaDocuments = new List<string>();
            foreach (var schemaTest in group.Elements(Ts + "schemaTest"))
            {
                schemaDocuments.AddRange(Documents(suiteRoot, directory, schemaTest, "schemaDocument"));
            }

            foreach (var test in group.Elements())
            {
                var isSchema = test.Name == Ts + "schemaTest";
                if (!isSchema && test.Name != Ts + "instanceTest")
                {
                    continue;
                }

                var scopes = new List<IReadOnlyList<string>>(3);
                foreach (var scope in new[] { setScope, groupScope, Tokens(test) })
                {
                    if (scope is not null)
                    {
                        scopes.Add(scope);
                    }
                }

                var expectations = new List<XstsExpectation>();
                foreach (var expected in test.Elements(Ts + "expected"))
                {
                    expectations.Add(
                        new XstsExpectation(Tokens(expected) ?? [], (string)expected.Attribute("validity")!)
                    );
                }

                cases.Add(
                    new XstsCase(
                        testSet,
                        groupName,
                        (string)test.Attribute("name")!,
                        isSchema ? XstsKind.Schema : XstsKind.Instance,
                        isSchema ? Documents(suiteRoot, directory, test, "schemaDocument") : schemaDocuments,
                        isSchema ? null : Documents(suiteRoot, directory, test, "instanceDocument")[0],
                        (string?)test.Element(Ts + "current")?.Attribute("status"),
                        scopes,
                        expectations,
                        exclusion
                    )
                );
            }
        }
    }

    private static string[]? Tokens(XElement element) =>
        ((string?)element.Attribute("version"))?.Split(Whitespace, StringSplitOptions.RemoveEmptyEntries);

    private static List<string> Documents(string suiteRoot, string directory, XElement test, string elementName)
    {
        var documents = new List<string>();
        foreach (var document in test.Elements(Ts + elementName))
        {
            var target = Path.GetFullPath(
                Path.Combine(suiteRoot, directory, (string)document.Attribute(Xlink + "href")!)
            );
            documents.Add(Path.GetRelativePath(suiteRoot, target).Replace('\\', '/'));
        }

        return documents;
    }
}
