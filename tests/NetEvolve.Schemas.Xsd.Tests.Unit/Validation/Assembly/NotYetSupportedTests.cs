namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;
using NetEvolve.Schemas.Xsd.Validation.Assembly;
using NetEvolve.Schemas.Xsd.Versioning.Constructs;
using XsdVersion = NetEvolve.Schemas.Xml.Datatypes.Contracts.XsdVersion;

/// <summary>
/// Under the default builder every XSD 1.1 construct of the table of #242 and the four value constructs that no row of that
/// table names yields its coded not-yet-supported schema error and never a silent pass (design #146 DP-16).
/// </summary>
public class NotYetSupportedTests
{
    private static readonly Dictionary<string, (string Body, string Attributes, int Code)> Constructs = new(
        StringComparer.Ordinal
    )
    {
        ["openContent"] = (
            "<xs:complexType name=\"c\"><xs:openContent><xs:any/></xs:openContent><xs:sequence/></xs:complexType>",
            "",
            4063
        ),
        ["defaultOpenContent"] = ("<xs:defaultOpenContent><xs:any/></xs:defaultOpenContent>", "", 4064),
        ["defaultAttributes"] = ("<xs:attributeGroup name=\"g\"/>", "defaultAttributes=\"t:g\"", 4066),
        ["defaultAttributesApply"] = ("<xs:complexType name=\"c\" defaultAttributesApply=\"false\"/>", "", 4067),
        ["precisionDecimal"] = (
            "<xs:simpleType name=\"s\"><xs:restriction base=\"xs:precisionDecimal\"/></xs:simpleType>",
            "",
            4076
        ),
        ["error"] = ("<xs:element name=\"e\" type=\"xs:error\"/>", "", 4077),
    };

    public static IEnumerable<Func<string>> Names() => Constructs.Keys.Select(k => (Func<string>)(() => k));

