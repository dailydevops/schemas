namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;

/// <summary>
/// The ID and the entity listener are created only for a schema that names a type of their check, directly or through a derived
/// type, a list, or a union (design #146 DP-12). NOTATION values are checked by the ID listener, so a NOTATION type demands it too.
/// </summary>
public class ListenerDemandTests
{
    private static readonly Dictionary<string, (string Body, bool Ids, bool Entities)> Schemas = new(
        StringComparer.Ordinal
    )
    {
        ["plain"] = ("<xs:element name=\"e\" type=\"xs:string\"/>", false, false),
        ["ncname"] = ("<xs:element name=\"e\" type=\"xs:NCName\"/>", false, false),
        ["anyType"] = ("<xs:element name=\"e\"/>", false, false),
        ["id"] = (
            "<xs:element name=\"e\"><xs:complexType><xs:attribute name=\"a\" type=\"xs:ID\"/></xs:complexType></xs:element>",
            true,
            false
        ),
        ["idref"] = ("<xs:element name=\"e\" type=\"xs:IDREF\"/>", true, false),
        ["idrefs"] = ("<xs:element name=\"e\" type=\"xs:IDREFS\"/>", true, false),
        ["derivedId"] = (
            "<xs:simpleType name=\"i\"><xs:restriction base=\"xs:ID\"/></xs:simpleType><xs:element name=\"e\" type=\"t:i\"/>",
            true,
            false
        ),
        ["derivedOfDerived"] = (
            "<xs:simpleType name=\"i\"><xs:restriction base=\"xs:IDREF\"/></xs:simpleType>"
                + "<xs:simpleType name=\"j\"><xs:restriction base=\"t:i\"/></xs:simpleType><xs:element name=\"e\" type=\"t:j\"/>",
            true,
            false
        ),
        ["listOfIdref"] = (
            "<xs:element name=\"e\"><xs:simpleType><xs:list itemType=\"xs:IDREF\"/></xs:simpleType></xs:element>",
            true,
            false
        ),
        ["unionWithId"] = (
            "<xs:element name=\"e\"><xs:simpleType><xs:union memberTypes=\"xs:int xs:ID\"/></xs:simpleType></xs:element>",
            true,
            false
        ),
        ["anonymousRestriction"] = (
            "<xs:element name=\"e\"><xs:complexType><xs:attribute name=\"a\"><xs:simpleType><xs:restriction base=\"xs:ID\"/></xs:simpleType></xs:attribute></xs:complexType></xs:element>",
            true,
            false
        ),
        ["notation"] = (
            "<xs:element name=\"e\"><xs:simpleType><xs:restriction base=\"xs:NOTATION\"><xs:enumeration value=\"t:n\"/></xs:restriction></xs:simpleType></xs:element>",
            true,
            false
        ),
        ["entity"] = (
            "<xs:element name=\"e\"><xs:complexType><xs:attribute name=\"a\" type=\"xs:ENTITY\"/></xs:complexType></xs:element>",
            false,
            true
        ),
        ["entities"] = ("<xs:element name=\"e\" type=\"xs:ENTITIES\"/>", false, true),
        ["derivedEntity"] = (
            "<xs:simpleType name=\"n\"><xs:restriction base=\"xs:ENTITY\"/></xs:simpleType><xs:element name=\"e\" type=\"t:n\"/>",
            false,
            true
        ),
        ["both"] = (
            "<xs:element name=\"e\"><xs:complexType><xs:attribute name=\"a\" type=\"xs:ID\"/><xs:attribute name=\"b\" type=\"xs:ENTITY\"/></xs:complexType></xs:element>",
            true,
            true
        ),
    };

    public static IEnumerable<Func<string>> Names() => Schemas.Keys.Select(k => (Func<string>)(() => k));

    [Test]
    [MethodDataSource(nameof(Names))]
    public async Task Compile_TellsWhichListenersTheSchemaNeeds(string name)
    {
        var (body, ids, entities) = Schemas[name];

        var compilation = CompilationFixture.Compile(CompilationFixture.Schema(body));

        await Assert.That(compilation.IsValid).IsTrue().Because(CompilationFixture.Describe(compilation));
        await Assert.That(compilation.NeedsIdListener).IsEqualTo(ids);
        await Assert.That(compilation.NeedsEntityListener).IsEqualTo(entities);
    }

    [Test]
    public async Task Compile_ASchemaWithAnError_NeedsNoListener()
    {
        var compilation = CompilationFixture.Compile(
            CompilationFixture.Schema(
                "<xs:element name=\"e\" type=\"xs:ID\"/><xs:element name=\"f\" type=\"t:missing\"/>"
            )
        );

        await Assert.That(compilation.IsValid).IsFalse();
        await Assert.That(compilation.NeedsIdListener).IsFalse();
        await Assert.That(compilation.NeedsEntityListener).IsFalse();
    }

    [Test]
    public async Task Validate_ASchemaWithoutIdTypes_IgnoresRepeatedValuesOfPlainTypes()
    {
        var schema = AssemblyFixture.WrapSchema(
            "<xs:element name=\"r\"><xs:complexType><xs:sequence><xs:element name=\"i\" type=\"xs:NCName\" maxOccurs=\"unbounded\"/></xs:sequence></xs:complexType></xs:element>"
        );

        var result = AssemblyFixture.Run(schema, "<r xmlns=\"urn:t\"><i>a</i><i>a</i></r>");

        await Assert.That(result.IsValid).IsTrue();
    }

    [Test]
    public async Task Validate_ANotationValueWithoutADeclaration_IsStillReportedWithoutAnyIdType()
    {
        var schema = AssemblyFixture.WrapSchema(Schemas["notation"].Body);

        var result = AssemblyFixture.Run(schema, "<e xmlns=\"urn:t\">n</e>");

        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Validate_ADerivedIdType_StillRejectsADuplicate()
    {
        var schema = AssemblyFixture.WrapSchema(
            "<xs:simpleType name=\"i\"><xs:restriction base=\"xs:ID\"/></xs:simpleType>"
                + "<xs:element name=\"r\"><xs:complexType><xs:sequence><xs:element name=\"x\" type=\"t:i\" maxOccurs=\"unbounded\"/></xs:sequence></xs:complexType></xs:element>"
        );

        var result = AssemblyFixture.Run(schema, "<r xmlns=\"urn:t\"><x>a</x><x>a</x></r>");

        await Assert.That(result.Issues.Any(i => i.Code == XsdRuleCodes.CvcId)).IsTrue();
    }

    [Test]
    public async Task Validate_AnEntityTypedValueWithoutADeclaration_IsStillRejected()
    {
        var schema = AssemblyFixture.WrapSchema(Schemas["entities"].Body);

        var result = AssemblyFixture.Run(schema, "<e xmlns=\"urn:t\">pic</e>", dtd: true);

        await Assert.That(result.IsValid).IsFalse();
    }
}
