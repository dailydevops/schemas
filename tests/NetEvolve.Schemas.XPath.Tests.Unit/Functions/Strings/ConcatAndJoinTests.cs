namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings.StringsHarness;

/// <summary><c>fn:concat</c> and <c>fn:string-join</c> (Functions and Operators 1.0, 7.2.1 and 7.2.2): the examples of the specification, the empty sequence, and the empty string.</summary>
public class ConcatAndJoinTests
{
    [Test]
    public async Task Concat_TwoStrings_JoinsThem() =>
        await Assert.That(Text("concat", Str("un"), Str("grateful"))).IsEqualTo("ungrateful");

    [Test]
    public async Task Concat_SpecificationExample_TreatsEmptyArgumentsAsTheEmptyString() =>
        await Assert
            .That(
                Text(
                    "concat",
                    Str("Thy "),
                    XdmSequence.Empty,
                    Str("old "),
                    Str("groans"),
                    Str(""),
                    Str(" ring"),
                    Str(" yet"),
                    Str(" again")
                )
            )
            .IsEqualTo("Thy old groans ring yet again");

    [Test]
    public async Task Concat_TwoEmptySequences_IsTheEmptyString() =>
        await Assert.That(Shown("concat", XdmSequence.Empty, XdmSequence.Empty)).IsEqualTo("string:");

    [Test]
    public async Task Concat_AtomicValuesOfOtherTypes_UseTheirStringValue() =>
        await Assert
            .That(Text("concat", Int(1), Dbl(2.5), XdmSequence.Create(XdmItem.FromBoolean(true))))
            .IsEqualTo("12.5true");

    [Test]
    public async Task Concat_SupplementaryCharacters_AreKeptWhole() =>
        await Assert.That(Text("concat", Str("a\U0001F600"), Str("b"))).IsEqualTo("a\U0001F600b");

    [Test]
    public async Task Concat_ManyArguments_JoinsAllOfThem()
    {
        var arguments = new XdmSequence[50];
        for (var i = 0; i < arguments.Length; i++)
        {
            arguments[i] = Str("x");
        }

        await Assert.That(Text("concat", arguments)).IsEqualTo(new string('x', 50));
    }

    [Test]
    public async Task Concat_AnArgumentOfTwoItems_IsXPTY0004() =>
        await Assert.That(Text("concat", Strs("a", "b"), Str("c"))).IsEqualTo("error:XPTY0004");

    [Test]
    public async Task Concat_OneArgument_IsNotRegistered() =>
        await Assert.That(Text("concat", Str("a"))).IsEqualTo("error:not-registered");

    [Test]
    public async Task Join_SpecificationExamples()
    {
        await Assert
            .That(Text("string-join", Strs("Now", "is", "the", "time", "..."), Str(" ")))
            .IsEqualTo("Now is the time ...");
        await Assert
            .That(Text("string-join", Strs("Blow, ", "blow, ", "thou ", "winter ", "wind!"), Str("")))
            .IsEqualTo("Blow, blow, thou winter wind!");
        await Assert.That(Text("string-join", XdmSequence.Empty, Str("separator"))).IsEqualTo("");
    }

    [Test]
    public async Task Join_OneItem_IsThatItemWithoutSeparator() =>
        await Assert.That(Text("string-join", Str("only"), Str(", "))).IsEqualTo("only");

    [Test]
    public async Task Join_EmptyStrings_StillGetSeparators() =>
        await Assert.That(Text("string-join", Strs("", "", ""), Str("-"))).IsEqualTo("--");

    [Test]
    public async Task Join_SupplementarySeparator_IsKeptWhole() =>
        await Assert.That(Text("string-join", Strs("a", "b"), Str("\U0001F600"))).IsEqualTo("a\U0001F600b");

    [Test]
    public async Task Join_EmptySeparatorArgument_IsXPTY0004() =>
        await Assert.That(Text("string-join", Strs("a", "b"), XdmSequence.Empty)).IsEqualTo("error:XPTY0004");

    [Test]
    public async Task Join_OneArgument_IsNotRegistered() =>
        await Assert.That(Text("string-join", Strs("a", "b"))).IsEqualTo("error:not-registered");
}
