namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.ContentModels;

using NetEvolve.Schemas.Xml.Dtd.Contracts;
using static NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.ContentModels.ContentModelHarness;

/// <summary>Accept and reject cases of the content kinds EMPTY, ANY, mixed, and children with every occurrence operator.</summary>
public class ContentModelMatchingTests
{
    [Test]
    [Arguments("<e/>")]
    [Arguments("<e></e>")]
    public async Task Empty_WithNoContent_IsAccepted(string script)
    {
        var run = Run(script, null, Empty("e"));

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    [Arguments("<e>~<!-- c --></e>")]
    [Arguments("<e>~<?pi x?></e>")]
    [Arguments("<e>~&x;</e>")]
    [Arguments("<e>~ </e>")]
    [Arguments("<e>~\n</e>")]
    [Arguments("<e>~x</e>")]
    [Arguments("<e>~&amp;</e>")]
    [Arguments("<e><![CDATA[~x]]></e>")]
    [Arguments("<e>~<c/></e>")]
    public async Task EmptyElement_WithCommentPiReferenceOrWhiteSpace_ReportsXml5004(string markedScript)
    {
        var run = Run(markedScript, null, Empty("e"), Empty("c"));

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5004.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
    }

    [Test]
    public async Task Any_AcceptsDeclaredChildrenTextCommentsProcessingInstructionsAndReferences()
    {
        var run = Run("<r>text<a/> \n<!--c--><?p?>&x;<b>more</b><![CDATA[ ]]></r>", null, Any("r"), Any("a"), Any("b"));

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task Any_UndeclaredChild_ReportsXml5004OnThatChild()
    {
        var run = Run("<r><a/>~<z/></r>", null, Any("r"), Empty("a"));

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5004.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
        await Assert.That(run.Issues[0].Message).Contains("'z'");
    }

    [Test]
    [Arguments("<r/>")]
    [Arguments("<r>text</r>")]
    [Arguments("<r>text<!--c--><?p?>&x;&#65;<![CDATA[y]]>more</r>")]
    public async Task MixedWithoutNames_AcceptsEveryKindOfCharacterContent(string script)
    {
        var run = Run(script, null, Mixed("r"));

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    public async Task MixedWithoutNames_RejectsAnyChildElement()
    {
        var run = Run("<r>text~<a/></r>", null, Mixed("r"), Empty("a"));

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
    }

    [Test]
    [Arguments("<r/>")]
    [Arguments("<r><a/></r>")]
    [Arguments("<r>t<a/>t<b/>t<a/><a/><b/></r>")]
    [Arguments("<r><b/>t<!--c--><?p?></r>")]
    public async Task MixedWithNames_AcceptsListedChildrenInAnyOrderAndRepeat(string script)
    {
        var run = Run(script, null, Mixed("r", "a", "b"), Empty("a"), Empty("b"), Empty("c"));

        await Assert.That(run.Issues).IsEmpty();
    }

    [Test]
    [Arguments("<r><a/>t~<c/></r>")]
    [Arguments("<r>~<c/><a/></r>")]
    [Arguments("<r><a/><b/>~<c/><c/></r>")]
    public async Task MixedWithNames_RejectsAnUnlistedChildAtThatChildOnly(string markedScript)
    {
        var run = Run(markedScript, null, Mixed("r", "a", "b"), Empty("a"), Empty("b"), Empty("c"));

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5004.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
    }

    // Once
    [Test]
    [Arguments("(a)", "<r><a/></r>")]
    [Arguments("(a,b)", "<r><a/><b/></r>")]
    [Arguments("(a|b)", "<r><b/></r>")]
    [Arguments("(a|b)", "<r><a/></r>")]
    // Optional
    [Arguments("(a?)", "<r/>")]
    [Arguments("(a?)", "<r><a/></r>")]
    [Arguments("(a?,b)", "<r><b/></r>")]
    [Arguments("(a?,b)", "<r><a/><b/></r>")]
    [Arguments("(a,b?)", "<r><a/></r>")]
    [Arguments("((a,b)?,c)", "<r><c/></r>")]
    [Arguments("((a,b)?,c)", "<r><a/><b/><c/></r>")]
    // Zero or more
    [Arguments("(a*)", "<r/>")]
    [Arguments("(a*)", "<r><a/><a/><a/></r>")]
    [Arguments("(a*,b)", "<r><b/></r>")]
    [Arguments("(a*,b)", "<r><a/><a/><b/></r>")]
    [Arguments("((a|b)*)", "<r/>")]
    [Arguments("((a|b)*)", "<r><a/><b/><a/></r>")]
    [Arguments("((a,b)*)", "<r><a/><b/><a/><b/></r>")]
    // One or more
    [Arguments("(a+)", "<r><a/></r>")]
    [Arguments("(a+)", "<r><a/><a/></r>")]
    [Arguments("((a|b)+)", "<r><b/><a/></r>")]
    [Arguments("((a,b)+,c)", "<r><a/><b/><a/><b/><c/></r>")]
    // Nested and combined
    [Arguments("(a,(b|c)+,d?)", "<r><a/><c/><b/><d/></r>")]
    [Arguments("(a,(b|c)+,d?)", "<r><a/><b/></r>")]
    [Arguments("((a?,b?)*,c)", "<r><a/><b/><b/><a/><c/></r>")]
    // White space and comments between children
    [Arguments("(a,b)", "<r>\n  <a/>\n\t<b/> \n</r>")]
    [Arguments("(a,b)", "<r><!--x--><a/><?p y?><b/><!--z--></r>")]
    public async Task Children_Accepts(string model, string script)
    {
        var run = Run(script, null, WithLeaves(Children("r", model)));

        await Assert.That(run.Issues).IsEmpty();
        await Assert.That(run.Result.IsValid).IsTrue();
    }

    // Once
    [Test]
    [Arguments("(a)", "<r>~</r>")]
    [Arguments("(a)", "<r><a/>~<a/></r>")]
    [Arguments("(a)", "<r>~<b/></r>")]
    [Arguments("(a,b)", "<r><a/>~</r>")]
    [Arguments("(a,b)", "<r>~<b/><a/></r>")]
    [Arguments("(a|b)", "<r>~<c/></r>")]
    [Arguments("(a|b)", "<r><a/>~<b/></r>")]
    // Optional
    [Arguments("(a?)", "<r><a/>~<a/></r>")]
    [Arguments("(a?,b)", "<r>~<c/></r>")]
    [Arguments("(a?,b)", "<r><a/>~</r>")]
    [Arguments("((a,b)?,c)", "<r><a/>~<c/></r>")]
    // Zero or more
    [Arguments("(a*,b)", "<r><a/><a/>~</r>")]
    [Arguments("(a*,b)", "<r><a/><b/>~<a/></r>")]
    [Arguments("((a|b)*)", "<r><a/>~<c/></r>")]
    [Arguments("((a,b)*)", "<r><a/><b/><a/>~</r>")]
    [Arguments("((a,b)*)", "<r><a/>~<a/></r>")]
    // One or more
    [Arguments("(a+)", "<r>~</r>")]
    [Arguments("(a+,b)", "<r><a/>~</r>")]
    [Arguments("((a|b)+)", "<r>~<c/></r>")]
    [Arguments("((a,b)+)", "<r><a/><b/><a/>~</r>")]
    // Nested and combined
    [Arguments("(a,(b|c)+,d?)", "<r><a/>~<d/></r>")]
    [Arguments("(a,(b|c)+,d?)", "<r><a/><b/><d/>~<d/></r>")]
    [Arguments("((a?,b?)*,c)", "<r><a/><b/>~</r>")]
    // The empty tag form puts the end of the content at the tag itself
    [Arguments("(a)", "~<r/>")]
    public async Task Children_RejectsAtTheFirstOffendingChild(string model, string markedScript)
    {
        var run = Run(markedScript, null, WithLeaves(Children("r", model)));

        await Assert.That(run.Issues.Count).IsEqualTo(1);
        await Assert.That(run.Issues[0].Code).IsEqualTo(DtdValidityCodes.XML5004.Code);
        await Assert.That(run.Issues[0].Location).IsEqualTo(run.Expected);
        await Assert.That(run.Result.IsValid).IsFalse();
    }
}
