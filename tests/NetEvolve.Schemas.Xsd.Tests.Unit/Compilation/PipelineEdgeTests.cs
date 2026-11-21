namespace NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;

using System;
using System.Linq;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Definitions;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Compilation.CompilationFixture;

/// <summary>The corners of the pipeline: the versions, the documents it reaches, the anonymous types, and the constructs it leaves out.</summary>
public class PipelineEdgeTests
{
    [Test]
    public async Task Compile_InXsd11_TheSharedConstructsCompileAndTheFirstVersionRulesAreNotApplied()
    {
        var compilation = Compile(
            Schema(
                "<xs:complexType name=\"c\"><xs:all><xs:element name=\"a\" maxOccurs=\"2\"/></xs:all></xs:complexType>"
                    + "<xs:complexType name=\"d\"><xs:complexContent><xs:restriction base=\"t:b\"><xs:sequence><xs:element name=\"x\"/></xs:sequence></xs:restriction></xs:complexContent></xs:complexType>"
                    + "<xs:complexType name=\"b\"><xs:sequence><xs:element name=\"y\"/></xs:sequence></xs:complexType>"
            ),
            XsdVersion.V11
        );

        await Assert.That(compilation.IsValid).IsTrue();
        await Assert.That(compilation.Set!.Version).IsEqualTo(XsdVersion.V11);
    }

