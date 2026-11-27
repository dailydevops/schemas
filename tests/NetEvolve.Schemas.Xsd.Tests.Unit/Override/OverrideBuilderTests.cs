namespace NetEvolve.Schemas.Xsd.Tests.Unit.Override;

using System;
using System.Linq;
using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

/// <summary>
/// The default builder (XSD 1.1) compiles a schema with the <see langword="override"/> element and validates an instance
/// against the overriding component (Structures 1.1, 4.2.5), so the element no longer yields NES4065.
/// </summary>
public class OverrideBuilderTests
{
    private const string Target = "file:///a.xsd";

    private static IXsdValidator Build(string targetBody, string overrideBody, string rootBody)
    {
        var resolver = new MapResolver().Add(Target, AssemblyFixture.WrapSchema(targetBody));
        var root = AssemblyFixture.WrapSchema(
            "<xs:override schemaLocation=\"a.xsd\">" + overrideBody + "</xs:override>" + rootBody
        );

        return SchemaValidator
            .ForXsd()
            .WithResolver(resolver)
            .AddSchema(root.AsMemory(), AssemblyFixture.SchemaId)
            .Build();
    }

    [Test]
    [Arguments(
        "<xs:simpleType name=\"T\"><xs:restriction base=\"xs:int\"/></xs:simpleType><xs:element name=\"root\" type=\"t:T\"/>",
        "<xs:simpleType name=\"T\"><xs:restriction base=\"xs:string\"/></xs:simpleType>",
        "<root xmlns=\"urn:t\">abc</root>"
    )]
    [Arguments(
        "<xs:complexType name=\"T\"><xs:sequence><xs:element name=\"a\"/></xs:sequence></xs:complexType><xs:element name=\"root\" type=\"t:T\"/>",
        "<xs:complexType name=\"T\"><xs:sequence><xs:element name=\"b\"/></xs:sequence></xs:complexType>",
        "<root xmlns=\"urn:t\"><b/></root>"
    )]
    [Arguments(
        "<xs:group name=\"T\"><xs:sequence><xs:element name=\"a\"/></xs:sequence></xs:group>"
            + "<xs:element name=\"root\"><xs:complexType><xs:group ref=\"t:T\"/></xs:complexType></xs:element>",
        "<xs:group name=\"T\"><xs:choice><xs:element name=\"b\"/></xs:choice></xs:group>",
        "<root xmlns=\"urn:t\"><b/></root>"
    )]
    [Arguments(
        "<xs:attributeGroup name=\"T\"><xs:attribute name=\"a\"/></xs:attributeGroup>"
            + "<xs:element name=\"root\"><xs:complexType><xs:attributeGroup ref=\"t:T\"/></xs:complexType></xs:element>",
        "<xs:attributeGroup name=\"T\"><xs:attribute name=\"b\"/></xs:attributeGroup>",
        "<root xmlns=\"urn:t\" b=\"1\"/>"
    )]
    [Arguments(
        "<xs:element name=\"e\" type=\"xs:int\"/>"
            + "<xs:element name=\"root\"><xs:complexType><xs:sequence><xs:element ref=\"t:e\"/></xs:sequence></xs:complexType></xs:element>",
        "<xs:element name=\"e\" type=\"xs:string\"/>",
        "<root xmlns=\"urn:t\"><e>abc</e></root>"
    )]
    [Arguments(
        "<xs:attribute name=\"a\" type=\"xs:int\"/>"
            + "<xs:element name=\"root\"><xs:complexType><xs:attribute ref=\"t:a\"/></xs:complexType></xs:element>",
        "<xs:attribute name=\"a\" type=\"xs:string\"/>",
        "<root xmlns=\"urn:t\" xmlns:t=\"urn:t\" t:a=\"abc\"/>"
    )]
    public async Task Build_WithAnOverride_ValidatesAgainstTheOverridingComponent(
        string target,
        string replacement,
        string instance
    )
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(replacement);
        ArgumentNullException.ThrowIfNull(instance);

        var validator = Build(target, replacement, "");

        await Assert.That(AssemblyFixture.Run(validator, instance).IsValid).IsTrue();
    }

    [Test]
    public async Task Build_WithAnOverride_StillRejectsWhatTheOverridingComponentRejects()
    {
        var validator = Build(
            "<xs:simpleType name=\"T\"><xs:restriction base=\"xs:string\"/></xs:simpleType><xs:element name=\"root\" type=\"t:T\"/>",
            "<xs:simpleType name=\"T\"><xs:restriction base=\"xs:int\"/></xs:simpleType>",
            ""
        );

        await Assert.That(AssemblyFixture.Run(validator, "<root xmlns=\"urn:t\">abc</root>").IsValid).IsFalse();
    }

    [Test]
    public async Task Build_WithAnOverrideOfAMissingDocument_IsALocatedSchemaError()
    {
        var root = AssemblyFixture.WrapSchema("<xs:override schemaLocation=\"missing.xsd\"/>");
        var resolver = new MapResolver();

        var exception = AssemblyFixture.Failure(() =>
            SchemaValidator.ForXsd().WithResolver(resolver).AddSchema(root.AsMemory(), AssemblyFixture.SchemaId).Build()
        );

        await Assert.That(exception.Issues.Any(i => i.Code.Number == 4065)).IsFalse();
        await Assert.That(exception.Issues.Any(i => i.Location.Line > 0)).IsTrue();
    }

    [Test]
    public async Task Build_WithAnOverrideOfAMissingComponent_LeavesTheTargetUnchanged()
    {
        var validator = Build(
            "<xs:simpleType name=\"T\"><xs:restriction base=\"xs:int\"/></xs:simpleType><xs:element name=\"root\" type=\"t:T\"/>",
            "<xs:simpleType name=\"Other\"><xs:restriction base=\"xs:string\"/></xs:simpleType>",
            ""
        );

        await Assert.That(AssemblyFixture.Run(validator, "<root xmlns=\"urn:t\">12</root>").IsValid).IsTrue();
        await Assert.That(AssemblyFixture.Run(validator, "<root xmlns=\"urn:t\">abc</root>").IsValid).IsFalse();
    }

    [Test]
    public async Task Build_WithAnOverrideThatBreaksTheTransformedSchema_IsALocatedSchemaError()
    {
        var exception = AssemblyFixture.Failure(() =>
            Build(
                "<xs:simpleType name=\"T\"><xs:restriction base=\"xs:int\"/></xs:simpleType><xs:element name=\"root\" type=\"t:T\"/>",
                "<xs:simpleType name=\"T\"><xs:restriction base=\"t:Missing\"/></xs:simpleType>",
                ""
            )
        );

        await Assert.That(exception.Issues.Any(i => i.Code.Number == 4065)).IsFalse();
        await Assert.That(exception.Issues.Any(i => i.Location.Line > 0)).IsTrue();
    }
}
