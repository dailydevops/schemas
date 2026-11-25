namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Regex;

using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Grouping;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Regex.Rx;

/// <summary>
/// <c>xsl:analyze-string</c> (XSLT 2.0 section 15.1): the matching and the non-matching substrings are processed in input order, the
/// flags <c>i</c>, <c>m</c>, <c>s</c>, and <c>x</c> change the result, the focus is the substring, <c>regex-group()</c> reads the
/// groups of the current match, and a regex that matches the zero-length string is XTDE1150.
/// </summary>
public class AnalyzeStringTests
{
    private static string Problem(DiagnosticCode code, string? constraint = null) =>
        "problem:" + code + (constraint is null ? string.Empty : "/" + constraint);

    [Test]
    public async Task Substrings_AreProcessedInInputOrder_MatchingAndNonMatching()
    {
        var harness = new RegexHarness(Brackets("'a1b22c'", "[0-9]+", null, 10));

        await Assert.That(harness.Result()).IsEqualTo("(a)[1](b)[22](c)");
    }

    [Test]
    public async Task Substrings_InputThatStartsAndEndsWithAMatch_HasNoEmptyNonMatchingSubstring()
    {
        await Assert.That(new RegexHarness(Brackets("'12ab'", "[0-9]+", null, 10)).Result()).IsEqualTo("[12](ab)");
        await Assert.That(new RegexHarness(Brackets("'ab9'", "[0-9]+", null, 10)).Result()).IsEqualTo("(ab)[9]");
        await Assert.That(new RegexHarness(Brackets("'1x2'", "[0-9]", null, 10)).Result()).IsEqualTo("[1](x)[2]");
    }

    [Test]
    public async Task Substrings_AdjacentMatches_HaveNoNonMatchingSubstringBetweenThem() =>
        await Assert.That(new RegexHarness(Brackets("'1234'", "[0-9]{{2}}", null, 10)).Result()).IsEqualTo("[12][34]");

    [Test]
    public async Task Substrings_InputWithoutAMatch_IsOneNonMatchingSubstring() =>
        await Assert.That(new RegexHarness(Brackets("'abc'", "[0-9]", null, 10)).Result()).IsEqualTo("(abc)");

    [Test]
    public async Task Substrings_EmptyInput_RunsNoBody()
    {
        var harness = new RegexHarness(Brackets("''", "[0-9]", null, 10));

        await Assert.That(harness.Result()).IsEqualTo(string.Empty);
        await Assert.That(harness.Sink.Events.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Substrings_OnlyAMatchingBody_SkipsTheNonMatchingSubstrings()
    {
        var harness = new RegexHarness(Analyze("'a1b2'", "[0-9]", null, 10, Match(11, Out("concat('[', ., ']')", 12))));

        await Assert.That(harness.Result()).IsEqualTo("[1][2]");
    }

    [Test]
    public async Task Substrings_OnlyANonMatchingBody_SkipsTheMatchingSubstrings()
    {
        var harness = new RegexHarness(
            Analyze("'a1b2'", "[0-9]", null, 10, NonMatch(11, Out("concat('(', ., ')')", 12)))
        );

        await Assert.That(harness.Result()).IsEqualTo("(a)(b)");
    }

    [Test]
    public async Task Flags_WithoutFlags_TheMatchIsCaseSensitive() =>
        await Assert.That(new RegexHarness(Brackets("'ABab'", "ab", null, 10)).Result()).IsEqualTo("(AB)[ab]");

    [Test]
    public async Task Flags_I_IgnoresCase() =>
        await Assert.That(new RegexHarness(Brackets("'ABab'", "ab", "i", 10)).Result()).IsEqualTo("[AB][ab]");

    [Test]
    public async Task Flags_M_LetsTheCaretMatchAtEveryLineStart()
    {
        await Assert.That(new RegexHarness(Brackets("'a\nb'", "^b", null, 10)).Result()).IsEqualTo("(a\nb)");
        await Assert.That(new RegexHarness(Brackets("'a\nb'", "^b", "m", 10)).Result()).IsEqualTo("(a\n)[b]");
    }

    [Test]
    public async Task Flags_S_LetsTheDotMatchANewLine()
    {
        await Assert.That(new RegexHarness(Brackets("'a\nb'", "a.b", null, 10)).Result()).IsEqualTo("(a\nb)");
        await Assert.That(new RegexHarness(Brackets("'a\nb'", "a.b", "s", 10)).Result()).IsEqualTo("[a\nb]");
    }

    [Test]
    public async Task Flags_X_IgnoresWhiteSpaceInTheRegex()
    {
        await Assert.That(new RegexHarness(Brackets("'ab'", "a b", null, 10)).Result()).IsEqualTo("(ab)");
        await Assert.That(new RegexHarness(Brackets("'ab'", "a b", "x", 10)).Result()).IsEqualTo("[ab]");
    }

    [Test]
    public async Task Flags_Combined_AreAllApplied() =>
        await Assert.That(new RegexHarness(Brackets("'A\nB'", "a.b", "is", 10)).Result()).IsEqualTo("[A\nB]");

    [Test]
    public async Task Attributes_AreTemplates_TheirExpressionsAreEvaluatedWithTheFocusOfTheInstruction()
    {
        var harness = new RegexHarness(Brackets("'a1B2'", "{$re}", "{$fl}", 10), ["re", "fl"]);
        harness.Bind("re", XdmSequence.Create(XdmItem.FromString("b")));
        harness.Bind("fl", XdmSequence.Create(XdmItem.FromString("i")));

        await Assert.That(harness.Result()).IsEqualTo("(a1)[B](2)");
    }

    [Test]
    public async Task Attributes_ATemplateWithAnExpressionThatFails_IsTheProblemOfTheExpression()
    {
        var harness = new RegexHarness(Brackets("'a'", "{error()}", null, 10));

        await Assert.That(harness.Result()).IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed, "FOER0000"));
    }

