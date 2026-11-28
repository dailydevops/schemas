namespace NetEvolve.Schemas.Schematron.Tests.Integration.Goldens.En16931;

using System.Text;
using System.Xml.Linq;

/// <summary>Reads a test-set file of the CEN EN 16931 unit-test corpus into one case per embedded document.</summary>
/// <remarks>
/// A test-set file has a root <c>testSet</c> with an optional <c>assert/scope</c> list and one <c>test</c> per document. A
/// <c>test</c> holds an <c>assert</c> with <c>success</c>, <c>error</c>, or <c>warning</c> ids and the document element. A
/// <c>success</c> id must not fire, an <c>error</c> or <c>warning</c> id must fire. Without a <c>scope</c> the ids of the asserts
/// of the test are the scope.
/// </remarks>
internal static class En16931TestSet
{
    private static readonly XNamespace Vefa = "http://difi.no/xsd/vefa/validator/1.0";
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Reads the cases of one test-set file.</summary>
    /// <param name="syntax">The syntax of the file, <c>ubl</c> or <c>cii</c>.</param>
    /// <param name="fileName">The file name, which prefixes the case ids.</param>
    /// <param name="xml">The text of the file.</param>
    /// <returns>One case per <c>test</c> in file order; the case id is <c>&lt;file name&gt;#&lt;position&gt;</c>.</returns>
    /// <exception cref="InvalidDataException">The file is no test set, or a test has no document or no assert.</exception>
    public static IReadOnlyList<En16931Case> Parse(string syntax, string fileName, string xml)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(syntax);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(xml);
        var root = XDocument.Parse(xml).Root;
        if (root is null || root.Name != Vefa + "testSet")
        {
            throw new InvalidDataException($"'{fileName}' is not a test set.");
        }

        var fileScope = root.Elements(Vefa + "assert")
            .Elements(Vefa + "scope")
            .Select(e => e.Value.Trim())
            .Where(s => s.Length > 0)
            .ToList();
        var cases = new List<En16931Case>();
        var position = 0;
        foreach (var test in root.Elements(Vefa + "test"))
        {
            position++;
            var id = $"{fileName}#{position}";
            var document = test.Elements().SingleOrDefault(e => e.Name.Namespace != Vefa);
            var asserts = test.Elements(Vefa + "assert").ToList();
            if (document is null || asserts.Count == 0)
            {
                throw new InvalidDataException($"Test '{id}' needs one assert and one document.");
            }

            var expected = asserts
                .SelectMany(a => a.Elements())
                .Where(e => e.Name == Vefa + "error" || e.Name == Vefa + "warning")
                .Select(e => e.Value.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();
            var mentioned = asserts
                .SelectMany(a => a.Elements())
                .Where(e => e.Name == Vefa + "error" || e.Name == Vefa + "warning" || e.Name == Vefa + "success")
                .Select(e => e.Value.Trim());
            var scope = fileScope.Concat(mentioned).Distinct(StringComparer.Ordinal).ToList();
            var text = new XDocument(document).ToString(SaveOptions.DisableFormatting);
            cases.Add(new En16931Case(syntax, id, Utf8.GetBytes(text), scope, expected, isExample: false));
        }

        return cases;
    }
}
