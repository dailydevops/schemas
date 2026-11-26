namespace NetEvolve.Schemas.XPath.Tests.Unit.Casting.Core;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Decimals;
using NetEvolve.Schemas.Xml.Datatypes.Decimals.Arithmetic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static CastFixture;

/// <summary>Pins the decisions of this package that observable results depend on, and the exact decimal arithmetic the unblocked rules need.</summary>
public class CastDecisionTests
{
    [Test]
    public async Task DecimalSum_OfTwoConstructorResults_IsExact()
    {
        // The sum goes through the arithmetic lowering and the operator table of the numeric arithmetic package, not a stand-in.
        var sum = Evaluate("xs:decimal('1.10') + xs:decimal('2.20')");
        var expected = Evaluate("xs:decimal('3.30')");

        await Assert.That(sum).IsEqualTo(expected);
        await Assert.That(Render(sum[0])).IsEqualTo("decimal:3.3");
        _ = sum[0].TryGetDecimal(out var value);
        await Assert.That(value).IsEqualTo(XsdDecimal.Parse("3.30".AsSpan()).Value);
        await Assert.That(value.FractionDigits).IsEqualTo(1);
    }

    [Test]
    public async Task DecimalSum_OfDecimalLiterals_HasNoBinaryRoundingError()
    {
        var sum = Evaluate("0.1 + 0.2");

        await Assert.That(sum).IsEqualTo(Evaluate("0.3"));
    }

    // Decision D1: a double or float cast to xs:decimal is the exact binary value (numerically closest, as the specification says),
    // not the shortest digits that round-trip.
    [Test]
    public async Task DoubleToDecimal_IsTheExactBinaryValue()
    {
        await Assert
            .That(EvaluateRendered("xs:double('0.1') cast as xs:decimal"))
            .IsEqualTo("decimal:0.1000000000000000055511151231257827021181583404541015625");
        await Assert
            .That(EvaluateRendered("xs:float('0.1') cast as xs:decimal"))
            .IsEqualTo("decimal:0.100000001490116119384765625");
        await Assert.That(EvaluateRendered("xs:double('0.5') cast as xs:decimal")).IsEqualTo("decimal:0.5");
        await Assert
            .That(EvaluateRendered("xs:double('1e20') cast as xs:decimal"))
            .IsEqualTo("decimal:100000000000000000000");
    }

    // Decision D2: a decimal is rounded once to a float, not through a double. This value lies just above the middle between 1 and
    // the next float; as a double it would round down onto the middle and then to even, which gives 1.
    [Test]
    public async Task DecimalToFloat_RoundsOnce()
    {
        await Assert
            .That(EvaluateRendered("xs:float(1.0000000596046447753906250000000001)"))
            .IsEqualTo("float:1.0000001");
        await Assert.That(EvaluateRendered("xs:float(1.00000005960464477539062500)")).IsEqualTo("float:1");
    }

    // Decision D3: a decimal beyond the 64 bits of the integer item raises FOCA0003 (an allowed cell can still reach the limit of the implementation).
    [Test]
    public async Task DecimalToInteger_BeyondTheItemWidth_RaisesFoca0003()
    {
        await Assert.That(Code("xs:decimal('9223372036854775808') cast as xs:integer")).IsEqualTo("FOCA0003");
        await Assert
            .That(CastingCatalog.Default.Table.GetCell(XdmTypeId.XsDecimal, XdmTypeId.XsInteger))
            .IsEqualTo(CastCell.Always);
    }

    // Decision D4: a built-in atomic type without a row is rejected while compiling, with XPST0051 and a message that says why. Every built-in atomic
    // type of the default table has its rows; the table of this test has none for xs:dateTimeStamp, a type of XPath 3.1.
    [Test]
    [Arguments("xs:dateTimeStamp", "xs:dateTimeStamp")]
    public async Task BuiltInTypeWithoutARow_IsRejectedWithAMessageThatNamesTheLimit(string type, string named)
    {
        var expression = "'x' cast as " + type;
        _ = XPathParser.TryParse(expression.AsSpan(), out var syntax, out _);
        var statics = Statics.ToBuilder().WithVersion(XPathVersion.V31).Build();
        var table = new CastingCatalog().Table;
        _ = ExpressionCompiler.TryCompile(syntax!, expression, statics, Registry(table), out _, out var diagnostic);

        await Assert.That(diagnostic.Code).IsEqualTo("XPST0051");
        await Assert.That(diagnostic.Message).Contains(named);
        await Assert.That(diagnostic.Message).Contains("not supported yet");
    }

    // Decision D5: a lexical number beyond the digit cap of the datatype layer is an implementation limit, not an invalid form.
    [Test]
    public async Task DigitCap_IsAnImplementationLimit_NotAnInvalidLexicalForm()
    {
        var beyond = new string('1', XsdDecimal.DefaultMaxDigits + 1);

        await Assert.That(Code("'" + beyond + "' cast as xs:decimal")).IsEqualTo("FOCA0001");
        await Assert.That(Code("'" + beyond + "' cast as xs:integer")).IsEqualTo("FOCA0003");
        await Assert
            .That(Code("'" + new string('1', XsdDecimal.DefaultMaxDigits) + "' cast as xs:decimal"))
            .IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ExactConversion_MatchesTheDatatypeLayer()
    {
        var expected = DecimalArithmetic.FromDouble(0.1).Value;

        _ = Evaluate("xs:double('0.1') cast as xs:decimal")[0].TryGetDecimal(out var value);

        await Assert.That(value).IsEqualTo(expected);
    }
}