    [Test]
    public async Task Focus_IsTheSubstring_WithItsPositionAndTheNumberOfSubstrings()
    {
        var body = Out("concat(position(), '/', last(), ':', .)", 12);
        var harness = new RegexHarness(Analyze("'a1b'", "[0-9]", null, 10, Match(11, body), NonMatch(13, body)));

        await Assert.That(harness.Result()).IsEqualTo("1/3:a2/3:13/3:b");
    }

    [Test]
    public async Task Focus_OfTheCaller_IsPutBackWhenTheInstructionEnds()
    {
        var harness = new RegexHarness(Brackets("'a1'", "[0-9]", null, 10));
        var before = harness.Frame.Focus;

        _ = harness.Execute();

        await Assert.That(harness.Frame.Focus.Position).IsEqualTo(before.Position);
        await Assert.That(harness.Frame.Focus.Node.Node).IsEqualTo(before.Node.Node);
    }

    [Test]
    public async Task RegexGroup_InAMatchingSubstring_IsTheTextOfTheGroup()
    {
        var harness = new RegexHarness(
            Analyze(
                "'xaz'",
                "(a)(b)?",
                null,
                10,
                Match(
                    11,
                    Out("concat(regex-group(0), '|', regex-group(1), '|', regex-group(2), '|', regex-group(3))", 12)
                ),
                NonMatch(13, Out("concat('<', regex-group(1), '>')", 14))
            )
        );

        await Assert.That(harness.Result()).IsEqualTo("<>a|a||<>");
    }

    [Test]
    public async Task RegexGroup_WithTwoMatches_ReadsTheGroupsOfTheMatchBeingProcessed()
    {
        var harness = new RegexHarness(
            Analyze(
                "'a1b2'",
                "([a-z])([0-9])",
                null,
                10,
                Match(11, Out("concat(regex-group(2), regex-group(1), ';')", 12))
            )
        );

        await Assert.That(harness.Result()).IsEqualTo("1a;2b;");
    }

    [Test]
    public async Task RegexGroup_SlotIsWrittenPerSubstring_AndPutBackWhenTheInstructionEnds()
    {
        var harness = new RegexHarness(Brackets("'a1b2'", "[0-9]", null, 10));

        _ = harness.Execute();

        await Assert.That(string.Join(",", harness.Run.Writes)).IsEqualTo("-,1,-,2,-");
        await Assert.That(harness.Run.RegexGroups).IsNull();
    }