    [Test]
    [MethodDataSource(nameof(Names))]
    public async Task Build_Version11_YieldsTheCodedNotYetSupportedError(string name)
    {
        var (body, attributes, code) = Constructs[name];

        var exception = BuildFailure(body, attributes, XsdVersion.V11);

        var matches = exception.Issues.Where(i => i.Code.Number == code).ToArray();
        await Assert.That(matches.Length).IsGreaterThanOrEqualTo(1);
        await Assert.That(matches[0].Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(matches[0].Location.Line).IsGreaterThan(0);
        await Assert.That(matches[0].Location.Source).IsEqualTo(AssemblyFixture.SchemaId);
        await Assert.That(matches[0].Message).Contains("not supported yet");
    }

    [Test]
    [MethodDataSource(nameof(Names))]
    public async Task Build_Version11_RaisesNoCascadeOfUnrelatedErrors(string name)
    {
        var (body, attributes, code) = Constructs[name];

        var exception = BuildFailure(body, attributes, XsdVersion.V11);

        // The gate stops the pipeline before any component is built (DP-16), so only the gate (and the structure of the document) report.
        await Assert.That(exception.Issues.Any(i => i.Code.Number == code)).IsTrue();
        await Assert
            .That(exception.Issues.Any(i => i.Code == XsdRuleCodes.SrcResolve))
            .IsFalse()
            .Because("a gate error ends the run before the references are resolved");
    }

    [Test]
    public async Task Table_EveryRowOfTheStrict10TableHasANotYetSupportedCounterpart()
    {
        var counterparts = Xsd11ConstructTable.Entries.Select(e => e.NotYetSupported.Code.Number).ToArray();

        // The assert element of a complex type, the assertion facet, the explicitTimezone facet, the override element, the type
        // alternative, the wildcard attributes notNamespace and notQName, the inheritable and xpathDefaultNamespace attributes, and
        // the local targetNamespace are wired (XSD-78, #360, #1191, #1430, #1423, #1425, #1427, #1428, #1429), so they have no row any more.
        foreach (
            var entry in Strict10ConstructTable.Entries.Where(e =>
                e.IsAttribute
                    ? e.Name
                        is not (
                            "notNamespace"
                            or "notQName"
                            or "targetNamespace"
                            or "inheritable"
                            or "xpathDefaultNamespace"
                        )
                    : e.Name is not ("assert" or "assertion" or "explicitTimezone" or "override" or "alternative")
            )
        )
        {
            await Assert.That(counterparts).Contains(entry.Rule.Code.Number + 40);
        }

        await Assert.That(counterparts.Distinct().Count()).IsEqualTo(counterparts.Length);
        await Assert
            .That(Constructs.Values.Select(c => c.Code).OrderBy(code => code))
            .IsEquivalentTo(counterparts.OrderBy(code => code));
    }

    [Test]
    public async Task Build_Version10_ReportsTheStrictCodeAndNotTheNotYetSupportedCode()
    {
        const string body =
            "<xs:complexType name=\"c\"><xs:sequence><xs:any notQName=\"##defined\"/></xs:sequence></xs:complexType>";

        var exception = BuildFailure(body, "", XsdVersion.V10);

        await Assert.That(exception.Issues.Any(i => i.Code == XsdRuleCodes.Strict10NotQName)).IsTrue();
        await Assert.That(exception.Issues.Any(i => i.Code == XsdRuleCodes.NotYetSupportedNotQName)).IsFalse();
    }

    [Test]
    public async Task Build_Version11_WithoutAnyConstruct_Succeeds()
    {
        var validator = AssemblyFixture.Build(AssemblyFixture.Schema, XsdVersion.V11);

        await Assert.That(validator.Version).IsEqualTo(XsdVersion.V11);
    }

    [Test]
    public async Task Build_Version11_AnIncludedDocumentWithAConstructIsReported()
    {
        var included = AssemblyFixture.WrapSchema("<xs:complexType name=\"c\" defaultAttributesApply=\"false\"/>");
        var root = AssemblyFixture.WrapSchema("<xs:include schemaLocation=\"inc.xsd\"/>");
        var resolver = new MapResolver().Add("file:///inc.xsd", included);

        var exception = AssemblyFixture.Failure(() =>
            SchemaValidator.ForXsd().WithResolver(resolver).AddSchema(root.AsMemory(), AssemblyFixture.SchemaId).Build()
        );

        await Assert
            .That(exception.Issues.Any(i => i.Code == XsdRuleCodes.NotYetSupportedDefaultAttributesApply))
            .IsTrue();
    }

    [Test]
    public async Task Build_Version11_AQNameOfAnotherNamespaceNamedPrecisionDecimal_IsNotReported()
    {
        var schema = AssemblyFixture.WrapSchema(
            "<xs:simpleType name=\"precisionDecimal\"><xs:restriction base=\"xs:string\"/></xs:simpleType>"
                + "<xs:element name=\"e\" type=\"t:precisionDecimal\"/>"
        );

        var validator = AssemblyFixture.Build(schema, XsdVersion.V11);

        await Assert.That(validator).IsNotNull();
    }

    [Test]
    public async Task Build_Version11_AQNameOfAnotherNamespaceNamedError_IsNotReported()
    {
        var schema = AssemblyFixture.WrapSchema(
            "<xs:simpleType name=\"error\"><xs:restriction base=\"xs:string\"/></xs:simpleType>"
                + "<xs:element name=\"e\" type=\"t:error\"/>"
        );

        var validator = AssemblyFixture.Build(schema, XsdVersion.V11);

        await Assert.That(validator).IsNotNull();
    }

    [Test]
    public async Task Build_Version10_XsError_IsAnUnresolvedReferenceBecauseTheTypeIsXsd11()
    {
        var exception = BuildFailure("<xs:element name=\"e\" type=\"xs:error\"/>", "", XsdVersion.V10);

        await Assert.That(exception.Issues.Any(i => i.Code == XsdRuleCodes.SrcResolve)).IsTrue();
        await Assert.That(exception.Issues.Any(i => i.Code.Number == 4077)).IsFalse();
    }

    private static SchemaCompilationException BuildFailure(string body, string attributes, XsdVersion version) =>
        AssemblyFixture.Failure(() =>
            SchemaValidator
                .ForXsd()
                .WithVersion(version)
                .AddSchema(AssemblyFixture.WrapSchema(body, attributes).AsMemory(), AssemblyFixture.SchemaId)
                .Build()
        );
}
