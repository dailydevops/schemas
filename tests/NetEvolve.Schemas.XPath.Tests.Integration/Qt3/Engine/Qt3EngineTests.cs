namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Engine;

using System.Globalization;
using NetEvolve.Schemas.Conformance.Results;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner.Qt3Fixtures;

/// <summary>The engine against small suites on disk: environments, documents, namespaces, variables, and error codes.</summary>
public class Qt3EngineTests
{
    private const string Catalog = "http://www.w3.org/2010/09/qt-fots-catalog";

    private const string Document = "<p:root xmlns:p=\"urn:p\" id=\"7\"><p:item>a</p:item><p:item>b</p:item></p:root>";

    private const string Environment =
        "<environment name=\"e\"><namespace prefix=\"p\" uri=\"urn:p\"/><source role=\".\" file=\"doc.xml\"/></environment>";

    private const string UseE = "<environment ref=\"e\"/>";

    private static string CaseXml(string query, string result = "<assert-true/>", string environment = "") =>
        $"<test-case name=\"c1\">{environment}<test>{query}</test><result>{result}</result></test-case>";

    private static Qt3Case Prepare(TempDirectory directory, string setBody, string? catalogEnvironments = null)
    {
        var root = WriteSuite(directory, ("set", setBody));
        File.WriteAllText(Path.Combine(directory.Path, "doc.xml"), Document);
        if (catalogEnvironments is not null)
        {
            File.WriteAllText(
                Path.Combine(root, "catalog.xml"),
                $"<catalog xmlns=\"{Catalog}\">{catalogEnvironments}<test-set name=\"set\" file=\"set.xml\"/></catalog>"
            );
        }
        return Qt3CatalogReader.Read(root)[0].Cases[0];
    }

    private static EngineResult Evaluate(string setBody, string? catalogEnvironments = null)
    {
        using var directory = new TempDirectory();
        var testCase = Prepare(directory, setBody, catalogEnvironments);
        return new Qt3Engine(directory.Path).Evaluate(testCase);
    }

    private static string Text(object item) =>
        item switch
        {
            string text => text,
            bool value => value ? "true" : "false",
            XdmItem xdm when xdm.TryGetInteger(out var integer) => integer.ToString(CultureInfo.InvariantCulture),
            _ => item.GetType().Name,
        };

    [Test]
    public async Task Evaluate_EnvironmentWithSourceAndNamespace_ContextItemIsTheDocument()
    {
        var id = Evaluate(Environment + CaseXml("string(/p:root/@id)", environment: UseE));
        var same = Evaluate(Environment + CaseXml(". is /", environment: UseE));

        await Assert.That(id.Kind).IsEqualTo(EngineResultKind.Value);
        await Assert.That(id.Items.Count).IsEqualTo(1);
        await Assert.That(id.Items[0]).IsEqualTo("7");
        await Assert.That(same.Items.Count).IsEqualTo(1);
        await Assert.That(Text(same.Items[0])).IsEqualTo("true");
    }

    [Test]
    public async Task Evaluate_EnvironmentFromTheCatalog_IsFoundByReference()
    {
        var result = Evaluate(CaseXml("count(/p:root/p:item)", environment: UseE), Environment);

        await Assert.That(result.Items.Count).IsEqualTo(1);
        await Assert.That(Text(result.Items[0])).IsEqualTo("2");
    }

    [Test]
    public async Task Evaluate_EnvironmentDefinedInTheCase_IsUsed()
    {
        var inline =
            "<environment><namespace prefix=\"p\" uri=\"urn:p\"/><source role=\".\" file=\"doc.xml\"/></environment>";

        var result = Evaluate(CaseXml("string(/p:root/p:item[2])", environment: inline));

        await Assert.That(result.Items[0]).IsEqualTo("b");
    }

    [Test]
    public async Task Evaluate_SourceWithVariableRole_BindsTheDocumentToTheVariable()
    {
        var environment =
            "<environment name=\"v\"><namespace prefix=\"p\" uri=\"urn:p\"/><source role=\"$doc\" file=\"doc.xml\"/></environment>";

        var result = Evaluate(
            environment + CaseXml("string($doc/p:root/@id)", environment: "<environment ref=\"v\"/>")
        );

        await Assert.That(result.Items[0]).IsEqualTo("7");
    }

    [Test]
    public async Task Evaluate_SourceWithVariableRole_LeavesNoContextItem()
    {
        var environment = "<environment name=\"v\"><source role=\"$doc\" file=\"doc.xml\"/></environment>";

        var result = Evaluate(environment + CaseXml(".", environment: "<environment ref=\"v\"/>"));

        await Assert.That(result.Kind).IsEqualTo(EngineResultKind.Error);
        await Assert.That(result.ErrorCode).IsEqualTo(new QName(QName.ErrorNamespace, "XPDY0002"));
    }

