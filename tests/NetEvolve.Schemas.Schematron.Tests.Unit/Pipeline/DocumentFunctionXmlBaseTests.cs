namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System;
using System.Linq;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// An <c>xml:base</c> on the element that holds an expression, or on an ancestor, is the base of the relative references of
/// <c>doc</c>, <c>document</c>, <c>doc-available</c>, <c>unparsed-text</c>, and <c>unparsed-text-available</c> (#1263).
/// </summary>
public class DocumentFunctionXmlBaseTests
{
    private const string Schema = "file:///schemas/rules.sch";
    private const string Attributes =
        " queryBinding=\"xslt2\" schematronEdition=\"2025\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\"";

    private static FakeResources Files() =>
        new FakeResources()
            .Add("file:///schemas/codes.xml", "<codes><c>Z</c></codes>")
            .Add("file:///schemas/data/codes.xml", "<codes><c>A</c></codes>")
            .Add("file:///other/codes.xml", "<codes><c>O</c></codes>")
            .Add("file:///schemas/data/list.txt", "one")
            .Add("file:///schemas/data/deep/codes.xml", "<codes><c>D</c></codes>");

    private static ValidationResult Run(string body, FakeResources files, string rootAttributes = "") =>
        Validate(
            SchemaValidator.ForSchematron(Wrap(body, Attributes + rootAttributes), Schema).WithResolver(files).Build(),
            "<doc/>"
        );

    private static string OnRule(string test, string ruleAttributes) =>
        $"<pattern id=\"p\"><rule context=\"doc\"{ruleAttributes}><assert test=\"{test}\">Failed.</assert></rule></pattern>";

    [Test]
    [Arguments("doc('codes.xml')//c = 'A'")]
    [Arguments("document('codes.xml')//c = 'A'")]
    [Arguments("doc-available('codes.xml') and doc('codes.xml')//c = 'A'")]
    [Arguments("unparsed-text('list.txt') = 'one'")]
    [Arguments("unparsed-text-available('list.txt')")]
    public async Task Function_UnderAnXmlBase_ResolvesAgainstThatBase(string test)
    {
        ArgumentNullException.ThrowIfNull(test);
        var result = Run(OnRule(test, " xml:base=\"data/\""), Files());

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Doc_XmlBaseOnTheSchemaRoot_AppliesToEveryExpression()
    {
        var files = Files();

        var result = Run(OnRule("doc('codes.xml')//c = 'A'", string.Empty), files, " xml:base=\"data/\"");

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(files.Requests.Single()).IsEqualTo("file:///schemas/data/codes.xml");
    }

    [Test]
    public async Task Doc_XmlBaseOnThePattern_AppliesToItsRules()
    {
        var result = Run(
            "<pattern id=\"p\" xml:base=\"data/\"><rule context=\"doc\"><assert test=\"doc('codes.xml')//c = 'A'\">Failed.</assert></rule></pattern>",
            Files()
        );

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Doc_XmlBaseOnTheAssert_AppliesToItsTest()
    {
        var result = Run(
            "<pattern id=\"p\"><rule context=\"doc\"><assert test=\"doc('codes.xml')//c = 'A'\" xml:base=\"data/\">Failed.</assert></rule></pattern>",
            Files()
        );

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Doc_XmlBaseOnALet_AppliesToItsValue()
    {
        var result = Run(
            "<pattern id=\"p\"><rule context=\"doc\"><let name=\"c\" value=\"doc('codes.xml')//c\" xml:base=\"data/\"/><assert test=\"$c = 'A'\">Failed.</assert></rule></pattern>",
            Files()
        );

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Doc_NestedXmlBase_ComposesPerRfc3986()
    {
        var files = Files();

        var result = Run(
            "<pattern id=\"p\" xml:base=\"data/\"><rule context=\"doc\" xml:base=\"deep/\"><assert test=\"doc('codes.xml')//c = 'D' and doc('../codes.xml')//c = 'A'\">Failed.</assert></rule></pattern>",
            files
        );

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Doc_AbsoluteInnerXmlBase_ReplacesTheOuterOne()
    {
        var result = Run(
            "<pattern id=\"p\" xml:base=\"data/\"><rule context=\"doc\" xml:base=\"file:///other/\"><assert test=\"doc('codes.xml')//c = 'O'\">Failed.</assert></rule></pattern>",
            Files()
        );

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Doc_ElementWithoutXmlBase_ResolvesAgainstTheSchemaAsBefore()
    {
        var result = Run(
            "<pattern id=\"p\" xml:base=\"data/\"/><pattern id=\"q\"><rule context=\"doc\"><assert test=\"doc('codes.xml')//c = 'Z'\">Failed.</assert></rule></pattern>",
            Files()
        );

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Doc_XmlBaseUnderAbstractPatternInstance_IsKept()
    {
        var result = Run(
            "<pattern abstract=\"true\" id=\"a\" xml:base=\"data/\"><rule context=\"doc\"><assert test=\"doc('codes.xml')//c = 'A'\">Failed.</assert></rule></pattern>"
                + "<pattern is-a=\"a\" id=\"i\"/>",
            Files()
        );

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Document_BaseNodeWithXmlBaseInTheLoadedDocument_ResolvesAgainstThatBase()
    {
        var files = Files()
            .Add("file:///schemas/outer.xml", "<o xml:base=\"data/\"><n/></o>")
            .Add("file:///schemas/data/inner.xml", "<codes><c>Y</c></codes>");

        var result = Run(OnRule("document('inner.xml', doc('outer.xml')/o/n)//c = 'Y'", string.Empty), files);

        await Assert.That(result.IsValid).IsTrue();
        await Assert.That(files.Requests.Last()).IsEqualTo("file:///schemas/data/inner.xml");
    }

    [Test]
    public async Task Document_NodeArgumentWithXmlBaseInTheLoadedDocument_ResolvesAgainstThatBase()
    {
        var files = Files()
            .Add("file:///schemas/outer.xml", "<o><r xml:base=\"data/\">inner.xml</r></o>")
            .Add("file:///schemas/data/inner.xml", "<codes><c>Y</c></codes>");

        var result = Run(OnRule("document(doc('outer.xml')/o/r)//c = 'Y'", string.Empty), files);

        await Assert.That(result.IsValid).IsTrue();
    }
}
