namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

using System.Globalization;
using System.Text;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;

internal static class Qt3Fixtures
{
    private const string CatalogNamespace = "http://www.w3.org/2010/09/qt-fots-catalog";

    public static Qt3Case Case(Qt3Assertion assertion, string name = "c1", string testSet = "set") =>
        new(testSet, name, "1", null, null, assertion, []);

    /// <summary>Writes a catalog with one test set per entry; the entry body is the inner XML of the test-set element.</summary>
    public static string WriteSuite(TempDirectory directory, params (string Name, string Body)[] sets)
    {
        var catalog = new StringBuilder().Append(
            CultureInfo.InvariantCulture,
            $"<catalog xmlns=\"{CatalogNamespace}\">"
        );
        foreach (var (name, body) in sets)
        {
            _ = catalog.Append(CultureInfo.InvariantCulture, $"<test-set name=\"{name}\" file=\"{name}.xml\"/>");
            File.WriteAllText(
                Path.Combine(directory.Path, name + ".xml"),
                $"<test-set xmlns=\"{CatalogNamespace}\" name=\"{name}\">{body}</test-set>"
            );
        }
        File.WriteAllText(Path.Combine(directory.Path, "catalog.xml"), catalog.Append("</catalog>").ToString());
        return directory.Path;
    }

    public static string TestCase(string name, string dependencies = "", string result = "<assert-true/>") =>
        $"<test-case name=\"{name}\">{dependencies}<test>1</test><result>{result}</result></test-case>";

    public static string Dependency(string type, string value, string? satisfied = null) =>
        $"<dependency type=\"{type}\" value=\"{value}\"{(satisfied is null ? "" : $" satisfied=\"{satisfied}\"")}/>";
}