    [Test]
    public async Task Evaluate_ParamWithSelect_BindsTheValue()
    {
        var environment =
            "<environment name=\"v\"><param name=\"n\" select=\"2 + 3\" declared=\"true\"/></environment>";
        var testCase = CaseXml("$n * 2", "<assert-eq>10</assert-eq>", "<environment ref=\"v\"/>");

        using var directory = new TempDirectory();
        var read = Prepare(directory, environment + testCase);
        var result = Qt3Runner.Run(read, new Qt3Engine(directory.Path));

        await Assert.That(result.Outcome).IsEqualTo(CaseOutcome.Pass);
    }

    [Test]
    public async Task Evaluate_WithoutEnvironment_HasNoContextItem()
    {
        var result = Evaluate(CaseXml("."));

        await Assert.That(result.Kind).IsEqualTo(EngineResultKind.Error);
        await Assert.That(result.ErrorCode).IsEqualTo(new QName(QName.ErrorNamespace, "XPDY0002"));
    }

    [Test]
    [Arguments("1 +", "XPST0003")]
    [Arguments("1 div 0", "FOAR0001")]
    [Arguments("nosuch:f()", "XPST0081")]
    [Arguments("nosuch()", "XPST0017")]
    [Arguments("$undeclared", "XPST0008")]
    public async Task Evaluate_QueryThatFails_ReturnsTheErrorCode(string query, string code)
    {
        var result = Evaluate(CaseXml(query));

        await Assert.That(result.Kind).IsEqualTo(EngineResultKind.Error);
        await Assert.That(result.ErrorCode).IsEqualTo(new QName(QName.ErrorNamespace, code));
    }

    [Test]
    public async Task Evaluate_PredeclaredPrefixes_AreBound()
    {
        var result = Evaluate(CaseXml("xs:integer('4') + fn:count((1, 2))"));

        await Assert.That(result.Kind).IsEqualTo(EngineResultKind.Value);
        await Assert.That(Text(result.Items[0])).IsEqualTo("6");
    }

    [Test]
    public async Task Evaluate_Items_AreBooleanStringOrXdmItem()
    {
        var result = Evaluate(CaseXml("(true(), 'a', 1, 2.5e0)"));

        await Assert.That(result.Items.Count).IsEqualTo(4);
        await Assert.That(Text(result.Items[0])).IsEqualTo("true");
        await Assert.That(result.Items[1]).IsEqualTo("a");
        await Assert.That(result.Items[2]).IsTypeOf<XdmItem>();
        await Assert.That(result.Items[3]).IsTypeOf<XdmItem>();
    }

    [Test]
    public async Task Evaluate_ResultVariable_IsBoundToTheGivenItems()
    {
        using var directory = new TempDirectory();
        var read = Prepare(directory, CaseXml("1"));
        var engine = new Qt3Engine(directory.Path);
        var first = engine.Evaluate(read);

        var count = engine.Evaluate(
            read with
            {
                Query = "count($result)",
                ResultVariable = [.. first.Items, "b", true],
            }
        );

        await Assert.That(Text(count.Items[0])).IsEqualTo("3");
    }

    [Test]
    public async Task Evaluate_StaticBaseUri_FollowsTheEnvironment()
    {
        var given = "<environment name=\"b\"><static-base-uri uri=\"http://example.com/base/\"/></environment>";
        var undefined = "<environment name=\"u\"><static-base-uri uri=\"#UNDEFINED\"/></environment>";

        var set = Evaluate(given + CaseXml("string(static-base-uri())", environment: "<environment ref=\"b\"/>"));
        var none = Evaluate(undefined + CaseXml("empty(static-base-uri())", environment: "<environment ref=\"u\"/>"));
        var unspecified = Evaluate(CaseXml("empty(static-base-uri())"));

        await Assert.That(set.Items[0]).IsEqualTo("http://example.com/base/");
        await Assert.That(Text(none.Items[0])).IsEqualTo("true");
        await Assert.That(Text(unspecified.Items[0])).IsEqualTo("true");
    }

    [Test]
    public async Task Evaluate_QueryInAFile_IsRead(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var directory = new TempDirectory();
        var read = Prepare(directory, CaseXml("0"));
        var file = Path.Combine(directory.Path, "query.xq");
        await File.WriteAllTextAsync(file, "40 + 2", cancellationToken);

        var result = new Qt3Engine(directory.Path).Evaluate(read with { Query = string.Empty, QueryFile = file });

        await Assert.That(Text(result.Items[0])).IsEqualTo("42");
    }

    [Test]
    public async Task Evaluate_SourceFileMissing_IsNotAnErrorResult()
    {
        var environment = "<environment name=\"m\"><source role=\".\" file=\"missing.xml\"/></environment>";

        var result = Evaluate(environment + CaseXml("1", "<error code=\"*\"/>", "<environment ref=\"m\"/>"));

        await Assert.That(result.Kind).IsEqualTo(EngineResultKind.NotBound);
    }
}
