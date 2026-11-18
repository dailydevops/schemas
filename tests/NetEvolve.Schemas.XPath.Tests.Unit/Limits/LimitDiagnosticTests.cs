namespace NetEvolve.Schemas.XPath.Tests.Unit.Limits;

using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;

/// <summary>One coded diagnostic per budget: the code, the kind, the limit name and configured value in the message, and the location.</summary>
public class LimitDiagnosticTests
{
    private static readonly XPathLimits Small = XPathLimits
        .Default.WithMaxRecursionDepth(2)
        .WithMaxSequenceSize(3)
        .WithMaxRangeSize(4)
        .WithMaxStringLength(5);

    private static readonly XPathErrorCode[] Codes =
    [
        XPathLimitErrors.RecursionDepthExceeded,
        XPathLimitErrors.SequenceSizeExceeded,
        XPathLimitErrors.RangeSizeExceeded,
        XPathLimitErrors.StringLengthExceeded,
    ];

    [Test]
    public async Task EachBudget_HasItsOwnCode_InTheEngineRange()
    {
        await Assert.That(Codes.Distinct().Count()).IsEqualTo(4);
        await Assert.That(Codes[0].ToString()).IsEqualTo("{urn:netevolve:schemas:xpath}XP0003");
        await Assert.That(Codes[1].ToString()).IsEqualTo("{urn:netevolve:schemas:xpath}XP0004");
        await Assert.That(Codes[2].ToString()).IsEqualTo("{urn:netevolve:schemas:xpath}XP0005");
        await Assert.That(Codes[3].ToString()).IsEqualTo("{urn:netevolve:schemas:xpath}XP0006");
    }

    [Test]
    public async Task TheLimitCodes_AreEngineErrors_AndDoNotCollideWithTheTableOrTheLoweringCode()
    {
        foreach (var code in Codes)
        {
            await Assert.That(XPathErrorCodes.All.Contains(code)).IsFalse();
            await Assert.That(XPathErrorCodes.KindOf(code)).IsEqualTo(XPathErrorKind.Engine);
            await Assert.That(code).IsNotEqualTo(LoweringContext.NoLowering);
        }
    }

    [Test]
    public async Task Depth_CarriesTheLimitName_TheConfiguredValue_AndTheLocation()
    {
        var raised = ArithmeticFixture.Raised(() =>
        {
            Small.ThrowIfTooDeep(3, new SourceSpan(11, 4));
            return default;
        });

        await AssertDiagnostic(raised, XPathLimitErrors.RecursionDepthExceeded, "MaxRecursionDepth", "2", 11, 4);
    }

    [Test]
    public async Task Range_CarriesTheLimitName_TheConfiguredValue_AndTheLocation()
    {
        var raised = ArithmeticFixture.Raised(() =>
        {
            Small.ThrowIfRangeTooLarge(1, 5, new SourceSpan(12, 5));
            return default;
        });

        await AssertDiagnostic(raised, XPathLimitErrors.RangeSizeExceeded, "MaxRangeSize", "4", 12, 5);
    }

    [Test]
    public async Task String_CarriesTheLimitName_TheConfiguredValue_AndTheLocation()
    {
        var raised = ArithmeticFixture.Raised(() =>
        {
            Small.ThrowIfStringTooLong(6, new SourceSpan(13, 6));
            return default;
        });

        await AssertDiagnostic(raised, XPathLimitErrors.StringLengthExceeded, "MaxStringLength", "5", 13, 6);
    }

    [Test]
    public async Task Sequence_CarriesTheLimitName_TheConfiguredValue_AndTheLocation()
    {
        var exception = XPathLimitErrors.SequenceSize(3, new SourceSpan(14, 7));

        await AssertDiagnostic(exception, XPathLimitErrors.SequenceSizeExceeded, "MaxSequenceSize", "3", 14, 7);
    }

    [Test]
    public async Task ALocationThatIsNotKnown_IsReportedAsMinusOne()
    {
        var exception = XPathLimitErrors.SequenceSize(3, new SourceSpan(-1, 0));

        await Assert.That(exception.Diagnostic.Offset).IsEqualTo(-1);
        await Assert.That(exception.Diagnostic.Length).IsEqualTo(0);
    }

    private static async Task AssertDiagnostic(
        XPathException? exception,
        XPathErrorCode code,
        string name,
        string value,
        int offset,
        int length
    )
    {
        await Assert.That(exception).IsNotNull();
        var diagnostic = exception!.Diagnostic;
        await Assert.That(diagnostic.QName).IsEqualTo(code);
        await Assert.That(diagnostic.Kind).IsEqualTo(XPathErrorKind.Engine);
        await Assert.That(diagnostic.Message).Contains(name);
        await Assert.That(diagnostic.Message).Contains(value);
        await Assert.That(diagnostic.Offset).IsEqualTo(offset);
        await Assert.That(diagnostic.Length).IsEqualTo(length);
    }
}