    [Test]
    public async Task Compile_Xsd11ConstructInAnIncludedDocument_IsRejectedInStrict10AndTheRestIsNotBuilt()
    {
        var resolver = new MapResolver().Add(
            "file:///inc.xsd",
            CompositionFixture.Schema(
                "<xs:element name=\"e\"><xs:complexType><xs:assert test=\"true()\"/></xs:complexType></xs:element>",
                Tns
            )
        );

        var compilation = Compile(
            Schema("<xs:include schemaLocation=\"inc.xsd\"/><xs:element name=\"f\" type=\"t:missing\"/>"),
            XsdVersion.V10,
            resolver
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.Strict10Assert]);
    }

    [Test]
    public async Task Compile_ADocumentIncludedTwice_ContributesItsComponentsOnce()
    {
        var resolver = new MapResolver()
            .Add("file:///a.xsd", CompositionFixture.Schema("<xs:include schemaLocation=\"c.xsd\"/>", Tns))
            .Add("file:///b.xsd", CompositionFixture.Schema("<xs:include schemaLocation=\"c.xsd\"/>", Tns))
            .Add("file:///c.xsd", CompositionFixture.Schema("<xs:element name=\"shared\"/>", Tns));

        var compilation = Compile(
            Schema("<xs:include schemaLocation=\"a.xsd\"/><xs:include schemaLocation=\"b.xsd\"/>"),
            XsdVersion.V10,
            resolver
        );

        await Assert.That(Describe(compilation)).IsEmpty();
    }

    [Test]
    public async Task Compile_AChameleonDocument_TakesTheNamespaceOfItsIncluder()
    {
        var resolver = new MapResolver().Add(
            "file:///c.xsd",
            CompositionFixture.Schema(
                "<xs:simpleType name=\"code\"><xs:restriction base=\"xs:string\"/></xs:simpleType><xs:element name=\"item\" type=\"code\"/>"
            )
        );

        var compilation = Compile(
            Schema("<xs:include schemaLocation=\"c.xsd\"/><xs:element name=\"root\" type=\"t:code\"/>"),
            XsdVersion.V10,
            resolver
        );

        await Assert.That(Describe(compilation)).IsEmpty();
        await Assert.That(Get(compilation, XsdComponentKind.ElementDeclaration, "item")).IsNotNull();
    }

    [Test]
    public async Task Compile_AReferenceInsideAnAnonymousType_IsBoundOrReportedAsUnresolved()
    {
        var bound = Compile(
            Schema(
                "<xs:element name=\"e\"><xs:complexType><xs:sequence><xs:element ref=\"t:other\"/><xs:group ref=\"t:g\"/></xs:sequence></xs:complexType></xs:element>"
                    + "<xs:element name=\"other\"/><xs:group name=\"g\"><xs:sequence><xs:element name=\"z\"/></xs:sequence></xs:group>"
            )
        );
        var unresolved = Compile(
            Schema(
                "<xs:element name=\"e\"><xs:complexType><xs:sequence><xs:element ref=\"t:none\"/></xs:sequence></xs:complexType></xs:element>"
            )
        );

        await Assert.That(Describe(bound)).IsEmpty();
        await Assert.That(Codes(unresolved)).IsEquivalentTo([XsdRuleCodes.SrcResolve]);
        await Assert.That(unresolved.Result.Issues[0].Message).Contains("Elements");
    }

    [Test]
    public async Task IdentityConstraints_KeyAndKeyref_AreBuiltAndRegistered()
    {
        var compilation = Compile(
            Schema(
                "<xs:element name=\"doc\"><xs:complexType><xs:sequence><xs:element name=\"item\" maxOccurs=\"unbounded\"><xs:complexType><xs:attribute name=\"id\"/><xs:attribute name=\"ref\"/></xs:complexType></xs:element></xs:sequence></xs:complexType>"
                    + "<xs:key name=\"k\"><xs:selector xpath=\"t:item\"/><xs:field xpath=\"@id\"/></xs:key>"
                    + "<xs:keyref name=\"r\" refer=\"t:k\"><xs:selector xpath=\"t:item\"/><xs:field xpath=\"@ref\"/></xs:keyref></xs:element>"
            )
        );

        await Assert.That(Describe(compilation)).IsEmpty();
        await Assert
            .That(compilation.Set!.TryGet(XsdComponentKind.IdentityConstraint, new XPathQName(Tns, "k"), out var key))
            .IsTrue();
        await Assert.That(((IdentityConstraintDefinition)key!).ConstraintKind).IsEqualTo(IdentityConstraintKind.Key);
        await Assert
            .That(
                compilation.Set!.TryGet(
                    XsdComponentKind.IdentityConstraint,
                    new XPathQName(Tns, "r"),
                    out var reference
                )
            )
            .IsTrue();
        await Assert.That(((IdentityConstraintDefinition)reference!).ReferencedConstraint).IsSameReferenceAs(key);
    }

    [Test]
    public async Task IdentityConstraints_WithAnEmptyNameOrADuplicateName_AreErrors()
    {
        var empty = Compile(
            Schema(
                "<xs:element name=\"e\"><xs:unique name=\"\"><xs:selector xpath=\"a\"/><xs:field xpath=\"@b\"/></xs:unique></xs:element>"
            )
        );
        var duplicate = Compile(
            Schema(
                "<xs:element name=\"e\"><xs:unique name=\"u\"><xs:selector xpath=\"a\"/><xs:field xpath=\"@b\"/></xs:unique><xs:key name=\"u\"><xs:selector xpath=\"a\"/><xs:field xpath=\"@b\"/></xs:key></xs:element>"
            )
        );

        await Assert.That(Codes(empty)).IsEquivalentTo([XsdRuleCodes.SrcIdentityConstraint]);
        await Assert.That(Codes(duplicate)).IsEquivalentTo([XsdRuleCodes.SchPropsCorrect]);
    }

    [Test]
    [Arguments("<xs:attribute name=\"a\" form=\"qualified\"/>")]
    [Arguments("<xs:attribute name=\"a\" use=\"required\"/>")]
    public async Task Attribute_ThatIsGlobalMustHaveNeitherFormNorUse(string body)
    {
        var compilation = Compile(Schema(body));

        await Assert.That(Codes(compilation)).Contains(XsdRuleCodes.SrcAttribute);
    }

    [Test]
    [Arguments(
        "<xs:attribute name=\"a\"><xs:simpleType name=\"x\"><xs:restriction base=\"xs:int\"/></xs:simpleType></xs:attribute>",
        true
    )]
    [Arguments("<xs:element name=\"e\"><xs:complexType name=\"x\"/></xs:element>", false)]
    public async Task AnonymousType_WithAName_IsOneError(string body, bool simple)
    {
        var compilation = Compile(Schema(body));

        await Assert.That(Codes(compilation)).Contains(simple ? XsdRuleCodes.SrcSimpleType : XsdRuleCodes.SrcCt);
    }

    [Test]
    [Arguments(
        "<xs:attribute name=\"a\"/><xs:complexType name=\"c\"><xs:attribute ref=\"t:a\" name=\"b\"/></xs:complexType>"
    )]
    [Arguments(
        "<xs:attribute name=\"a\"/><xs:complexType name=\"c\"><xs:attribute ref=\"t:a\" type=\"xs:int\"/></xs:complexType>"
    )]
    [Arguments(
        "<xs:attribute name=\"a\"/><xs:complexType name=\"c\"><xs:attribute ref=\"t:a\" form=\"qualified\"/></xs:complexType>"
    )]
    [Arguments(
        "<xs:attribute name=\"a\"/><xs:complexType name=\"c\"><xs:attribute ref=\"t:a\"><xs:simpleType><xs:restriction base=\"xs:int\"/></xs:simpleType></xs:attribute></xs:complexType>"
    )]
    [Arguments("<xs:complexType name=\"c\"><xs:attribute/></xs:complexType>")]
    [Arguments("<xs:complexType name=\"c\"><xs:attribute name=\"\"/></xs:complexType>")]
    [Arguments("<xs:complexType name=\"c\"><xs:attribute name=\"a\" use=\"required\" default=\"x\"/></xs:complexType>")]
    [Arguments("<xs:complexType name=\"c\"><xs:attribute name=\"a\" type=\"t:c\"/></xs:complexType>")]
    public async Task AttributeUse_ThatBreaksAnAttributeConstraint_IsAnError(string body)
    {
        var compilation = Compile(Schema(body));

        await Assert.That(compilation.IsValid).IsFalse();
        await Assert.That(Codes(compilation)).Contains(XsdRuleCodes.SrcAttribute);
    }

    [Test]
    public async Task AttributeGroups_WithoutANameOrWithABrokenReference_AreErrors()
    {
        var unnamed = Compile(Schema("<xs:attributeGroup/>"));
        var noReference = Compile(Schema("<xs:attributeGroup name=\"g\"><xs:attributeGroup/></xs:attributeGroup>"));
        var badPrefix = Compile(
            Schema("<xs:attributeGroup name=\"g\"><xs:attributeGroup ref=\"u:x\"/></xs:attributeGroup>")
        );
        var intersect = Compile(
            Schema(
                "<xs:attributeGroup name=\"a\"><xs:anyAttribute namespace=\"urn:one\"/></xs:attributeGroup>"
                    + "<xs:attributeGroup name=\"b\"><xs:anyAttribute namespace=\"urn:two\"/></xs:attributeGroup>"
                    + "<xs:attributeGroup name=\"c\"><xs:attributeGroup ref=\"t:a\"/><xs:attributeGroup ref=\"t:b\"/></xs:attributeGroup>"
            )
        );
        var wildcard = Compile(
            Schema("<xs:attributeGroup name=\"g\"><xs:anyAttribute namespace=\"##any ##other\"/></xs:attributeGroup>")
        );

        await Assert.That(Codes(unnamed)).Contains(XsdRuleCodes.SrcAttributeGroup);
        await Assert.That(Codes(noReference)).Contains(XsdRuleCodes.SrcAttributeGroup);
        await Assert.That(Codes(badPrefix)).Contains(XsdRuleCodes.SrcResolve);
        await Assert.That(intersect.IsValid).IsTrue();
        await Assert.That(Codes(wildcard)).Contains(XsdRuleCodes.SrcWildcard);
    }

    [Test]
    public async Task ComplexType_WildcardsOfTheGroupsAndTheType_AreIntersected()
    {
        var compilation = Compile(
            Schema(
                "<xs:attributeGroup name=\"a\"><xs:anyAttribute namespace=\"urn:one\"/></xs:attributeGroup>"
                    + "<xs:complexType name=\"c\"><xs:attributeGroup ref=\"t:a\"/><xs:anyAttribute namespace=\"urn:two\"/></xs:complexType>"
            )
        );

        await Assert.That(compilation.IsValid).IsTrue();
        await Assert
            .That(
                Get<NetEvolve.Schemas.Xsd.ComplexTypes.Definitions.ComplexTypeDefinition>(
                    compilation,
                    XsdComponentKind.ComplexType,
                    "c"
                ).AttributeWildcard
            )
            .IsNotNull();
    }

    [Test]
    [Arguments("<xs:complexType name=\"c\"><xs:complexContent/></xs:complexType>")]
    [Arguments("<xs:complexType name=\"c\"><xs:simpleContent/></xs:complexType>")]
    [Arguments("<xs:complexType name=\"c\"><xs:complexContent><xs:extension/></xs:complexContent></xs:complexType>")]
    [Arguments("<xs:complexType name=\"c\"><xs:simpleContent><xs:restriction/></xs:simpleContent></xs:complexType>")]
    [Arguments(
        "<xs:complexType name=\"c\"><xs:simpleContent><xs:extension base=\"t:c\"/></xs:simpleContent></xs:complexType>"
    )]
    [Arguments(
        "<xs:complexType name=\"c\"><xs:complexContent><xs:extension base=\"xs:int\"/></xs:complexContent></xs:complexType>"
    )]
    public async Task ComplexType_WithAContentThatIsMissingItsDerivation_IsAnError(string body)
    {
        var compilation = Compile(Schema(body));

        await Assert.That(compilation.IsValid).IsFalse();
        await Assert.That(Codes(compilation)).Contains(XsdRuleCodes.SrcCt);
    }

    [Test]
    public async Task SimpleContent_RestrictedByFacetsOrAnAnonymousType_GetsTheRestrictedSimpleType()
    {
        var compilation = Compile(
            Schema(
                "<xs:complexType name=\"base\"><xs:simpleContent><xs:extension base=\"xs:string\"/></xs:simpleContent></xs:complexType>"
                    + "<xs:complexType name=\"byFacets\"><xs:simpleContent><xs:restriction base=\"t:base\"><xs:maxLength value=\"3\"/></xs:restriction></xs:simpleContent></xs:complexType>"
                    + "<xs:complexType name=\"byInline\"><xs:simpleContent><xs:restriction base=\"t:base\"><xs:simpleType><xs:restriction base=\"xs:string\"><xs:maxLength value=\"4\"/></xs:restriction></xs:simpleType></xs:restriction></xs:simpleContent></xs:complexType>"
                    + "<xs:complexType name=\"byBoth\"><xs:simpleContent><xs:restriction base=\"t:base\"><xs:simpleType><xs:restriction base=\"xs:string\"/></xs:simpleType><xs:minLength value=\"1\"/></xs:restriction></xs:simpleContent></xs:complexType>"
                    + "<xs:complexType name=\"plain\"><xs:simpleContent><xs:restriction base=\"t:base\"/></xs:simpleContent></xs:complexType>"
                    + "<xs:complexType name=\"mixedBase\" mixed=\"true\"><xs:sequence><xs:element name=\"x\" minOccurs=\"0\"/></xs:sequence></xs:complexType>"
                    + "<xs:complexType name=\"fromMixed\"><xs:simpleContent><xs:restriction base=\"t:mixedBase\"><xs:simpleType><xs:restriction base=\"xs:string\"/></xs:simpleType></xs:restriction></xs:simpleContent></xs:complexType>"
            )
        );

        await Assert.That(Codes(compilation).Count).IsLessThanOrEqualTo(1);
        var byFacets = Get<NetEvolve.Schemas.Xsd.ComplexTypes.Definitions.ComplexTypeDefinition>(
            compilation,
            XsdComponentKind.ComplexType,
            "byFacets"
        );
        await Assert.That(byFacets.SimpleContentType!.Facets.Count).IsEqualTo(1);
        var byBoth = Get<NetEvolve.Schemas.Xsd.ComplexTypes.Definitions.ComplexTypeDefinition>(
            compilation,
            XsdComponentKind.ComplexType,
            "byBoth"
        );
        await Assert.That(byBoth.SimpleContentType!.Facets.Count).IsEqualTo(1);
    }

    [Test]
    public async Task SimpleContent_FacetsOnABaseWithoutSimpleContent_AreOneError()
    {
        var compilation = Compile(
            Schema(
                "<xs:complexType name=\"b\"><xs:sequence><xs:element name=\"x\"/></xs:sequence></xs:complexType>"
                    + "<xs:complexType name=\"c\"><xs:simpleContent><xs:restriction base=\"t:b\"><xs:maxLength value=\"3\"/></xs:restriction></xs:simpleContent></xs:complexType>"
            )
        );

        await Assert.That(compilation.IsValid).IsFalse();
        await Assert.That(Codes(compilation)).Contains(XsdRuleCodes.SrcCt);
    }

    [Test]
    public async Task Facets_WithoutAValueOrStatedTwice_AreErrors()
    {
        var missing = Compile(
            Schema(
                "<xs:simpleType name=\"s\"><xs:restriction base=\"xs:int\"><xs:minInclusive/></xs:restriction></xs:simpleType>"
            )
        );
        var twice = Compile(
            Schema(
                "<xs:simpleType name=\"s\"><xs:restriction base=\"xs:int\"><xs:minInclusive value=\"1\"/><xs:minInclusive value=\"2\"/></xs:restriction></xs:simpleType>"
            )
        );

        await Assert.That(Codes(missing)).Contains(XsdRuleCodes.SrcSingleFacetValue);
        await Assert.That(Codes(twice)).Contains(XsdRuleCodes.SrcSingleFacetValue);
    }

    [Test]
    public async Task SimpleType_Variants_BuildTheirTypes()
    {
        var kit = new ContextKit(
            "<xs:simpleType name=\"item\"><xs:restriction base=\"xs:int\"/></xs:simpleType>"
                + "<xs:simpleType name=\"bad1\"><xs:list itemType=\"t:missingList\"/></xs:simpleType>"
                + "<xs:simpleType name=\"bad2\"><xs:restriction base=\"t:missingBase\"/></xs:simpleType>"
                + "<xs:simpleType name=\"bad3\"><xs:union memberTypes=\"xs:int t:missingMember\"/></xs:simpleType>"
                + "<xs:simpleType name=\"ok\"><xs:list><xs:simpleType><xs:restriction base=\"t:item\"/></xs:simpleType></xs:list></xs:simpleType>"
        );

        foreach (var definition in kit.Context.Definitions)
        {
            _ = kit.Context.EnsureType(definition);
        }

        await Assert.That(kit.Codes().Length).IsEqualTo(3);
        await Assert.That(kit.Context.Definitions[1].Component).IsTypeOf<SimpleTypeDefinition>();
        await Assert.That(((SimpleTypeDefinition)kit.Context.Definitions[4].Component!).ItemType).IsNotNull();
    }

    [Test]
    public async Task SimpleType_FinalOfTheSchemaDefault_IsTakenWhenTheTypeStatesNone()
    {
        var compilation = Compile(
            "<xs:schema xmlns:xs=\""
                + Xs
                + "\" xmlns:t=\""
                + Tns
                + "\" targetNamespace=\""
                + Tns
                + "\" finalDefault=\"restriction\" blockDefault=\"extension\">"
                + "<xs:simpleType name=\"s\"><xs:restriction base=\"xs:int\"/></xs:simpleType>"
                + "<xs:simpleType name=\"d\"><xs:restriction base=\"t:s\"/></xs:simpleType>"
                + "<xs:complexType name=\"c\" abstract=\"true\" block=\"restriction\" final=\"#all\"/>"
                + "<xs:complexType name=\"e\"/></xs:schema>"
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.CosStRestricts]);
    }

    [Test]
    public async Task Compile_ALocalAttributeWithQualifiedFormByTheSchemaDefault_IsInTheTargetNamespace()
    {
        var compilation = Compile(
            "<xs:schema xmlns:xs=\""
                + Xs
                + "\" targetNamespace=\""
                + Tns
                + "\" attributeFormDefault=\"qualified\"><xs:complexType name=\"c\"><xs:attribute name=\"a\"/><xs:attribute name=\"b\" form=\"unqualified\"/></xs:complexType></xs:schema>"
        );

        var type = Get<NetEvolve.Schemas.Xsd.ComplexTypes.Definitions.ComplexTypeDefinition>(
            compilation,
            XsdComponentKind.ComplexType,
            "c"
        );

        await Assert.That(type.AttributeUses.Select(u => u.Name.NamespaceName)).IsEquivalentTo([Tns, string.Empty]);
    }

    [Test]
    public async Task Compile_AResolverRefusal_IsReportedAndTheRestOfTheSchemaStillCompiles()
    {
        var compilation = Compile(
            Schema("<xs:include schemaLocation=\"x.xsd\"/><xs:element name=\"e\" type=\"t:nope\"/>")
        );

        await Assert.That(Codes(compilation).Count).IsEqualTo(2);
        await Assert.That(Codes(compilation)).Contains(XsdRuleCodes.SrcResolve);
    }

    [Test]
    public async Task Compile_ADocumentWithoutABaseUri_IsCompiledWithoutDeduplication(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var compiler = Compiler();
        var text = Schema("<xs:element name=\"e\"/>");

        var compilation = compiler.Compile(
            [
                NetEvolve.Schemas.Xsd.Compilation.SchemaInput.FromChars(text.AsMemory(), null),
                NetEvolve.Schemas.Xsd.Compilation.SchemaInput.FromChars(text.AsMemory(), null),
            ],
            cancellationToken
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.SchPropsCorrect]);
    }

    [Test]
    public async Task Compile_AnUnreadableSecondRoot_IsReportedAndTheFirstStillCompiles(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var good = Schema("<xs:element name=\"e\"/>");

        var compilation = Compiler()
            .Compile(
                [
                    NetEvolve.Schemas.Xsd.Compilation.SchemaInput.FromChars(good.AsMemory(), "file:///a.xsd"),
                    NetEvolve.Schemas.Xsd.Compilation.SchemaInput.FromChars("<broken".AsMemory(), "file:///b.xsd"),
                ],
                cancellationToken
            );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.SchemaReference]);
    }

    [Test]
    public async Task ComplexType_WildcardsThatHaveNoExpressibleIntersection_AreOneError()
    {
        var resolver = new MapResolver().Add(
            "file:///b.xsd",
            CompositionFixture.Schema(
                "<xs:attributeGroup name=\"g\"><xs:anyAttribute namespace=\"##other\"/></xs:attributeGroup>",
                "urn:b"
            )
        );

        var compilation = Compile(
            Schema(
                    "<xs:import namespace=\"urn:b\" schemaLocation=\"b.xsd\"/>"
                        + "<xs:complexType name=\"c\"><xs:attributeGroup ref=\"b:g\"/><xs:anyAttribute namespace=\"##other\"/></xs:complexType>"
                )
                .Replace("<xs:schema ", "<xs:schema xmlns:b=\"urn:b\" ", StringComparison.Ordinal),
            XsdVersion.V10,
            resolver
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.CosAwIntersect]);
    }

    [Test]
    public async Task AttributeUse_Prohibited_IsKeptOutOfTheUsesOfTheType()
    {
        var compilation = Compile(
            Schema(
                "<xs:complexType name=\"c\"><xs:attribute name=\"a\" use=\"prohibited\"/><xs:attribute name=\"b\"/></xs:complexType>"
            )
        );

        var type = Get<NetEvolve.Schemas.Xsd.ComplexTypes.Definitions.ComplexTypeDefinition>(
            compilation,
            XsdComponentKind.ComplexType,
            "c"
        );

        await Assert.That(type.AttributeUses.Select(u => u.Name.LocalName)).IsEquivalentTo(["b"]);
    }

    [Test]
    public async Task Facets_OfXsd11OrWithAnInvalidMode_AreNotCarried()
    {
        var kit = new ContextKit(
            "<xs:simpleType name=\"s\"><xs:restriction base=\"xs:string\"><xs:whiteSpace value=\"bogus\"/></xs:restriction></xs:simpleType>"
                + "<xs:simpleType name=\"d\"><xs:restriction base=\"xs:dateTime\"><xs:explicitTimezone value=\"required\"/></xs:restriction></xs:simpleType>",
            XsdVersion.V11
        );

        var bogus = (SimpleTypeDefinition)kit.Context.EnsureType(kit.Context.Definitions[0]);
        var timezone = (SimpleTypeDefinition)kit.Context.EnsureType(kit.Context.Definitions[1]);

        await Assert.That(bogus.Facets).IsEmpty();
        await Assert.That(timezone.BaseType).IsNotNull();
    }
}