    [Test]
    public async Task RegexGroup_OfTheCaller_IsPutBackAlsoWhenABodyFails()
    {
        var harness = new RegexHarness(Analyze("'a1'", "[0-9]", null, 10, Match(11, Out("error()", 12))));
        harness.Run.RegexGroups = new XPathRegexGroups("zz", [new TextRange(0, 2)]);

        await Assert.That(harness.Result()).IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed, "FOER0000"));
        await Assert.That(harness.Run.RegexGroups?.GetGroup(0).ToString()).IsEqualTo("zz");
    }

    [Test]
    public async Task EmptyMatch_RegexThatMatchesTheZeroLengthString_IsXTDE1150()
    {
        await Assert
            .That(new RegexHarness(Brackets("'abc'", "a*", null, 10)).Result())
            .IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed, "XTDE1150"));
        await Assert
            .That(new RegexHarness(Brackets("''", "x?", null, 10)).Result())
            .IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed, "XTDE1150"));
    }

    [Test]
    public async Task EmptyMatch_RunsNoBody()
    {
        var harness = new RegexHarness(Brackets("'abc'", "a*", null, 10));

        _ = harness.Execute();

        await Assert.That(harness.Sink.Events.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Regex_Invalid_IsXTDE1140() =>
        await Assert
            .That(new RegexHarness(Brackets("'abc'", "(", null, 10)).Result())
            .IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed, "XTDE1140"));

    [Test]
    public async Task Flags_Invalid_IsXTDE1145() =>
        await Assert
            .That(new RegexHarness(Brackets("'abc'", "b", "z", 10)).Result())
            .IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed, "XTDE1145"));

    [Test]
    public async Task Regex_ExceedingALimitOfTheRegexEngine_IsTheCodedLimitError() =>
        await Assert
            .That(new RegexHarness(Brackets("'abc'", "(a{{1000}}){{1000}}", null, 10)).Result())
            .IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationLimitStop));

    [Test]
    public async Task Select_WithoutAValue_IsXTTE1100() =>
        await Assert
            .That(new RegexHarness(Brackets("()", "b", null, 10)).Result())
            .IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed, "XTTE1100"));

    [Test]
    public async Task Select_WithTwoValues_IsXTTE1100() =>
        await Assert
            .That(new RegexHarness(Brackets("'a', 'b'", "b", null, 10)).Result())
            .IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed, "XTTE1100"));

    [Test]
    public async Task Select_OfAnotherAtomicType_IsXTTE1100() =>
        await Assert
            .That(new RegexHarness(Brackets("5", "b", null, 10)).Result())
            .IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed, "XTTE1100"));

    [Test]
    public async Task Select_OfANode_IsItsStringValue()
    {
        var harness = new RegexHarness(Brackets(".", "b", null, 10));

        await Assert.That(harness.Result()).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Select_ThatFails_IsTheProblemOfTheRun() =>
        await Assert
            .That(new RegexHarness(Brackets("error()", "b", null, 10)).Result())
            .IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed, "FOER0000"));

    [Test]
    public async Task Body_ThatFails_StopsTheInstruction() =>
        await Assert
            .That(new RegexHarness(Analyze("'a1b'", "[0-9]", null, 10, NonMatch(11, Out("error()", 12)))).Result())
            .IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed, "FOER0000"));

    [Test]
    public async Task Limit_MoreSubstringsThanTheSequenceLimit_IsTheCodedLimitError()
    {
        var harness = new RegexHarness(Brackets("'a1b2c'", "[0-9]", null, 10));
        harness.Frame.MaxSequenceLength = 3;

        await Assert.That(harness.Result()).IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationLimitStop));
    }

    [Test]
    public async Task Run_ThatCannotHoldTheRegexGroups_IsAnEvaluationFailure()
    {
        var harness = new RegexHarness(Brackets("'a1'", "[0-9]", null, 10));
        using var run = new GroupRun(new GroupBinding());
        var frame = new XsltFrame(run);

        var status = harness.Executable!.TryExecute(frame, harness.Sink);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(frame.TakeProblem().Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
    }

    [Test]
    public async Task Cancellation_BeforeTheRun_StopsTheInstruction()
    {
        using var source = new CancellationTokenSource();
        var harness = new RegexHarness(Brackets("'a1b'", "[0-9]", null, 10), cancellationToken: source.Token);
#if NET8_0_OR_GREATER
        await source.CancelAsync();
#else
        source.Cancel();
#endif

        await Assert.That(() => harness.Execute()).Throws<System.OperationCanceledException>();
        await Assert.That(harness.Sink.Events.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Cancellation_EverySubstring_IsACancellationPoint()
    {
        using var source = new CancellationTokenSource();
        var harness = new RegexHarness(Brackets("'a1b'", "[0-9]", null, 10), cancellationToken: source.Token);
        var sink = new CancelOnTextSink(source);

        // The token is canceled while the first substring writes; the check before the second substring stops the instruction.
        await Assert.That(() => harness.Execute(sink)).Throws<System.OperationCanceledException>();
        await Assert.That(sink.Texts).IsEqualTo(1);
    }
}
