namespace NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;

using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Validation.Assembly;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Compilation.CompilationFixture;
using XsdVersion = NetEvolve.Schemas.Xml.Datatypes.Contracts.XsdVersion;

/// <summary>The pipeline reads <c>substitutionGroup</c> and checks the affiliation of every global element declaration.</summary>
public class SubstitutionGroupPipelineTests
{
    private const string Types =
        "<xs:complexType name=\"b\"><xs:sequence><xs:element name=\"x\" type=\"xs:string\"/></xs:sequence></xs:complexType>"
        + "<xs:complexType name=\"d\"><xs:complexContent><xs:extension base=\"t:b\"/></xs:complexContent></xs:complexType>";

    [Test]
    public async Task Member_WithATypeDerivedFromTheHead_IsValid()
    {
        var compilation = Compile(
            Schema(
                Types
                    + "<xs:element name=\"h\" type=\"t:b\"/><xs:element name=\"m\" type=\"t:d\" substitutionGroup=\"t:h\"/>"
            )
        );

        await Assert.That(compilation.IsValid).IsTrue();
    }

    [Test]
    public async Task Member_WithATypeThatIsNotDerivedFromTheHead_IsReported()
    {
        var compilation = Compile(
            Schema(
                Types
                    + "<xs:element name=\"h\" type=\"t:d\"/><xs:element name=\"m\" type=\"t:b\" substitutionGroup=\"t:h\"/>"
            )
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.CosEquivDerivedOkRec]);
    }

    [Test]
    public async Task Member_OfAHeadThatIsFinalForTheDerivation_IsReported()
    {
        var compilation = Compile(
            Schema(
                Types
                    + "<xs:element name=\"h\" type=\"t:b\" final=\"extension\"/><xs:element name=\"m\" type=\"t:d\" substitutionGroup=\"t:h\"/>"
            )
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.CosEquivDerivedOkRec]);
    }

    [Test]
    public async Task Affiliation_ThatIsCircular_IsReported()
    {
        var compilation = Compile(
            Schema(
                "<xs:element name=\"a\" substitutionGroup=\"t:b\"/><xs:element name=\"b\" substitutionGroup=\"t:a\"/>"
            )
        );

        await Assert.That(compilation.IsValid).IsFalse();
    }

    [Test]
    public async Task Head_ThatIsNotDeclared_IsAResolveError()
    {
        var compilation = Compile(Schema("<xs:element name=\"m\" substitutionGroup=\"t:missing\"/>"));

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.SrcResolve]);
    }

    [Test]
    public async Task Head_WithAnAnonymousType_IsComparedWithTheTypeOfTheMember()
    {
        var compilation = Compile(
            Schema(
                "<xs:element name=\"h\"><xs:complexType><xs:sequence/></xs:complexType></xs:element><xs:element name=\"m\" type=\"xs:string\" substitutionGroup=\"t:h\"/>"
            )
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.CosEquivDerivedOkRec]);
    }

    [Test]
    public async Task Member_WithoutAType_HasTheTypeOfItsHead()
    {
        var compilation = Compile(
            Schema(
                Types
                    + "<xs:element name=\"h\" type=\"t:b\"/><xs:element name=\"m\" substitutionGroup=\"t:h\"/><xs:element name=\"n\" substitutionGroup=\"t:m\"/>"
            )
        );

        await Assert.That(compilation.IsValid).IsTrue();
        await Assert
            .That(Get<ElementDeclaration>(compilation, XsdComponentKind.ElementDeclaration, "n").TypeDefinition)
            .IsSameReferenceAs(Get(compilation, XsdComponentKind.ComplexType, "b"));
    }

    [Test]
    public async Task Restriction_ThatNamesAMemberOfTheHeadOfTheBase_IsValid()
    {
        var compilation = Compile(
            Schema(
                "<xs:element name=\"h\"/><xs:element name=\"m1\" substitutionGroup=\"t:h\"/><xs:element name=\"m2\" substitutionGroup=\"t:h\"/>"
                    + "<xs:complexType name=\"b\"><xs:sequence><xs:element ref=\"t:h\"/></xs:sequence></xs:complexType>"
                    + "<xs:complexType name=\"d\"><xs:complexContent><xs:restriction base=\"t:b\"><xs:sequence><xs:choice><xs:element ref=\"t:m1\"/><xs:element ref=\"t:m2\"/></xs:choice></xs:sequence></xs:restriction></xs:complexContent></xs:complexType>"
            )
        );

        await Assert.That(compilation.IsValid).IsTrue();
    }

    [Test]
    public async Task Restriction_ThatNamesASingleMemberOfTheHeadOfTheBase_IsValid()
    {
        var compilation = Compile(
            Schema(
                "<xs:element name=\"h\"/><xs:element name=\"m1\" substitutionGroup=\"t:h\"/><xs:element name=\"m2\" substitutionGroup=\"t:h\"/>"
                    + "<xs:complexType name=\"b\"><xs:sequence><xs:element ref=\"t:h\"/></xs:sequence></xs:complexType>"
                    + "<xs:complexType name=\"d\"><xs:complexContent><xs:restriction base=\"t:b\"><xs:sequence><xs:element ref=\"t:m1\"/></xs:sequence></xs:restriction></xs:complexContent></xs:complexType>"
            )
        );

        await Assert.That(compilation.IsValid).IsTrue();
    }

    [Test]
    public async Task Restriction_ThatNamesTheHeadWhereTheBaseNamesAMember_IsReported()
    {
        var compilation = Compile(
            Schema(
                "<xs:element name=\"h\"/><xs:element name=\"m1\" substitutionGroup=\"t:h\"/>"
                    + "<xs:complexType name=\"b\"><xs:sequence><xs:element ref=\"t:m1\"/></xs:sequence></xs:complexType>"
                    + "<xs:complexType name=\"d\"><xs:complexContent><xs:restriction base=\"t:b\"><xs:sequence><xs:element ref=\"t:h\"/></xs:sequence></xs:restriction></xs:complexContent></xs:complexType>"
            )
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.CosParticleRestrict]);
    }

    [Test]
    public async Task Restriction_ThatNamesAnElementOutsideTheGroupOfTheHead_IsReported()
    {
        var compilation = Compile(
            Schema(
                "<xs:element name=\"h\"/><xs:element name=\"m1\" substitutionGroup=\"t:h\"/><xs:element name=\"o\"/>"
                    + "<xs:complexType name=\"b\"><xs:sequence><xs:element ref=\"t:h\"/></xs:sequence></xs:complexType>"
                    + "<xs:complexType name=\"d\"><xs:complexContent><xs:restriction base=\"t:b\"><xs:sequence><xs:element ref=\"t:o\"/></xs:sequence></xs:restriction></xs:complexContent></xs:complexType>"
            )
        );

        await Assert.That(compilation.IsValid).IsFalse();
    }

    [Test]
    public async Task ContentModel_ThatHoldsAHeadAndItsMember_IsAmbiguous()
    {
        var compilation = Compile(
            Schema(
                "<xs:element name=\"h\"/><xs:element name=\"m\" substitutionGroup=\"t:h\"/>"
                    + "<xs:complexType name=\"c\"><xs:choice><xs:element ref=\"t:h\"/><xs:element ref=\"t:m\"/></xs:choice></xs:complexType>"
            )
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.CosNonambig]);
    }

    [Test]
    [Arguments("<r><m>1</m></r>", true)]
    [Arguments("<r><h>1.5</h></r>", true)]
    [Arguments("<r><m>1.5</m></r>", false)]
    [Arguments("<r><o>1</o></r>", false)]
    public async Task Instance_WithAMemberInPlaceOfTheHead_IsValidatedAgainstTheTypeOfTheMember(
        string instance,
        bool valid,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validator = await SchemaValidator
            .ForXsd()
            .WithVersion(XsdVersion.V10)
            .AddSchema(
                (
                    "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\">"
                    + "<xs:element name=\"h\" type=\"xs:decimal\"/><xs:element name=\"m\" type=\"xs:int\" substitutionGroup=\"h\"/><xs:element name=\"o\" type=\"xs:string\"/>"
                    + "<xs:element name=\"r\"><xs:complexType><xs:sequence><xs:element ref=\"h\"/></xs:sequence></xs:complexType></xs:element></xs:schema>"
                ).AsMemory(),
                "file:///s.xsd"
            )
            .BuildAsync(cancellationToken);

        var result = validator.Validate(instance.AsSpan(), "file:///i.xml", cancellationToken);

        await Assert.That(result.IsValid).IsEqualTo(valid);
    }
}
