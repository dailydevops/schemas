namespace NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;

using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Compilation.CompilationFixture;

/// <summary>Every check that earlier work packages left unwired runs in the pipeline and reports its rule at the offending component.</summary>
public class CheckTests
{
    [Test]
    public async Task TypeDerivation_RestrictionThatMakesARequiredAttributeOptional_IsReported()
    {
        var compilation = Compile(
            Schema(
                "<xs:complexType name=\"b\"><xs:attribute name=\"a\" use=\"required\"/></xs:complexType>"
                    + "<xs:complexType name=\"d\"><xs:complexContent><xs:restriction base=\"t:b\"><xs:attribute name=\"a\"/></xs:restriction></xs:complexContent></xs:complexType>"
            )
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.DerivationOkRestriction]);
        await Assert.That(compilation.Result.Issues[0].Location.Line).IsGreaterThan(0);
    }

    [Test]
    public async Task TypeDerivation_ExtensionOfAFinalType_IsReported()
    {
        var compilation = Compile(
            Schema(
                "<xs:complexType name=\"b\" final=\"extension\"/>"
                    + "<xs:complexType name=\"d\"><xs:complexContent><xs:extension base=\"t:b\"/></xs:complexContent></xs:complexType>"
            )
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.CosCtExtends]);
    }

    [Test]
    public async Task ParticleRestriction_ThatAddsAnElement_IsReportedWithTheViolatedRule()
    {
        var compilation = Compile(
            Schema(
                "<xs:complexType name=\"b\"><xs:sequence><xs:element name=\"a\"/><xs:element name=\"b\" minOccurs=\"0\"/></xs:sequence></xs:complexType>"
                    + "<xs:complexType name=\"d\"><xs:complexContent><xs:restriction base=\"t:b\"><xs:sequence><xs:element name=\"a\"/><xs:element name=\"c\"/></xs:sequence></xs:restriction></xs:complexContent></xs:complexType>"
            )
        );

        await Assert.That(compilation.IsValid).IsFalse();
        await Assert.That(compilation.Result.Issues.Count).IsEqualTo(1);
        await Assert.That(compilation.Result.Issues[0].Constraint).StartsWith("rcase-");
    }

    [Test]
    public async Task ParticleRestriction_ThatNarrowsTheOccurrence_IsValid()
    {
        var compilation = Compile(
            Schema(
                "<xs:complexType name=\"b\"><xs:sequence><xs:element name=\"a\" maxOccurs=\"5\"/><xs:element name=\"b\" minOccurs=\"0\"/></xs:sequence></xs:complexType>"
                    + "<xs:complexType name=\"d\"><xs:complexContent><xs:restriction base=\"t:b\"><xs:sequence><xs:element name=\"a\" maxOccurs=\"2\"/></xs:sequence></xs:restriction></xs:complexContent></xs:complexType>"
            )
        );

        await Assert.That(Describe(compilation)).IsEmpty();
    }

    [Test]
    public async Task UniqueParticleAttribution_TwoParticlesThatMatchTheSameName_IsReported()
    {
        var compilation = Compile(
            Schema(
                "<xs:complexType name=\"c\"><xs:sequence><xs:element name=\"a\" minOccurs=\"0\"/><xs:element name=\"a\"/></xs:sequence></xs:complexType>"
            )
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.CosNonambig]);
    }

    [Test]
    public async Task UniqueParticleAttribution_AWildcardAndAnElementThatItAllows_IsReportedInOneZero()
    {
        var compilation = Compile(
            Schema(
                "<xs:complexType name=\"c\"><xs:sequence><xs:element name=\"a\" minOccurs=\"0\"/><xs:any/></xs:sequence></xs:complexType>"
            )
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.CosNonambig]);
    }

    [Test]
    public async Task FacetCheck_ARestrictionThatWidensABound_IsReported()
    {
        var compilation = Compile(
            Schema(
                "<xs:simpleType name=\"b\"><xs:restriction base=\"xs:int\"><xs:maxInclusive value=\"10\"/></xs:restriction></xs:simpleType>"
                    + "<xs:simpleType name=\"d\"><xs:restriction base=\"t:b\"><xs:maxInclusive value=\"20\"/></xs:restriction></xs:simpleType>"
            )
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.MaxInclusiveValidRestriction]);
    }

    [Test]
    public async Task FacetCheck_AFacetThatDoesNotApplyToTheBase_IsReported()
    {
        var compilation = Compile(
            Schema(
                "<xs:simpleType name=\"s\"><xs:restriction base=\"xs:boolean\"><xs:minLength value=\"1\"/></xs:restriction></xs:simpleType>"
            )
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.CosApplicableFacets]);
    }

    [Test]
    public async Task FacetCheck_AFixedFacetThatIsChanged_IsReported()
    {
        var compilation = Compile(
            Schema(
                "<xs:simpleType name=\"b\"><xs:restriction base=\"xs:string\"><xs:length value=\"3\" fixed=\"true\"/></xs:restriction></xs:simpleType>"
                    + "<xs:simpleType name=\"d\"><xs:restriction base=\"t:b\"><xs:length value=\"4\"/></xs:restriction></xs:simpleType>"
            )
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.LengthValidRestriction]);
    }

    [Test]
    [Arguments("<xs:element name=\"e\" type=\"xs:int\" default=\"abc\"/>")]
    [Arguments(
        "<xs:complexType name=\"c\"><xs:sequence><xs:element name=\"e\" type=\"xs:int\" fixed=\"x\"/></xs:sequence></xs:complexType>"
    )]
    public async Task ValueConstraint_OfAnElementThatIsNotValidForItsType_IsReported(string body)
    {
        var compilation = Compile(Schema(body));

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.CosValidDefault]);
    }

    [Test]
    [Arguments("<xs:notation name=\"n\" public=\"p\"/>")]
    [Arguments(
        "<xs:simpleType name=\"b\"><xs:restriction base=\"xs:NOTATION\"><xs:length value=\"3\"/></xs:restriction></xs:simpleType>"
    )]
    [Arguments(
        "<xs:notation name=\"n\" public=\"p\"/>"
            + "<xs:simpleType name=\"b\"><xs:restriction base=\"xs:NOTATION\"><xs:enumeration value=\"t:n\"/></xs:restriction></xs:simpleType>"
            + "<xs:simpleType name=\"d\"><xs:restriction base=\"t:b\"><xs:maxLength value=\"5\"/></xs:restriction></xs:simpleType>"
            + "<xs:element name=\"e\" type=\"t:d\"/>"
    )]
    public async Task Notation_RestrictionStepWithoutEnumerationOrEnumeratedType_IsNotReported(string body)
    {
        var compilation = Compile(Schema(body));

        await Assert.That(Codes(compilation).Count).IsEqualTo(0);
    }

    [Test]
    [Arguments(
        "<xs:simpleType name=\"b\"><xs:restriction base=\"xs:NOTATION\"><xs:length value=\"3\"/></xs:restriction></xs:simpleType>"
            + "<xs:complexType name=\"c\"><xs:attribute name=\"a\" type=\"t:b\"/></xs:complexType>"
    )]
    public async Task Notation_DeclarationOfATypeWithoutEnumeration_IsReported(string body)
    {
        var compilation = Compile(Schema(body));

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.EnumerationRequiredNotation]);
    }

    [Test]
    [Arguments(
        "<xs:notation name=\"png\" public=\"p\"/><xs:simpleType name=\"b\"><xs:restriction base=\"xs:NOTATION\"><xs:enumeration value=\"t:jpeg\"/></xs:restriction></xs:simpleType>"
    )]
    [Arguments(
        "<xs:simpleType name=\"b\"><xs:restriction base=\"xs:NOTATION\"><xs:enumeration value=\"t:png\"/></xs:restriction></xs:simpleType>"
    )]
    [Arguments(
        "<xs:notation name=\"png\" public=\"p\"/><xs:simpleType name=\"b\"><xs:restriction base=\"xs:NOTATION\"><xs:enumeration value=\"t:png\"/><xs:enumeration value=\"t:jpeg\"/></xs:restriction></xs:simpleType>"
    )]
    [Arguments(
        "<xs:simpleType name=\"b\"><xs:restriction base=\"xs:NOTATION\"><xs:enumeration value=\"png\"/></xs:restriction></xs:simpleType>"
    )]
    [Arguments(
        "<xs:complexType name=\"c\"><xs:simpleContent><xs:restriction base=\"t:base\"><xs:enumeration value=\"t:jpeg\"/></xs:restriction></xs:simpleContent></xs:complexType>"
            + "<xs:complexType name=\"base\"><xs:simpleContent><xs:extension base=\"t:nt\"/></xs:simpleContent></xs:complexType>"
            + "<xs:notation name=\"png\" public=\"p\"/>"
            + "<xs:simpleType name=\"nt\"><xs:restriction base=\"xs:NOTATION\"><xs:enumeration value=\"t:png\"/></xs:restriction></xs:simpleType>"
    )]
    public async Task Notation_EnumerationValueWithoutDeclaration_IsReported(string body)
    {
        var compilation = Compile(Schema(body));

        await Assert.That(Codes(compilation)).Contains(XsdRuleCodes.SrcResolve);
    }

    [Test]
    public async Task Notation_EnumerationValueWithUndeclaredPrefix_IsReportedAsUnresolved()
    {
        var compilation = Compile(
            Schema(
                "<xs:simpleType name=\"b\"><xs:restriction base=\"xs:NOTATION\"><xs:enumeration value=\"x:png\"/></xs:restriction></xs:simpleType>"
            )
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.SrcResolve]);
    }

    [Test]
    [Arguments("")]
    [Arguments(":png")]
    [Arguments("x:")]
    public async Task Notation_EnumerationValueThatIsMalformed_IsReportedOnce(string value)
    {
        var compilation = Compile(
            Schema(
                "<xs:notation name=\"png\" public=\"p\"/><xs:simpleType name=\"b\"><xs:restriction base=\"xs:NOTATION\"><xs:enumeration value=\""
                    + value
                    + "\"/></xs:restriction></xs:simpleType>"
            )
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.EnumerationValidRestriction]);
    }

    [Test]
    [Arguments(
        "<xs:simpleType name=\"b\"><xs:restriction base=\"xs:NOTATION\"><xs:enumeration value=\"t:png\"/></xs:restriction></xs:simpleType><xs:notation name=\"png\" public=\"p\"/>"
    )]
    [Arguments(
        "<xs:notation name=\"png\" public=\"p\"/><xs:simpleType name=\"b\"><xs:restriction base=\"xs:NOTATION\"><xs:enumeration value=\"t:png\"/></xs:restriction></xs:simpleType>"
            + "<xs:complexType name=\"c\"><xs:simpleContent><xs:restriction base=\"t:base\"><xs:enumeration value=\"t:png\"/></xs:restriction></xs:simpleContent></xs:complexType>"
            + "<xs:complexType name=\"base\"><xs:simpleContent><xs:extension base=\"t:b\"/></xs:simpleContent></xs:complexType>"
    )]
    public async Task Notation_EnumerationValueWithDeclaration_IsNotReported(string body)
    {
        var compilation = Compile(Schema(body));

        await Assert.That(Codes(compilation).Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("<xs:attribute name=\"a\" type=\"xs:int\" fixed=\"abc\"/>")]
    [Arguments(
        "<xs:complexType name=\"c\"><xs:attribute name=\"a\" type=\"xs:int\" default=\"abc\"/></xs:complexType>"
    )]
    public async Task ValueConstraint_OfAnAttributeThatIsNotValidForItsType_IsReported(string body)
    {
        var compilation = Compile(Schema(body));

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.APropsCorrect]);
    }

    [Test]
    public async Task ValueConstraint_OfAnAttributeUseThatIsNotValidForTheDeclaredType_IsReported()
    {
        var compilation = Compile(
            Schema(
                "<xs:attribute name=\"g\" type=\"xs:int\"/><xs:complexType name=\"c\"><xs:attribute ref=\"t:g\" default=\"abc\"/></xs:complexType>"
            )
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.AuPropsCorrect]);
    }

    [Test]
    public async Task ValueConstraint_OfAnElementWithAValidDefault_IsValid()
    {
        var compilation = Compile(
            Schema(
                "<xs:element name=\"e\" type=\"xs:int\" default=\"7\"/><xs:attribute name=\"a\" type=\"xs:boolean\" fixed=\"true\"/>"
            )
        );

        await Assert.That(Describe(compilation)).IsEmpty();
    }

    [Test]
    public async Task AllGroup_InStrictOneZero_AllowsNoOccurrenceAboveOneForAMember()
    {
        var compilation = Compile(
            Schema(
                "<xs:complexType name=\"c\"><xs:all><xs:element name=\"a\" maxOccurs=\"2\"/></xs:all></xs:complexType>"
            ),
            XsdVersion.V10
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.CosAllLimited]);
    }

    [Test]
    public async Task ElementDeclarationsConsistent_TwoElementsOfOneNameWithDifferentTypes_IsReported()
    {
        var compilation = Compile(
            Schema(
                "<xs:complexType name=\"c\"><xs:sequence><xs:element name=\"a\" type=\"xs:int\"/><xs:element name=\"b\"/><xs:element name=\"a\" type=\"xs:string\"/></xs:sequence></xs:complexType>"
            )
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.CosElementConsistent]);
    }

    [Test]
    public async Task IdAttributes_TwoOnOneType_AreReportedOnTheTypeAndOnTheGroup()
    {
        var compilation = Compile(
            Schema(
                "<xs:complexType name=\"c\"><xs:attribute name=\"a\" type=\"xs:ID\"/><xs:attribute name=\"b\" type=\"xs:ID\"/></xs:complexType>"
                    + "<xs:attributeGroup name=\"g\"><xs:attribute name=\"a\" type=\"xs:ID\"/><xs:attribute name=\"b\" type=\"xs:ID\"/></xs:attributeGroup>"
            )
        );

        await Assert
            .That(Codes(compilation))
            .IsEquivalentTo([XsdRuleCodes.CtPropsCorrect, XsdRuleCodes.AgPropsCorrect]);
    }

    [Test]
    public async Task ElementDeclaration_WithABrokenDefaultAndFixed_IsReportedOnceByTheBuilder()
    {
        var compilation = Compile(Schema("<xs:element name=\"e\" default=\"a\" fixed=\"a\"/>"));

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.SrcElement]);
    }

    [Test]
    public async Task ContentModel_ThatExceedsTheCeiling_IsReportedAsAnEngineIssue()
    {
        var elements = string.Concat(Enumerable.Range(0, 70_000).Select(i => "<xs:element name=\"e" + i + "\"/>"));
        var compilation = Compile(
            Schema("<xs:complexType name=\"c\"><xs:sequence>" + elements + "</xs:sequence></xs:complexType>")
        );

        await Assert.That(Codes(compilation)).Contains(XsdRuleCodes.ContentModelPositionCeilingExceeded);
    }

    [Test]
    public async Task IdentityConstraint_WithASelectorOutsideTheSubset_IsReported()
    {
        var compilation = Compile(
            Schema(
                "<xs:element name=\"e\"><xs:key name=\"k\"><xs:selector xpath=\"//a\"/><xs:field xpath=\"@id\"/></xs:key></xs:element>"
            )
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.CSelectorXpath]);
    }

    [Test]
    public async Task IdentityConstraint_KeyrefToAnUnknownKey_IsOneUnresolvedReferenceError()
    {
        var compilation = Compile(
            Schema(
                "<xs:element name=\"e\"><xs:keyref name=\"r\" refer=\"t:nope\"><xs:selector xpath=\"a\"/><xs:field xpath=\"@id\"/></xs:keyref></xs:element>"
            )
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.SrcResolve]);
    }
}
