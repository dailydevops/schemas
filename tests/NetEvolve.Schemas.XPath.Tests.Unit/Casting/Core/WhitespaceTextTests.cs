namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.XPath.Casting.Core;

/// <summary>The whitespace of the <c>whiteSpace</c> facet applied to a string, shared by the casts to derived strings and to <c>xs:anyURI</c>.</summary>
public class WhitespaceTextTests
{
    [Test]
    [Arguments(WhitespaceMode.Preserve, "  a \t b \n c \r d  ", "  a \t b \n c \r d  ")]
    [Arguments(WhitespaceMode.Replace, "  a \t b \n c \r d  ", "  a   b   c   d  ")]
    [Arguments(WhitespaceMode.Replace, " a  b ", " a  b ")]
    [Arguments(WhitespaceMode.Replace, "\t", " ")]
    [Arguments(WhitespaceMode.Collapse, "  a \t b \n c \r d  ", "a b c d")]
    [Arguments(WhitespaceMode.Collapse, "a b  c", "a b c")]
    [Arguments(WhitespaceMode.Collapse, " a", "a")]
    [Arguments(WhitespaceMode.Collapse, "a ", "a")]
    [Arguments(WhitespaceMode.Collapse, "a\tb\nc", "a b c")]
    [Arguments(WhitespaceMode.Collapse, "\t", "")]
    [Arguments(WhitespaceMode.Collapse, "abc", "abc")]
    [Arguments(WhitespaceMode.Collapse, "", "")]
    public async Task Apply_NormalizesTheLexicalFormAsTheModeStates(
        WhitespaceMode mode,
        string lexical,
        string expected
    ) => await Assert.That(WhitespaceText.Apply(lexical, mode)).IsEqualTo(expected);

    // A text longer than any scratch buffer takes the rented path.
    [Test]
    public async Task Apply_NormalizesALongText()
    {
        var lexical = " " + new string('a', 5000) + "\t\t" + new string('b', 5000) + " ";

        await Assert
            .That(WhitespaceText.Apply(lexical, WhitespaceMode.Collapse))
            .IsEqualTo(new string('a', 5000) + " " + new string('b', 5000));
    }

    [Test]
    [Arguments(WhitespaceMode.Preserve)]
    [Arguments(WhitespaceMode.Replace)]
    [Arguments(WhitespaceMode.Collapse)]
    public async Task Apply_ReturnsTheSameString_WhenNothingChanges(WhitespaceMode mode)
    {
        var text = new string('x', 5);

        await Assert.That(WhitespaceText.Apply(text, mode)).IsSameReferenceAs(text);
    }

    [Test]
    public async Task Apply_Guard_RejectsNull() =>
        await Assert.That(() => WhitespaceText.Apply(null!, WhitespaceMode.Collapse)).Throws<ArgumentNullException>();
}
