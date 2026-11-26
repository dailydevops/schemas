namespace NetEvolve.Schemas.Xsd.Tests.Unit.Override;

using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Composition.CompositionFixture;

/// <summary>
/// Equivalent overrides of one document are one unit, and an override that changes nothing in a target and in the documents
/// it includes leaves every one of them a plain unit (Structures 1.1, 4.2.5).
/// </summary>
public class OverrideEquivalenceTests
{
    private const string Simple = "<xs:simpleType name=\"T\"><xs:restriction base=\"xs:string\"/></xs:simpleType>";
    private const string Replacement = "<xs:simpleType name=\"T\"><xs:restriction base=\"xs:int\"/></xs:simpleType>";
    private const string ReplacementByte =
        "<xs:simpleType name=\"T\"><xs:restriction base=\"xs:byte\"/></xs:simpleType>";
    private const string ReplacementFinal =
        "<xs:simpleType name=\"T\" final=\"list\"><xs:restriction base=\"xs:int\"/></xs:simpleType>";
    private const string Other = "<xs:element name=\"X\"/>";

    private static string Override(string location, string children) =>
        "\n  <xs:override schemaLocation=\"" + location + "\">" + children + "</xs:override>";

    [Test]
    public async Task Override_WithEqualChildrenTwice_IsOneUnitOfTheDocument()
    {
        var resolver = new MapResolver().Add("file:///a.xsd", Schema(Simple + "<xs:element name=\"E\"/>"));

        var (set, issues) = Load(Schema(Override("a.xsd", Replacement) + Override("a.xsd", Replacement)), resolver);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(2);
        await Assert.That(resolver.Requests.Count).IsEqualTo(1);
    }

    [Test]
    public async Task Override_WithEqualChildrenInAnotherOrder_IsOneUnitOfTheDocument()
    {
        var resolver = new MapResolver().Add("file:///a.xsd", Schema(Simple + "<xs:element name=\"E\"/>"));
        const string element = "<xs:element name=\"E\" type=\"xs:int\"/>";

        var (set, issues) = Load(
            Schema(Override("a.xsd", Replacement + element) + Override("a.xsd", element + Replacement)),
            resolver
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Override_WithDifferentChildrenTwice_IsTwoUnitsOfTheDocument()
    {
        var resolver = new MapResolver().Add("file:///a.xsd", Schema(Simple));

        var (set, issues) = Load(Schema(Override("a.xsd", Replacement) + Override("a.xsd", ReplacementByte)), resolver);

        await Assert.That(set.Units.Count).IsEqualTo(3);
        await Assert.That(issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Override_WithChildrenOfDifferentAttributes_IsTwoUnitsOfTheDocument()
    {
        var resolver = new MapResolver().Add("file:///a.xsd", Schema(Simple));

        var (set, _) = Load(Schema(Override("a.xsd", Replacement) + Override("a.xsd", ReplacementFinal)), resolver);

        await Assert.That(set.Units.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Override_ThatMatchesNothingBelowAnIncludingTarget_LeavesEveryDocumentAPlainUnit()
    {
        var resolver = new MapResolver()
            .Add("file:///a.xsd", Schema("<xs:include schemaLocation=\"b.xsd\"/><xs:element name=\"E\"/>"))
            .Add("file:///b.xsd", Schema(Simple + "<xs:element name=\"F\"/>"));

        var (set, issues) = Load(Schema(Override("a.xsd", Other)), resolver);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(3);
        await Assert.That(set.Units[1].Overrides).IsNull();
        await Assert.That(set.Units[2].Overrides).IsNull();
        await Assert.That(set.Units[1].Included.Contains(set.Units[2])).IsTrue();
        await Assert.That(resolver.Requests.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Override_ThatMatchesNothingBelowAnIncludeCycle_LeavesEveryDocumentAPlainUnit()
    {
        var resolver = new MapResolver()
            .Add("file:///a.xsd", Schema("<xs:include schemaLocation=\"b.xsd\"/>" + Simple))
            .Add("file:///b.xsd", Schema("<xs:include schemaLocation=\"a.xsd\"/><xs:element name=\"E\"/>"));

        var (set, issues) = Load(Schema(Override("a.xsd", Other)), resolver);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(3);
        await Assert.That(set.Units[1].Overrides).IsNull();
        await Assert.That(set.Units[2].Overrides).IsNull();
        await Assert.That(resolver.Requests.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Override_ThatMatchesInADocumentTwoIncludesDown_KeepsTheChainOverridden()
    {
        var resolver = new MapResolver()
            .Add("file:///a.xsd", Schema("<xs:include schemaLocation=\"b.xsd\"/>"))
            .Add("file:///b.xsd", Schema("<xs:include schemaLocation=\"c.xsd\"/>"))
            .Add("file:///c.xsd", Schema(Simple));

        var (set, issues) = Load(Schema(Override("a.xsd", Replacement)), resolver);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(4);
        await Assert.That(set.Units[1].Overrides).IsNotNull();
        await Assert.That(set.Units[2].Overrides).IsNotNull();
        await Assert.That(set.Units[3].Overrides).IsNotNull();
        await Assert.That(resolver.Requests.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Override_ThatMatchesNothingBelowAnIncludingTarget_ReusesTheKnownPlainUnits()
    {
        var resolver = new MapResolver()
            .Add("file:///a.xsd", Schema("<xs:include schemaLocation=\"b.xsd\"/>"))
            .Add("file:///b.xsd", Schema(Simple));

        var (set, issues) = Load(Schema("<xs:include schemaLocation=\"a.xsd\"/>" + Override("a.xsd", Other)), resolver);

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(set.Units.Count).IsEqualTo(3);
        await Assert.That(resolver.Requests.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Override_ThatMatchesNothing_WhenAnIncludedDocumentIsMissing_ReportsTheMissingDocumentOnce()
    {
        var resolver = new MapResolver().Add("file:///a.xsd", Schema("<xs:include schemaLocation=\"b.xsd\"/>"));

        var (set, issues) = Load(Schema(Override("a.xsd", Other)), resolver);

        await Assert.That(set.Units.Count).IsEqualTo(2);
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CoreDiagnosticCodes.ResourceNotFound);
    }
}
