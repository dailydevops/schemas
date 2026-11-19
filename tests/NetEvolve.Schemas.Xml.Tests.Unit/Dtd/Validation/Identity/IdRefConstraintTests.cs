namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Identity;

using NetEvolve.Schemas;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Identity.IdentityHarness;

/// <summary>
/// The validity constraint IDREF: an IDREF value is a Name and an IDREFS value is Names, and every name matches the value
/// of an ID attribute. A reference may precede its ID, so unmatched names are decided at the end of the document.
/// </summary>
public class IdRefConstraintTests
{
    [Test]
    public async Task Idref_ToAnEarlierId_IsValid()
    {
        var run = Run("<r><a id=\"x\"/><b ref=\"x\"/></r>");

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task Idref_BeforeItsId_IsValid()
    {
        var run = Run("<r><b ref=\"x\"/><a id=\"x\"/></r>");

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    [Arguments("<r id=\"x\" ref=\"x\"/>")]
    [Arguments("<r ref=\"x\" id=\"x\"/>")]
    public async Task Idref_ToTheIdOfTheSameElement_IsValid(string script)
    {
        var run = Run(script);

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task Idref_Unmatched_IsReportedAtTheEndOfTheDocumentWithTheAttributePosition()
    {
        var run = Run("<r>\n  <a ~ref=\"ghost\"/>\n  <b ~id=\"1x\"/>\n</r>");

        await Assert.That(run.Issues.Count).IsEqualTo(2);

        // The invalid ID is reported while the events arrive, the unmatched reference only when the document ends.
        await Assert.That(run.CountBeforeEnd).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5009.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected[1]);

        var issue = run.Issues[1];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5012.Code);
        await Assert.That(issue.Severity).IsEqualTo(IssueSeverity.Error);
        await Assert.That(issue.Constraint).IsEqualTo("IDREF");
        await Assert.That(issue.Location).IsEqualTo(run.Expected[0]);
        await Assert.That(issue.Location).IsEqualTo(new DocumentLocation(SystemId, 2, 6));
        await Assert.That(issue.Message).Contains(DtdValidityCodes.XML5012.Message);
        await Assert.That(issue.Message).Contains("'ghost'");
    }

    [Test]
    public async Task Idref_UnmatchedOnAFollowingLine_CarriesLineAndColumnOfTheAttribute()
    {
        var run = Run("<r id=\"y\"\n    ~ref=\"ghost\"/>");

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Location).IsEqualTo(new DocumentLocation(SystemId, 2, 5));
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected[0]);
    }

    [Test]
    public async Task Idref_UnmatchedMoreThanOnce_IsReportedPerOccurrenceInDocumentOrder()
    {
        var run = Run("<r><a ~ref=\"ghost\"/><b ~ref=\"ghost\"/><a ~ref=\"spirit\"/></r>");

        await Assert.That(run.Issues.Count).IsEqualTo(3);
        await Assert.That(run.CountBeforeEnd).IsEqualTo(0);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected[0]);
        await Assert.That(run.Issues[1].Location).IsEqualTo(run.Expected[1]);
        await Assert.That(run.Issues[2].Location).IsEqualTo(run.Expected[2]);
        await Assert.That(run.Issues[2].Message).Contains("'spirit'");
    }

    [Test]
    public async Task Idref_ToAnAttributeThatIsNotAnId_IsUnmatched()
    {
        var run = Run("<r note=\"x\" ~ref=\"x\"/>");

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5012.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected[0]);
    }

    [Test]
    public async Task Idref_ToAnIdThatIsNotAName_IsUnmatched()
    {
        var run = Run("<r><a ~id=\"1x\"/><b ~ref=\"1x\"/></r>");

        await Assert.That(run.Issues.Count).IsEqualTo(2);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5009.Code);
        await Assert.That(run.Issues[1].Code).IsEqualTo(DtdValidityCodes.XML5012.Code);
        await Assert.That(run.Issues[1].Location).IsEqualTo(run.Expected[1]);
    }

    [Test]
    [Arguments("1x")]
    [Arguments("a b")]
    [Arguments("")]
    [Arguments("-")]
    [Arguments(" a")]
    public async Task Idref_ThatIsNotAName_ReportsXml5012AtOnceWithTheAttributePosition(string value)
    {
        var run = Run("<r id=\"a\" ~ref=\"" + value + "\"/>");

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.CountBeforeEnd).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5012.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected[0]);
        await Assert.That(run.Issues[0].Message).Contains("not a name");
        await Assert.That(run.Issues[0].Message).Contains("'" + value + "'");
    }

    [Test]
    public async Task Idref_WithADefaultedValue_IsCheckedAtTheElement()
    {
        var run = Run(
            "<r id=\"ok\">~<a/></r>",
            new Attlist("r", [Id("id")]),
            new Attlist("a", [Attribute("target", DtdAttributeType.IdRef, DtdDefaultKind.Default, "gone")])
        );

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5012.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected[0]);
        await Assert.That(run.Issues[0].Message).Contains("'gone'");
    }

    [Test]
    public async Task Idrefs_AllNamesMatched_IsValid_ForwardAndBackward()
    {
        var run = Run("<r><a id=\"n1\"/><b refs=\"n1 n2 n3\"/><a id=\"n2\"/><a id=\"n3\"/></r>");

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task Idrefs_WithOneUnmatchedToken_ReportsExactlyThatToken()
    {
        var run = Run("<r><a id=\"first\"/><a id=\"second\"/><b ~refs=\"first ghost second\"/></r>");

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        var issue = run.Issues[0];
        await Assert.That(issue.Code).IsEqualTo(DtdValidityCodes.XML5012.Code);
        await Assert.That(issue.Location).IsEqualTo(run.Expected[0]);
        await Assert.That(issue.Message).Contains("'ghost'");
        await Assert.That(issue.Message).DoesNotContain("'first'");
        await Assert.That(issue.Message).DoesNotContain("'second'");
    }

    [Test]
    public async Task Idrefs_WithSeveralUnmatchedTokens_ReportsEachTokenOnce_AlsoWhenARepeatedTokenIsUnmatched()
    {
        var run = Run("<r id=\"known\"><b ~refs=\"ghost known spirit ghost\"/></r>");

        await Assert.That(run.Issues.Count).IsEqualTo(3);
        await Assert.That(run.Issues[0].Message).Contains("'ghost'");
        await Assert.That(run.Issues[1].Message).Contains("'spirit'");
        await Assert.That(run.Issues[2].Message).Contains("'ghost'");
        foreach (var issue in run.Issues)
        {
            await Assert.That(issue.Location).IsEqualTo(run.Expected[0]);
        }
    }

    [Test]
    public async Task Idrefs_WithOneTokenThatIsNotAName_ReportsOnlyThatToken_AtOnce()
    {
        var run = Run("<r id=\"first\"><b ~refs=\"first 9x first\"/></r>");

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.CountBeforeEnd).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5012.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected[0]);
        await Assert.That(run.Issues[0].Message).Contains("'9x'");
        await Assert.That(run.Issues[0].Message).DoesNotContain("'first'");
    }

    [Test]
    [Arguments("")]
    [Arguments("first  first")]
    [Arguments(" first")]
    [Arguments("first ")]
    [Arguments("first\tfirst")]
    public async Task Idrefs_ThatIsNotNamesSeparatedBySingleSpaces_ReportsTheOffendingToken(string value)
    {
        // Production 6 separates the names by exactly one space; the reader normalizes the value before the step sees it.
        var run = Run("<r id=\"first\"><b ~refs=\"" + value + "\"/></r>");

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5012.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected[0]);
        await Assert.That(run.Issues[0].Message).Contains("not a name");
    }

    [Test]
    public async Task Document_WithoutAnyIdentityAttribute_ReportsNothing()
    {
        var run = Run("<r><a/><b note=\"x\"/></r>");

        await Assert.That(run.Issues).IsEmpty();
        await Assert.That(run.Collector.IsStopped).IsFalse();
    }
}
