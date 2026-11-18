namespace NetEvolve.Schemas.XPath.Tests.Unit.Types;

using NetEvolve.Schemas.XPath.Xdm.Values;
using static TypeFixture;

public class OccurrenceTests
{
    private static XdmSequence OfLength(int length)
    {
        var items = new XdmItem[length];
        for (var i = 0; i < length; i++)
        {
            items[i] = XdmItem.FromInteger(i);
        }

        return Seq(items);
    }

    [Test]
    [Arguments("xs:integer", 0, false)]
    [Arguments("xs:integer", 1, true)]
    [Arguments("xs:integer", 2, false)]
    [Arguments("xs:integer?", 0, true)]
    [Arguments("xs:integer?", 1, true)]
    [Arguments("xs:integer?", 2, false)]
    [Arguments("xs:integer*", 0, true)]
    [Arguments("xs:integer*", 1, true)]
    [Arguments("xs:integer*", 2, true)]
    [Arguments("xs:integer+", 0, false)]
    [Arguments("xs:integer+", 1, true)]
    [Arguments("xs:integer+", 2, true)]
    [Arguments("empty-sequence()", 0, true)]
    [Arguments("empty-sequence()", 1, false)]
    [Arguments("empty-sequence()", 2, false)]
    [Arguments("item()", 0, false)]
    [Arguments("item()", 1, true)]
    [Arguments("item()", 2, false)]
    [Arguments("item()?", 0, true)]
    [Arguments("item()*", 2, true)]
    [Arguments("item()+", 0, false)]
    public async Task Occurrence_AgainstSequencesOfLengthZeroOneAndTwo_MatchesByCount(
        string type,
        int length,
        bool expected
    ) => await Assert.That(Matcher(type).Matches(OfLength(length))).IsEqualTo(expected);

    [Test]
    [Arguments("xs:string", 1)]
    [Arguments("xs:string?", 1)]
    [Arguments("xs:string*", 2)]
    [Arguments("xs:string+", 2)]
    [Arguments("empty-sequence()", 1)]
    public async Task Occurrence_RightCountButWrongItemType_DoesNotMatch(string type, int length) =>
        await Assert.That(Matcher(type).Matches(OfLength(length))).IsFalse();

    [Test]
    public async Task EmptySequence_MatchesOptionalAndStarOfAnyItemType()
    {
        await Assert.That(Matcher("xs:string?").Matches(XdmSequence.Empty)).IsTrue();
        await Assert.That(Matcher("element(a)*").Matches(XdmSequence.Empty)).IsTrue();
        await Assert.That(Matcher("element(a)").Matches(XdmSequence.Empty)).IsFalse();
    }
}
