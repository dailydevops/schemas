namespace NetEvolve.Schemas.Xsd.Tests.Unit.Declarations11;

using System.Linq;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;
using XsdVersion = NetEvolve.Schemas.Xml.Datatypes.Contracts.XsdVersion;

/// <summary>An element with several substitution group heads is a member of each group in the compiled validator (XSD-78, #1429, #824).</summary>
public class SubstitutionGroupListWiringTests
{
    private static string Schema(string aType = "xs:decimal", string bType = "xs:integer", string cType = "xs:int") =>
        AssemblyFixture.WrapSchema(
            $"""
            <xs:element name="a" type="{aType}"/>
            <xs:element name="b" type="{bType}"/>
            <xs:element name="c" type="{cType}" substitutionGroup="t:a t:b"/>
            <xs:element name="d" substitutionGroup="t:c"/>
            <xs:element name="ra"><xs:complexType><xs:sequence><xs:element ref="t:a"/></xs:sequence></xs:complexType></xs:element>
            <xs:element name="rb"><xs:complexType><xs:sequence><xs:element ref="t:b"/></xs:sequence></xs:complexType></xs:element>
            """
        );

    private static bool IsValid(string instance) =>
        AssemblyFixture.Run(AssemblyFixture.Build(Schema()), instance).IsValid;

    [Test]
    public async Task Validate_AMemberOfSeveralGroups_IsAcceptedWhereAnyHeadIsAllowed()
    {
        await Assert.That(IsValid("<ra xmlns=\"urn:t\"><c>1</c></ra>")).IsTrue();
        await Assert.That(IsValid("<rb xmlns=\"urn:t\"><c>1</c></rb>")).IsTrue();
    }

    [Test]
    public async Task Validate_AMemberOfAMember_IsAcceptedWhereTheHeadsOfTheMemberAreAllowed()
    {
        await Assert.That(IsValid("<ra xmlns=\"urn:t\"><d>1</d></ra>")).IsTrue();
        await Assert.That(IsValid("<rb xmlns=\"urn:t\"><d>1</d></rb>")).IsTrue();
    }

    [Test]
    public async Task Validate_AHeadThatIsNotAMember_IsStillRejected()
    {
        await Assert.That(IsValid("<ra xmlns=\"urn:t\"><b>1</b></ra>")).IsFalse();
        await Assert.That(IsValid("<rb xmlns=\"urn:t\"><a>1</a></rb>")).IsFalse();
    }

    [Test]
    public async Task Validate_AMemberWithAValueOfItsType_IsCheckedAgainstTheType() =>
        await Assert.That(IsValid("<ra xmlns=\"urn:t\"><c>x</c></ra>")).IsFalse();

    [Test]
    public async Task Build_ATypeThatIsNotDerivedFromEveryHead_IsALocatedSchemaError()
    {
        var exception = AssemblyFixture.Failure(() => AssemblyFixture.Build(Schema(bType: "xs:string")));

        var issue = exception.Issues.Single(i => i.Code == XsdRuleCodes.CosEquivDerivedOkRec);
        await Assert.That(issue.Location.Line).IsGreaterThan(0);
        await Assert.That(issue.Message).Contains("substitution group head");
    }

    [Test]
    public async Task Build_ACycleThroughAnyHead_IsStillReported()
    {
        var schema = AssemblyFixture.WrapSchema(
            """
            <xs:element name="x" type="xs:string" substitutionGroup="t:y t:z"/>
            <xs:element name="y" type="xs:string" substitutionGroup="t:x"/>
            <xs:element name="z" type="xs:string"/>
            """
        );

        var exception = AssemblyFixture.Failure(() => AssemblyFixture.Build(schema));

        await Assert.That(exception.Issues.Any(i => i.Code == XsdRuleCodes.CosEquivDerivedOkRec)).IsTrue();
    }

    [Test]
    public async Task Build_AHeadThatIsNotDeclared_IsAnUnresolvedReference()
    {
        var schema = AssemblyFixture.WrapSchema(
            """
            <xs:element name="a" type="xs:string"/>
            <xs:element name="c" type="xs:string" substitutionGroup="t:a t:missing"/>
            """
        );

        var exception = AssemblyFixture.Failure(() => AssemblyFixture.Build(schema));

        await Assert.That(exception.Issues.Any(i => i.Code == XsdRuleCodes.SrcResolve)).IsTrue();
    }

    [Test]
    public async Task Validate_Version10_KeepsReadingOnlyTheFirstHead()
    {
        var validator = AssemblyFixture.Build(Schema(), XsdVersion.V10);

        await Assert.That(AssemblyFixture.Run(validator, "<ra xmlns=\"urn:t\"><c>1</c></ra>").IsValid).IsTrue();
        await Assert.That(AssemblyFixture.Run(validator, "<rb xmlns=\"urn:t\"><c>1</c></rb>").IsValid).IsFalse();
    }
}
