namespace NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;

using static NetEvolve.Schemas.Xsd.Tests.Unit.Compilation.CompilationFixture;

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
            Schema(Types + "<xs:element name=\"h\" type=\"t:b\"/><xs:element name=\"m\" type=\"t:d\" substitutionGroup=\"t:h\"/>")
        );

        await Assert.That(compilation.IsValid).IsTrue();
    }

    [Test]
    public async Task Member_WithATypeThatIsNotDerivedFromTheHead_IsReported()
    {
        var compilation = Compile(
            Schema(Types + "<xs:element name=\"h\" type=\"t:d\"/><xs:element name=\"m\" type=\"t:b\" substitutionGroup=\"t:h\"/>")
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
    public async Task Head_WithAnAnonymousType_IsHandled()
    {
        var compilation = Compile(
            Schema(
                "<xs:element name=\"h\"><xs:complexType><xs:sequence/></xs:complexType></xs:element><xs:element name=\"m\" substitutionGroup=\"t:h\"/>"
            )
        );

        await Assert.That(Codes(compilation)).IsEquivalentTo([XsdRuleCodes.CosEquivDerivedOkRec]);
    }
}
