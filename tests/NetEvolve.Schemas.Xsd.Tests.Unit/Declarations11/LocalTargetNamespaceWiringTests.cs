namespace NetEvolve.Schemas.Xsd.Tests.Unit.Declarations11;

using System;
using System.Linq;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

/// <summary>The <c>targetNamespace</c> attribute of local element and attribute declarations is compiled and checked (XSD-78, #1429).</summary>
public class LocalTargetNamespaceWiringTests
{
    private const string Unqualified = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:t="urn:t" targetNamespace="urn:t">
          <xs:element name="root">
            <xs:complexType>
              <xs:sequence>
                <xs:element name="a" type="xs:string" targetNamespace="urn:t"/>
              </xs:sequence>
              <xs:attribute name="b" type="xs:string" targetNamespace="urn:t"/>
            </xs:complexType>
          </xs:element>
        </xs:schema>
        """;

    private const string Base = """
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:b="urn:b" targetNamespace="urn:b" elementFormDefault="qualified">
          <xs:complexType name="base">
            <xs:sequence><xs:element name="a" type="xs:string"/></xs:sequence>
          </xs:complexType>
        </xs:schema>
        """;

    private static string Derived(string restated) => $"""
        <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:t="urn:t" xmlns:b="urn:b" targetNamespace="urn:t">
          <xs:import namespace="urn:b" schemaLocation="base.xsd"/>
          <xs:complexType name="derived">
            <xs:complexContent>
              <xs:restriction base="b:base">
                <xs:sequence>{restated}</xs:sequence>
              </xs:restriction>
            </xs:complexContent>
          </xs:complexType>
          <xs:element name="root" type="t:derived"/>
        </xs:schema>
        """;

    private static IXsdValidator BuildDerived(string restated) =>
        SchemaValidator
            .ForXsd()
            .WithResolver(new MapResolver().Add("file:///base.xsd", Base))
            .AddSchema(Derived(restated).AsMemory(), AssemblyFixture.SchemaId)
            .Build();

    [Test]
    public async Task Validate_ALocalElementAndAttributeWithTheTargetNamespace_AreQualifiedInIt()
    {
        var validator = AssemblyFixture.Build(Unqualified);

        await Assert
            .That(AssemblyFixture.Run(validator, "<t:root xmlns:t=\"urn:t\" t:b=\"1\"><t:a>x</t:a></t:root>").IsValid)
            .IsTrue();
        await Assert
            .That(
                AssemblyFixture.Run(validator, "<t:root xmlns:t=\"urn:t\" b=\"1\"><a xmlns=\"\">x</a></t:root>").IsValid
            )
            .IsFalse();
    }

    [Test]
    public async Task Validate_ARestrictionThatRestatesTheDeclarationInTheNamespaceOfTheBase_IsValid()
    {
        var validator = BuildDerived("<xs:element name=\"a\" type=\"xs:string\" targetNamespace=\"urn:b\"/>");

        await Assert
            .That(
                AssemblyFixture
                    .Run(validator, "<t:root xmlns:t=\"urn:t\" xmlns:b=\"urn:b\"><b:a>x</b:a></t:root>")
                    .IsValid
            )
            .IsTrue();
        await Assert
            .That(AssemblyFixture.Run(validator, "<t:root xmlns:t=\"urn:t\"><t:a>x</t:a></t:root>").IsValid)
            .IsFalse();
    }

    [Test]
    public async Task Build_AnotherNamespaceInARestrictionOfAnyType_IsALocatedSchemaError()
    {
        const string schema = """
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:t="urn:t" targetNamespace="urn:t">
              <xs:complexType name="derived">
                <xs:complexContent>
                  <xs:restriction base="xs:anyType">
                    <xs:sequence><xs:element name="a" type="xs:string" targetNamespace="urn:o"/></xs:sequence>
                  </xs:restriction>
                </xs:complexContent>
              </xs:complexType>
            </xs:schema>
            """;

        var exception = AssemblyFixture.Failure(() => AssemblyFixture.Build(schema));

        var issue = exception.Issues.Single(i => i.Code == XsdRuleCodes.SrcElement);
        await Assert.That(issue.Constraint).IsEqualTo("src-element.4.3.2");
        await Assert.That(issue.Location.Line).IsGreaterThan(0);
    }

    [Test]
    public async Task Build_AnotherNamespaceOutsideARestriction_IsALocatedSchemaError()
    {
        var schema = Unqualified.Replace(
            "targetNamespace=\"urn:t\"/>",
            "targetNamespace=\"urn:o\"/>",
            StringComparison.Ordinal
        );

        var exception = AssemblyFixture.Failure(() => AssemblyFixture.Build(schema));

        var issues = exception
            .Issues.Where(i => i.Code == XsdRuleCodes.SrcElement || i.Code == XsdRuleCodes.SrcAttribute)
            .ToArray();
        await Assert.That(issues.Length).IsGreaterThanOrEqualTo(1);
        await Assert.That(issues.All(i => i.Location.Line > 0)).IsTrue();
    }

    [Test]
    public async Task Build_ATargetNamespaceTogetherWithForm_IsALocatedSchemaError()
    {
        var schema = Unqualified.Replace(
            "<xs:element name=\"a\" type=\"xs:string\" targetNamespace=\"urn:t\"/>",
            "<xs:element name=\"a\" type=\"xs:string\" targetNamespace=\"urn:t\" form=\"qualified\"/>",
            StringComparison.Ordinal
        );

        var exception = AssemblyFixture.Failure(() => AssemblyFixture.Build(schema));

        var issue = exception.Issues.Single(i => i.Code == XsdRuleCodes.SrcElement);
        await Assert.That(issue.Constraint).IsEqualTo("src-element.4.2");
        await Assert.That(issue.Location.Line).IsGreaterThan(0);
    }
}
