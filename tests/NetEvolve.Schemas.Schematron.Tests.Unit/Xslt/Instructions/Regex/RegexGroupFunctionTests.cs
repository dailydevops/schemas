namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Regex;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Regex;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>regex-group()</c> (XSLT 2.0 section 15.2): the text of a group of the current match, read from the regex groups slot; the
/// zero-length string for an unset slot, a group number outside the groups, and a group that took no part in the match.
/// </summary>
public class RegexGroupFunctionTests
{
    // The subject "xabz", the whole match "ab" at 1..3, group 1 "a", group 2 "b", group 3 without a part in the match.
    private static readonly XPathRegexGroups Groups = new(
        "xabz",
        [new TextRange(1, 2), new TextRange(1, 1), new TextRange(2, 1), default]
    );

    private static string Call(XPathRegexGroups? groups, long number)
    {
        using var context = new XPathEvaluationContext();
        context.Host.RegexGroups = groups;
        var result = new RegexGroupFunction().Call(context, [XdmSequence.Create(XdmItem.FromInteger(number))]);
        _ = result[0].TryGetString(out var text);
        return text!;
    }

    [Test]
    public async Task Name_IsRegexGroup_WithOneArgument()
    {
        var function = new RegexGroupFunction();

        await Assert.That(function.Name.LocalName).IsEqualTo("regex-group");
        await Assert.That(function.MinArity).IsEqualTo(1);
        await Assert.That(function.MaxArity).IsEqualTo(1);
    }

    [Test]
    public async Task Call_GroupZero_IsTheWholeMatch() => await Assert.That(Call(Groups, 0)).IsEqualTo("ab");

    [Test]
    public async Task Call_NumberedGroups_AreTheirText()
    {
        await Assert.That(Call(Groups, 1)).IsEqualTo("a");
        await Assert.That(Call(Groups, 2)).IsEqualTo("b");
    }

    [Test]
    public async Task Call_GroupWithoutAPartInTheMatch_IsTheZeroLengthString() =>
        await Assert.That(Call(Groups, 3)).IsEqualTo(string.Empty);

    [Test]
    public async Task Call_NumberOutsideTheGroups_IsTheZeroLengthString()
    {
        await Assert.That(Call(Groups, 4)).IsEqualTo(string.Empty);
        await Assert.That(Call(Groups, -1)).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Call_WithAnUnsetSlot_IsTheZeroLengthString_NotAnError() =>
        await Assert.That(Call(null, 0)).IsEqualTo(string.Empty);
}
