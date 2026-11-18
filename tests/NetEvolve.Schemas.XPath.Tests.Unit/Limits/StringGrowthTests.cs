namespace NetEvolve.Schemas.XPath.Tests.Unit.Limits;

using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using static LimitsFixture;

/// <summary>
/// The string growth budget: the length of one produced string, charged before the string is built. <c>repeat(text, count)</c> is a
/// test-local stand-in that calls the same helper as the string functions of #263 and #355.
/// </summary>
public class StringGrowthTests
{
    [Test]
    public async Task String_BeyondTheDefault_EndsInTheCodedDiagnostic_AndNotInOutOfMemory()
    {
        var diagnostic = Evaluate("repeat('ab', 10000000000)", XPathLimits.Default, out var result);

        await Assert.That(diagnostic.QName).IsEqualTo(XPathLimitErrors.StringLengthExceeded);
        await Assert.That(diagnostic.Kind).IsEqualTo(XPathErrorKind.Engine);
        await Assert.That(result.IsEmpty).IsTrue();
    }

    [Test]
    public async Task String_ExactlyAtTheLimit_Succeeds()
    {
        var diagnostic = Evaluate("repeat('ab', 5)", XPathLimits.Default.WithMaxStringLength(10), out var result);

        await Assert.That(diagnostic.Code).IsEqualTo(string.Empty);
        _ = result[0].TryGetString(out var text);
        await Assert.That(text).IsEqualTo("ababababab");
    }

    [Test]
    public async Task String_OneCharacterBeyondTheLimit_EndsInTheDiagnostic()
    {
        var diagnostic = Evaluate("repeat('abc', 4)", XPathLimits.Default.WithMaxStringLength(11), out _);

        await Assert.That(diagnostic.QName).IsEqualTo(XPathLimitErrors.StringLengthExceeded);
    }

    [Test]
    public async Task String_BeyondTheLimit_NamesTheLimitAndTheValue_AndLocatesTheCall()
    {
        var diagnostic = Evaluate("(repeat('abc', 4))", XPathLimits.Default.WithMaxStringLength(11), out _);

        await Assert.That(diagnostic.Message).Contains("MaxStringLength");
        await Assert.That(diagnostic.Message).Contains("11");
        await Assert.That(diagnostic.Offset).IsEqualTo(1);
        await Assert.That(diagnostic.Length).IsEqualTo(16);
    }

    [Test]
    public async Task String_Empty_FitsTheSmallestLimit()
    {
        var diagnostic = Evaluate("repeat('', 100)", XPathLimits.Default.WithMaxStringLength(1), out var result);

        await Assert.That(diagnostic.Code).IsEqualTo(string.Empty);
        _ = result[0].TryGetString(out var text);
        await Assert.That(text).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ThrowIfStringTooLong_AtAndBeyondTheLimit()
    {
        var limits = XPathLimits.Default.WithMaxStringLength(10);

        limits.ThrowIfStringTooLong(0, new SourceSpan(0, 1));
        limits.ThrowIfStringTooLong(10, new SourceSpan(0, 1));
        var raised = ArithmeticFixture.Raised(() =>
        {
            limits.ThrowIfStringTooLong(11, new SourceSpan(0, 1));
            return default;
        });

        await Assert.That(raised).IsNotNull();
        await Assert.That(raised!.Diagnostic.QName).IsEqualTo(XPathLimitErrors.StringLengthExceeded);
    }
}
