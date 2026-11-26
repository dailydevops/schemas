namespace NetEvolve.Schemas.Xsd.Tests.Unit.Override;

using System;
using System.Threading;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xsd.Compilation;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Elements;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Composition.CompositionFixture;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Override.OverrideFixture;

/// <summary>
/// A component that an <see langword="override"/> replaces takes the document-level defaults of the overridden schema
/// document, not those of the overriding one (Structures 1.1, 4.2.5).
/// </summary>
public class OverrideDocumentDefaultsTests
{
    private const string A = "file:///a.xsd";
    private const string Replacement =
        "<xs:element name=\"T\"><xs:complexType><xs:sequence><xs:element name=\"L\"/></xs:sequence></xs:complexType></xs:element>";

    private const string Defaults = " elementFormDefault=\"qualified\" blockDefault=\"#all\" finalDefault=\"#all\"";

    private static string Document(string attributes, string body) =>
        "<xs:schema xmlns:xs=\"" + Xs + "\" targetNamespace=\"urn:t\"" + attributes + ">" + body + "</xs:schema>";

    // Builds the replacing element and its local child in the unit of the overridden document.
    private static (ElementDeclaration Global, ElementDeclaration Local) BuildReplaced(
        string overriding,
        string overridden
    )
    {
        var (set, _) = Load(
            Document(overriding, "<xs:override schemaLocation=\"a.xsd\">" + Replacement + "</xs:override>"),
            new Composition.MapResolver().Add(A, Document(overridden, "<xs:element name=\"T\"/>"))
        );
        var unit = new UnitScope(set.Units[1]);
        var reporter = new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default));
        var node = Component(set.Units[1], "T");
        var global = unit.Elements.BuildDeclaration(node, isGlobal: true, reporter);
        var local = unit.Elements.BuildDeclaration(
            node.GetChildNode(0).GetChildNode(0).GetChildNode(0),
            isGlobal: false,
            reporter
        );
        return (global, local);
    }

    [Test]
    public async Task Override_TheReplacedElement_TakesTheDefaultsOfTheOverriddenDocument()
    {
        var (global, local) = BuildReplaced(string.Empty, Defaults);

        await Assert.That(global.Block).IsNotEqualTo(XsdDerivationMethod.None);
        await Assert.That(global.Final).IsNotEqualTo(XsdDerivationMethod.None);
        await Assert.That(local.Name.NamespaceName).IsEqualTo("urn:t");
    }

    [Test]
    public async Task Override_TheReplacedElement_IgnoresTheDefaultsOfTheOverridingDocument()
    {
        var (global, local) = BuildReplaced(Defaults, string.Empty);

        await Assert.That(global.Block).IsEqualTo(XsdDerivationMethod.None);
        await Assert.That(global.Final).IsEqualTo(XsdDerivationMethod.None);
        await Assert.That(local.Name.NamespaceName).IsEqualTo(string.Empty);
    }
}
