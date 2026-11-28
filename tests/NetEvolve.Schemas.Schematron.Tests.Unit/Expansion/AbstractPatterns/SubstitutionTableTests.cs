namespace NetEvolve.Schemas.Schematron.Tests.Unit.Expansion.AbstractPatterns;

using System;
using NetEvolve.Schemas.Schematron.Expansion.AbstractPatterns;

public class SubstitutionTableTests
{
    [Test]
    public async Task Apply_LongestNameWins_WhenNamesArePrefixesOfEachOther()
    {
        var table = new SubstitutionTable(["a", "ab"], ["1", "2"]);

        await Assert.That(table.Apply("$ab")).IsEqualTo("2");
        await Assert.That(table.Apply("$a")).IsEqualTo("1");
        await Assert.That(table.Apply("$ac")).IsEqualTo("1c");
    }

    [Test]
    public async Task Apply_LongestNameWins_WhenTheLongerNameIsListedFirst()
    {
        var table = new SubstitutionTable(["ab", "a"], ["2", "1"]);

        await Assert.That(table.Apply("$ab $a")).IsEqualTo("2 1");
    }

    [Test]
    public async Task Apply_FirstDuplicateWins()
    {
        var table = new SubstitutionTable(["x", "x"], ["first", "second"]);

        await Assert.That(table.Apply("$x")).IsEqualTo("first");
    }

    [Test]
    public async Task Apply_MatchesWithoutNameBoundary()
    {
        var table = new SubstitutionTable(["a"], ["1"]);

        await Assert.That(table.Apply("$abc")).IsEqualTo("1bc");
    }

    [Test]
    public async Task Apply_IgnoresEmptyNames_AndReturnsSameInstanceWithoutMatch()
    {
        var table = new SubstitutionTable([string.Empty, "a"], ["e", "1"]);
        const string text = "$ $b a$";

        await Assert.That(table.Apply(text)).IsSameReferenceAs(text);
        await Assert.That(table.Apply("$$a$")).IsEqualTo("$1$");
    }

    [Test]
    public async Task Apply_DoesNotScanReplacedValuesAgain()
    {
        var table = new SubstitutionTable(["a", "b"], ["$b", "x"]);

        await Assert.That(table.Apply("$a$b")).IsEqualTo("$bx");
    }

    [Test]
    public async Task Apply_NameAtTheEndOfTheText()
    {
        var table = new SubstitutionTable(["abc"], ["v"]);

        await Assert.That(table.Apply("x$abc")).IsEqualTo("xv");
        await Assert.That(table.Apply("x$ab")).IsEqualTo("x$ab");
    }

    [Test]
    public void Apply_ThrowsOnNull()
    {
        var table = new SubstitutionTable(["a"], ["1"]);

        _ = Assert.Throws<ArgumentNullException>(() => table.Apply(null!));
    }
}
