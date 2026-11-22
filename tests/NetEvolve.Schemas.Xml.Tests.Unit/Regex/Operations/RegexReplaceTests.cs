namespace NetEvolve.Schemas.Xml.Tests.Unit.Regex.Operations;

using System;
using NetEvolve.Schemas.Xml.Regex.Contracts;
using NetEvolve.Schemas.Xml.Regex.Operations;
using NetEvolve.Schemas.Xml.Regex.XPathDialect;

public class RegexReplaceTests
{
    [Test]
    [Arguments("a", "banana", "-", "b-n-n-")]
    [Arguments("a+", "baanaa", "-", "b-n-")]
    [Arguments("x", "banana", "-", "banana")]
    [Arguments("a", "", "-", "")]
    [Arguments("a", "a", "", "")]
    [Arguments("a", "aaa", "b", "bbb")]
    [Arguments("^a", "aaa", "b", "baa")]
    [Arguments("a$", "aaa", "b", "aab")]
    [Arguments("b", "abc", "[]", "a[]c")]
    public async Task Replace_SubstitutesEveryNonOverlappingMatch(
        string pattern,
        string input,
        string replacement,
        string expected
    ) => await Assert.That(RegexOps.Replace(pattern, input, replacement)).IsEqualTo(expected);

    [Test]
    [Arguments("(a)(b)", "ab", "$2$1", "ba")]
    [Arguments("(a)(b)", "xaby", "[$0]", "x[ab]y")]
    [Arguments("(a)(b)", "ab", "$0$0", "abab")]
    [Arguments("(a)", "a", "$1$1$1", "aaa")]
    [Arguments("(a)", "a", "<$1>", "<a>")]
    [Arguments("(a)|(b)", "b", "[$1]", "[]")]
    [Arguments("(a)|(b)", "b", "[$2]", "[b]")]
    [Arguments("(a)", "a", "$2", "")]
    [Arguments("(a)", "a", "$9", "")]
    [Arguments("a", "a", "$1", "")]
    [Arguments("a", "a", "$0", "a")]
    public async Task Replace_ResolvesGroupReferences(
        string pattern,
        string input,
        string replacement,
        string expected
    ) => await Assert.That(RegexOps.Replace(pattern, input, replacement)).IsEqualTo(expected);

    [Test]
    [Arguments("(a)", "a", "$10", "a0")]
    [Arguments("(a)", "a", "$11", "a1")]
    [Arguments("(a)", "a", "$19", "a9")]
    [Arguments("(a)", "a", "$100", "a00")]
    [Arguments("(a)(b)(c)(d)(e)(f)(g)(h)(i)(j)", "abcdefghij", "$10", "j")]
    [Arguments("(a)(b)(c)(d)(e)(f)(g)(h)(i)(j)", "abcdefghij", "$1", "a")]
    [Arguments("(a)(b)(c)(d)(e)(f)(g)(h)(i)(j)", "abcdefghij", "$11", "a1")]
    [Arguments("(a)(b)(c)(d)(e)(f)(g)(h)(i)(j)", "abcdefghij", "$10x", "jx")]
    public async Task Replace_TakesAFurtherDigitOnlyWhileTheGroupExists(
        string pattern,
        string input,
        string replacement,
        string expected
    ) => await Assert.That(RegexOps.Replace(pattern, input, replacement)).IsEqualTo(expected);

    [Test]
    [Arguments("a", "a", "\\$", "$")]
    [Arguments("a", "a", "\\\\", "\\")]
    [Arguments("a", "a", "\\$1", "$1")]
    [Arguments("(a)", "a", "\\\\$1", "\\a")]
    [Arguments("a", "a", "\\$\\\\\\$", "$\\$")]
    [Arguments("a", "a", "x\\$y", "x$y")]
    public async Task Replace_UnescapesDollarAndBackslash(
        string pattern,
        string input,
        string replacement,
        string expected
    ) => await Assert.That(RegexOps.Replace(pattern, input, replacement)).IsEqualTo(expected);

    [Test]
    [Arguments("$")]
    [Arguments("a$")]
    [Arguments("$a")]
    [Arguments("$ ")]
    [Arguments("$$")]
    [Arguments("\\")]
    [Arguments("a\\")]
    [Arguments("\\a")]
    [Arguments("\\1")]
    [Arguments("\\\\\\")]
    public async Task Replace_WithAnInvalidReplacementString_IsTheCodedError(string replacement)
    {
        await Assert.That(RegexOps.Replace("a", "a", replacement)).IsEqualTo("InvalidReplacement");
        await Assert.That(RegexOps.Replace("a", "b", replacement)).IsEqualTo("InvalidReplacement");
        await Assert.That(RegexOps.Replace("a", "", replacement)).IsEqualTo("InvalidReplacement");
    }

