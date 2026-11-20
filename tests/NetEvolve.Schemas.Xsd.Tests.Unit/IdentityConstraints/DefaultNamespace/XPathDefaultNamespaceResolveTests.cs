namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.DefaultNamespace;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.IdentityConstraints.DefaultNamespace;
using static NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.DefaultNamespace.DefaultNamespaceFixture;

public class XPathDefaultNamespaceResolveTests
{
    private static string Resolve(
        string? schemaValue,
        string? selectorValue,
        string schemaExtra = "",
        XsdVersion version = XsdVersion.V11
    )
    {
        var constraint = Constraint(Schema(schemaExtra + Attr(schemaValue), Unique("e", selectorValue, "@a", null)));
        return XPathDefaultNamespace.Resolve(constraint.GetChildNode(0), Scope, version);
    }

    [Test]
    public async Task Resolve_NeitherSelectorNorSchemaHasTheAttribute_MeansNoNamespace() =>
        await Assert.That(Resolve(null, null)).IsEqualTo(string.Empty);

    [Test]
    public async Task Resolve_AttributeOnTheElement_IsTheNamespace() =>
        await Assert.That(Resolve(null, "urn:q")).IsEqualTo("urn:q");

    [Test]
    public async Task Resolve_AttributeOnlyOnTheSchema_IsInherited() =>
        await Assert.That(Resolve("urn:s", null)).IsEqualTo("urn:s");

    [Test]
    public async Task Resolve_AttributeOnElementAndSchema_TheElementWins() =>
        await Assert.That(Resolve("urn:s", "urn:q")).IsEqualTo("urn:q");

    [Test]
    public async Task Resolve_EmptyValueOnTheElement_OverridesTheSchemaWithNoNamespace() =>
        await Assert.That(Resolve("urn:s", string.Empty)).IsEqualTo(string.Empty);

    [Test]
    public async Task Resolve_UriValue_IsTrimmed() =>
        await Assert.That(Resolve(null, " \turn:q\n ")).IsEqualTo("urn:q");

    [Test]
    public async Task Resolve_Local_MeansNoNamespaceEvenWhenTheSchemaHasOne()
    {
        await Assert.That(Resolve("urn:s", "##local")).IsEqualTo(string.Empty);
        await Assert.That(Resolve("##local", null)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Resolve_TargetNamespace_IsTheTargetNamespaceOfTheSchema()
    {
        await Assert.That(Resolve(null, "##targetNamespace")).IsEqualTo(Tns);
        await Assert.That(Resolve("##targetNamespace", null)).IsEqualTo(Tns);
    }

    [Test]
    public async Task Resolve_TargetNamespace_OfAChameleonIsTheIncludingNamespace()
    {
        var constraint = Constraint(Schema(string.Empty, Unique("e", "##targetNamespace", "@a", null)));

        var result = XPathDefaultNamespace.Resolve(
            constraint.GetChildNode(0),
            new SchemaNamespaceScope("urn:i", true),
            XsdVersion.V11
        );

        await Assert.That(result).IsEqualTo("urn:i");
    }

    [Test]
    public async Task Resolve_TargetNamespace_WithoutOneMeansNoNamespace()
    {
        var constraint = Constraint(Schema(string.Empty, Unique("e", "##targetNamespace", "@a", null)));

        var result = XPathDefaultNamespace.Resolve(
            constraint.GetChildNode(0),
            new SchemaNamespaceScope(string.Empty, false),
            XsdVersion.V11
        );

        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Resolve_DefaultNamespace_IsTheDefaultNamespaceInScope()
    {
        await Assert.That(Resolve(null, "##defaultNamespace", " xmlns=\"urn:d\"")).IsEqualTo("urn:d");
        await Assert.That(Resolve("##defaultNamespace", null, " xmlns=\"urn:d\"")).IsEqualTo("urn:d");
    }

    [Test]
    public async Task Resolve_DefaultNamespace_WithoutOneInScopeMeansNoNamespace() =>
        await Assert.That(Resolve(null, "##defaultNamespace")).IsEqualTo(string.Empty);

    [Test]
    public async Task Resolve_DefaultNamespace_UsesTheDeclarationInScopeAtTheSelector()
    {
        var schema = Schema(
            " xmlns=\"urn:d\" xpathDefaultNamespace=\"##defaultNamespace\"",
            "<xs:unique name=\"u\"><xs:selector xmlns=\"urn:inner\" xpath=\"e\"/><xs:field xpath=\"@a\"/></xs:unique>"
        );
        var constraint = Constraint(schema);

        var selector = XPathDefaultNamespace.Resolve(constraint.GetChildNode(0), Scope, XsdVersion.V11);
        var field = XPathDefaultNamespace.Resolve(constraint.GetChildNode(1), Scope, XsdVersion.V11);

        await Assert.That(selector).IsEqualTo("urn:inner");
        await Assert.That(field).IsEqualTo("urn:d");
    }

    [Test]
    public async Task Resolve_Strict10_IgnoresTheAttribute()
    {
        await Assert.That(Resolve(null, "urn:q", version: XsdVersion.V10)).IsEqualTo(string.Empty);
        await Assert.That(Resolve("urn:s", null, version: XsdVersion.V10)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Resolve_Guards_Throw()
    {
        var node = Constraint(Schema(string.Empty, Unique("e", null, "@a", null))).GetChildNode(0);

        await Assert
            .That(() => XPathDefaultNamespace.Resolve(null!, Scope, XsdVersion.V11))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => XPathDefaultNamespace.Resolve(node, Scope, (XsdVersion)(-1)))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => XPathDefaultNamespace.Resolve(node, Scope, (XsdVersion)99))
            .Throws<ArgumentOutOfRangeException>();
    }
}
