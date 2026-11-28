namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Regex;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Regex.Rx;

/// <summary>
/// The compile of <c>xsl:analyze-string</c> (XSLT 2.0 section 15.1): <see langword="select"/> and <c>regex</c> are required, the content is an
/// optional <c>xsl:matching-substring</c> and an optional <c>xsl:non-matching-substring</c> in this order, at least one of them
/// (XTSE1130), <c>xsl:fallback</c> is ignored, and every problem is reported without stopping the compile.
/// </summary>
public class AnalyzeStringCompileTests
{
    private static string Code(DiagnosticCode code, string? constraint = null) =>
        code + (constraint is null ? string.Empty : "/" + constraint);

    [Test]
    public async Task Compile_WithoutSelect_IsTheMissingAttributeError()
    {
        var harness = new RegexHarness(Analyze(null, "b", null, 10, Match(11, Out("'x'", 12))));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo(Code(SchematronDiagnosticCodes.AttributeMissing));
    }

    [Test]
    public async Task Compile_WithoutRegex_IsTheMissingAttributeError()
    {
        var harness = new RegexHarness(Analyze("'a'", null, null, 10, Match(11, Out("'x'", 12))));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo(Code(SchematronDiagnosticCodes.AttributeMissing));
    }

    [Test]
    public async Task Compile_WithoutSelectAndRegex_ReportsBoth()
    {
        var harness = new RegexHarness(Analyze(null, null, null, 10, Match(11, Out("'x'", 12))));

        await Assert
            .That(harness.CompileCodes)
            .IsEqualTo(
                Code(SchematronDiagnosticCodes.AttributeMissing)
                    + ","
                    + Code(SchematronDiagnosticCodes.AttributeMissing)
            );
    }

    [Test]
    public async Task Compile_WithoutAnySubstringElement_IsXTSE1130()
    {
        var harness = new RegexHarness(Analyze("'a'", "b", null, 10));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).IsEqualTo(Code(SchematronDiagnosticCodes.ElementMisplaced, "XTSE1130"));
    }

    [Test]
    public async Task Compile_WithBothSubstringElements_IsValid()
    {
        var harness = new RegexHarness(Brackets("'a'", "b", "i", 10));

        await Assert.That(harness.CompileCodes).IsEqualTo(string.Empty);
        await Assert.That(harness.Executable).IsNotNull();
    }

    [Test]
    public async Task Compile_WithWhiteSpaceBetweenTheChildren_IsValid()
    {
        var harness = new RegexHarness(Analyze("'a'", "b", null, 10, "\n  ", Match(11, Out("'x'", 12)), "\n"));

        await Assert.That(harness.CompileCodes).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Compile_WithTextContent_IsMisplaced()
    {
        var harness = new RegexHarness(Analyze("'a'", "b", null, 10, "text", Match(11, Out("'x'", 12))));

        await Assert.That(harness.CompileCodes).IsEqualTo(Code(SchematronDiagnosticCodes.ElementMisplaced, "XTSE0010"));
    }

    [Test]
    public async Task Compile_WithAnotherChildElement_IsMisplaced()
    {
        var harness = new RegexHarness(Analyze("'a'", "b", null, 10, Out("'x'", 11), Match(12, Out("'x'", 13))));

        await Assert.That(harness.CompileCodes).IsEqualTo(Code(SchematronDiagnosticCodes.ElementMisplaced, "XTSE0010"));
    }

    [Test]
    public async Task Compile_WithFallback_IgnoresTheFallback()
    {
        var harness = new RegexHarness(Analyze("'a'", "b", null, 10, Match(11, Out("'x'", 12)), X("fallback", 13)));

        await Assert.That(harness.CompileCodes).IsEqualTo(string.Empty);
        await Assert.That(harness.Executable).IsNotNull();
    }

    [Test]
    public async Task Compile_WithTheMatchingSubstringTwice_IsMisplaced()
    {
        var harness = new RegexHarness(Analyze("'a'", "b", null, 10, Match(11, Out("'x'", 12)), Match(13)));

        await Assert.That(harness.CompileCodes).IsEqualTo(Code(SchematronDiagnosticCodes.ElementMisplaced, "XTSE0010"));
    }

    [Test]
    public async Task Compile_WithTheNonMatchingSubstringTwice_IsMisplaced()
    {
        var harness = new RegexHarness(Analyze("'a'", "b", null, 10, NonMatch(11), NonMatch(12)));

        await Assert.That(harness.CompileCodes).IsEqualTo(Code(SchematronDiagnosticCodes.ElementMisplaced, "XTSE0010"));
    }

    [Test]
    public async Task Compile_WithTheNonMatchingSubstringBeforeTheMatchingOne_IsMisplaced()
    {
        var harness = new RegexHarness(Analyze("'a'", "b", null, 10, NonMatch(11), Match(12)));

        await Assert.That(harness.CompileCodes).IsEqualTo(Code(SchematronDiagnosticCodes.ElementMisplaced, "XTSE0010"));
    }

    [Test]
    public async Task Compile_WithAnInvalidSelect_IsTheExpressionError()
    {
        var harness = new RegexHarness(Analyze("'a", "b", null, 10, Match(11, Out("'x'", 12))));

        await Assert
            .That(harness.CompileCodes)
            .IsEqualTo(Code(SchematronDiagnosticCodes.ExpressionInvalid, "XPST0003"));
    }

    [Test]
    public async Task Compile_WithAnInvalidBody_IsTheExpressionError()
    {
        var harness = new RegexHarness(Analyze("'a'", "b", null, 10, Match(11, Out("'x", 12))));

        await Assert
            .That(harness.CompileCodes)
            .IsEqualTo(Code(SchematronDiagnosticCodes.ExpressionInvalid, "XPST0003"));
    }

    [Test]
    public async Task Compile_WithAnUnclosedTemplate_IsReported()
    {
        var harness = new RegexHarness(Brackets("'a'", "{b", null, 10));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).Contains("XTSE0350");
    }

    [Test]
    public async Task Compile_WithAnInvalidFlagsTemplate_IsReported()
    {
        var harness = new RegexHarness(Brackets("'a'", "b", "}", 10));

        await Assert.That(harness.Executable).IsNull();
        await Assert.That(harness.CompileCodes).Contains("XTSE0370");
    }

    [Test]
    public async Task Compile_ReportsTheSlotsOfEveryExpression_AndTheSlotEndOfTheBodies()
    {
        var harness = new RegexHarness(Brackets("$s", "{$re}", "{$fl}", 10), ["s", "re", "fl"]);
        var reader = (IXsltSlotReader)harness.Executable!;
        var slots = reader.ReferencedSlots.ToArray();

        await Assert.That(slots).Contains(0);
        await Assert.That(slots).Contains(1);
        await Assert.That(slots).Contains(2);
        await Assert.That(((IXsltLocalSlots)harness.Executable!).SlotEnd).IsEqualTo(3);
    }
}