    [Test]
    [Arguments("a*")]
    [Arguments("(a)?")]
    [Arguments("^")]
    [Arguments("$")]
    [Arguments("(a|)")]
    [Arguments("")]
    [Arguments("a{0,2}")]
    [Arguments("(a)?\\1")]
    public async Task Replace_WithAPatternThatMatchesTheEmptyString_IsTheCodedError(string pattern)
    {
        await Assert.That(RegexOps.Replace(pattern, "aaa", "x")).IsEqualTo("ZeroLengthMatch");
        await Assert.That(RegexOps.Replace(pattern, "", "x")).IsEqualTo("ZeroLengthMatch");
    }

    [Test]
    public async Task Replace_ReportsAZeroLengthMatchBeforeAnInvalidReplacement() =>
        await Assert.That(RegexOps.Replace("a*", "a", "$")).IsEqualTo("ZeroLengthMatch");

    [Test]
    public async Task Replace_WithTheFlags_UsesThem()
    {
        await Assert.That(RegexOps.Replace("A", "aA", "x", RegexOptions.IgnoreCase)).IsEqualTo("xx");
        await Assert.That(RegexOps.Replace("^a", "a\na", "x", RegexOptions.MultiLine)).IsEqualTo("x\nx");
        await Assert
            .That(RegexOps.Replace("a . b", "a\nb", "x", RegexOptions.DotAll | RegexOptions.FreeSpacing))
            .IsEqualTo("x");
    }

    [Test]
    public async Task Replace_OfALongInput_GrowsItsBuffer()
    {
        var input = RegexOps.Repeat("ab", 5_000);
        var expected = RegexOps.Repeat("[ab]", 5_000);

        await Assert.That(RegexOps.Replace("(a)(b)", input, "[$1$2]")).IsEqualTo(expected);
        await Assert.That(RegexOps.Replace("a", input, RegexOps.Repeat("z", 50))).Length().IsEqualTo(5_000 * 51);
    }

    [Test]
    public async Task Replace_ThatReachesTheCeiling_IsAborted()
    {
        var limits = RegexStepLimits.Default.WithMaxNfaSteps(50);

        await Assert
            .That(RegexOps.Replace("(a+)+b", RegexOps.Repeat("a", 100), "x", limits: limits))
            .IsEqualTo("Aborted");
        await Assert.That(RegexOps.Replace("a*b", RegexOps.Repeat("a", 100), "x", limits: limits)).IsEqualTo("Aborted");
    }

    [Test]
    public async Task Replace_IntoADestination_WritesTheResult()
    {
        var operations = RegexOps.Create("(a)(b)");
        var destination = new char[16];

        var status = Replace(operations, "xaby", "[$2$1]", destination, out var written);

        await Assert.That(status).IsEqualTo(RegexOperationStatus.Success);
        await Assert.That(new string(destination, 0, written)).IsEqualTo("x[ba]y");
    }

    [Test]
    public async Task Replace_IntoADestinationOfTheExactSize_Fits()
    {
        var operations = RegexOps.Create("a");
        var destination = new char[3];

        var status = Replace(operations, "bab", "-", destination, out var written);

        await Assert.That(status).IsEqualTo(RegexOperationStatus.Success);
        await Assert.That(new string(destination, 0, written)).IsEqualTo("b-b");
    }

    [Test]
    public async Task Replace_IntoADestinationThatIsTooSmall_ReportsTheRequiredLength()
    {
        var operations = RegexOps.Create("a");
        var destination = new char[2];

        var status = Replace(operations, "bab", "-", destination, out var written);

        await Assert.That(status).IsEqualTo(RegexOperationStatus.DestinationTooSmall);
        await Assert.That(written).IsEqualTo(3);
    }

    [Test]
    public async Task Replace_IntoADestination_ReportsTheOtherOutcomes()
    {
        var destination = new char[8];

        await Assert
            .That(Replace(RegexOps.Create("a*"), "a", "x", destination, out var zero))
            .IsEqualTo(RegexOperationStatus.ZeroLengthMatch);
        await Assert
            .That(Replace(RegexOps.Create("a"), "a", "$", destination, out var invalid))
            .IsEqualTo(RegexOperationStatus.InvalidReplacement);
        await Assert
            .That(
                Replace(
                    RegexOps.Create("a*b"),
                    RegexOps.Repeat("a", 100),
                    "x",
                    destination,
                    out var aborted,
                    RegexStepLimits.Default.WithMaxNfaSteps(50)
                )
            )
            .IsEqualTo(RegexOperationStatus.Aborted);
        await Assert.That(zero).IsEqualTo(0);
        await Assert.That(invalid).IsEqualTo(0);
        await Assert.That(aborted).IsEqualTo(0);
    }

    [Test]
    public async Task Status_CodesAreTheRxNumbers()
    {
        await Assert.That((int)RegexOperationStatus.Success).IsEqualTo(0);
        await Assert.That((int)RegexOperationStatus.ZeroLengthMatch).IsEqualTo(12);
        await Assert.That((int)RegexOperationStatus.InvalidReplacement).IsEqualTo(13);
    }

    private static RegexOperationStatus Replace(
        RegexOperations operations,
        string input,
        string replacement,
        char[] destination,
        out int written,
        RegexStepLimits? limits = null
    ) => operations.Replace(input, replacement, destination, limits, out written);
}
