namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings;

using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.XPath.Tests.Unit.Functions.Strings.StringsHarness;

/// <summary><c>fn:substring</c> (Functions and Operators 1.0, 7.4.3): positions count code points, and the window follows the rounding rules of <c>fn:round</c>.</summary>
public class SubstringTests
{
    [Test]
    [Arguments("motor car", 6.0, double.NaN, " car")]
    [Arguments("metadata", 4.0, 3.0, "ada")]
    [Arguments("12345", 1.5, 2.6, "234")]
    [Arguments("12345", 0.0, 3.0, "12")]
    [Arguments("12345", 5.0, -3.0, "")]
    [Arguments("12345", -3.0, 5.0, "1")]
    [Arguments("12345", 2.0, double.NaN, "2345")]
    [Arguments("12345", 1.0, 5.0, "12345")]
    [Arguments("12345", 1.0, 6.0, "12345")]
    [Arguments("12345", 6.0, double.NaN, "")]
    [Arguments("12345", 5.0, double.NaN, "5")]
    [Arguments("12345", 0.0, double.NaN, "12345")]
    [Arguments("12345", -100.0, double.NaN, "12345")]
    [Arguments("12345", 2.0, 0.0, "")]
    [Arguments("", 1.0, 3.0, "")]
    public async Task Substring_ExamplesAndEdges(string source, double start, double length, string expected)
    {
        // NaN stands for "no length argument" in this table: the NaN length itself has its own test.
        var result = double.IsNaN(length)
            ? Text("substring", Str(source), Dbl(start))
            : Text("substring", Str(source), Dbl(start), Dbl(length));

        await Assert.That(result).IsEqualTo(expected);
    }

    [Test]
    [Arguments(0.5, 1.0, "1")] // round(0.5) = 1, so the window is [1, 2).
    [Arguments(1.4, 1.0, "1")] // round(1.4) = 1.
    [Arguments(1.5, 1.0, "2")] // round(1.5) = 2: halves round toward positive infinity.
    [Arguments(-0.5, 3.0, "12")] // round(-0.5) = 0, so the window is [0, 3).
    [Arguments(0.49999999999999994, 1.0, "")] // round gives 0: no value is pushed over a boundary by an addition.
    [Arguments(2.5, 2.5, "345")] // round(2.5) = 3 and round(2.5) = 3: the window is [3, 6).
    public async Task Substring_FractionalArguments_RoundHalfTowardPositiveInfinity(
        double start,
        double length,
        string expected
    ) => await Assert.That(Text("substring", Str("12345"), Dbl(start), Dbl(length))).IsEqualTo(expected);

    [Test]
    public async Task Substring_NaNStart_IsEmpty() =>
        await Assert.That(Text("substring", Str("12345"), Dbl(double.NaN), Dbl(3))).IsEqualTo("");

    [Test]
    public async Task Substring_NaNStartWithoutLength_IsEmpty() =>
        await Assert.That(Text("substring", Str("12345"), Dbl(double.NaN))).IsEqualTo("");

    [Test]
    public async Task Substring_NaNLength_IsEmpty() =>
        await Assert.That(Text("substring", Str("12345"), Dbl(1), Dbl(double.NaN))).IsEqualTo("");

    [Test]
    public async Task Substring_InfiniteLength_TakesEverythingFromTheStart()
    {
        await Assert.That(Text("substring", Str("12345"), Dbl(-42), Dbl(double.PositiveInfinity))).IsEqualTo("12345");
        await Assert.That(Text("substring", Str("12345"), Dbl(3), Dbl(double.PositiveInfinity))).IsEqualTo("345");
    }

    [Test]
    public async Task Substring_NegativeInfiniteStartWithInfiniteLength_IsEmpty_BecauseTheSumIsNaN() =>
        await Assert
            .That(Text("substring", Str("12345"), Dbl(double.NegativeInfinity), Dbl(double.PositiveInfinity)))
            .IsEqualTo("");

    [Test]
    public async Task Substring_NegativeInfiniteLength_IsEmpty() =>
        await Assert.That(Text("substring", Str("12345"), Dbl(1), Dbl(double.NegativeInfinity))).IsEqualTo("");

    [Test]
    public async Task Substring_PositiveInfiniteStart_IsEmpty() =>
        await Assert.That(Text("substring", Str("12345"), Dbl(double.PositiveInfinity))).IsEqualTo("");

    [Test]
    public async Task Substring_NegativeInfiniteStartWithoutLength_IsEverything() =>
        await Assert.That(Text("substring", Str("12345"), Dbl(double.NegativeInfinity))).IsEqualTo("12345");

    [Test]
    public async Task Substring_EmptySequence_IsTheEmptyString() =>
        await Assert.That(Shown("substring", XdmSequence.Empty, Dbl(1), Dbl(3))).IsEqualTo("string:");

    [Test]
    public async Task Substring_IntegerArguments_ArePromotedToDouble() =>
        await Assert.That(Text("substring", Str("abcdef"), Int(2), Int(3))).IsEqualTo("bcd");

    [Test]
    public async Task Substring_ArgumentOfTheWrongType_IsXPTY0004() =>
        await Assert.That(Text("substring", Str("abc"), Str("1"))).IsEqualTo("error:XPTY0004");

    [Test]
    public async Task Substring_MissingStart_IsXPTY0004() =>
        await Assert.That(Text("substring", Str("abc"), XdmSequence.Empty)).IsEqualTo("error:XPTY0004");

    [Test]
    public async Task Substring_OneArgument_IsNotRegistered() =>
        await Assert.That(Text("substring", Str("abc"))).IsEqualTo("error:not-registered");

    [Test]
    public async Task Substring_ThatKeepsTheWholeString_ReturnsThatString()
    {
        var source = new string(['a', 'b', 'c']);
        var result = Call("substring", Str(source), Dbl(1));

        await Assert.That(result[0].TryGetString(out var text)).IsTrue();
        await Assert.That(ReferenceEquals(text, source)).IsTrue();
    }
}
