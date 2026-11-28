namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Assembly;

using System.Linq;
using System.Threading.Tasks;

/// <summary>The validator checks the assertions of list items and union members (#949).</summary>
public class FacetAssertionUnionListWiringTests
{
    private static readonly string Schema = AssemblyFixture.WrapSchema(
        """
        <xs:simpleType name="big">
          <xs:restriction base="xs:integer"><xs:assertion test="$value gt 5"/></xs:restriction>
        </xs:simpleType>
        <xs:simpleType name="bigs"><xs:list itemType="t:big"/></xs:simpleType>
        <xs:simpleType name="bigOrText"><xs:union memberTypes="t:big xs:string"/></xs:simpleType>
        <xs:simpleType name="bigOnly"><xs:union memberTypes="t:big xs:date"/></xs:simpleType>
        <xs:element name="bigs" type="t:bigs"/>
        <xs:element name="u" type="t:bigOrText"/>
        <xs:element name="only" type="t:bigOnly"/>
        """
    );

    private static int Count(string instance) =>
        AssemblyFixture
            .Run(AssemblyFixture.Build(Schema), instance)
            .Issues.Count(i => i.Code == XsdRuleCodes.CvcAssertionsValid);

    [Test]
    public async Task Assertion_ListItems_CheckTheAssertionsOfTheItemType()
    {
        await Assert.That(Count("<bigs xmlns=\"urn:t\">6 7</bigs>")).IsEqualTo(0);
        await Assert.That(Count("<bigs xmlns=\"urn:t\">6 3 2</bigs>")).IsEqualTo(2);
    }

    [Test]
    public async Task Assertion_Union_PicksTheMemberThatSatisfiesItsAssertions()
    {
        await Assert.That(Count("<u xmlns=\"urn:t\">7</u>")).IsEqualTo(0);
        await Assert.That(Count("<u xmlns=\"urn:t\">3</u>")).IsEqualTo(0);
        await Assert.That(Count("<only xmlns=\"urn:t\">3</only>")).IsEqualTo(1);
    }
}
