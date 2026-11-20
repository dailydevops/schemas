namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Grouping;

using System.Threading.Tasks;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Grouping.Gr;

/// <summary>
/// <c>xsl:for-each-group group-starting-with</c> and <c>group-ending-with</c> (XSLT 2.0 section 14.3): the population is split at the
/// nodes that match the pattern. The pattern is compiled and matched by the shared pattern matcher (SCH-54, #212): the folder holds no
/// pattern code of its own (see <see cref="GroupingArchitectureTests"/>), and a predicate of the pattern reaches the binding through
/// that matcher.
/// </summary>
public class GroupBoundaryTests
{
    private static string Problem(DiagnosticCode code, string? constraint = null) =>
        "problem:" + code + (constraint is null ? string.Empty : "/" + constraint);

    [Test]
    public async Task GroupStartingWith_SplitsTheSequenceAtTheNodesThatMatchThePattern()
    {
        var harness = new GroupHarness(Starting("*", "h", 10, Members(11)), "h p p h p");

        await Assert.That(harness.Result()).IsEqualTo("h p p;h p;");
    }

    [Test]
    public async Task GroupStartingWith_TheFirstNodeStartsAGroupEvenWhenItDoesNotMatch()
    {
        var harness = new GroupHarness(Starting("*", "h", 10, Members(11)), "p h p");

        await Assert.That(harness.Result()).IsEqualTo("p;h p;");
    }

    [Test]
    public async Task GroupStartingWith_NoNodeMatches_IsOneGroup()
    {
        var harness = new GroupHarness(Starting("*", "h", 10, Members(11)), "p p");

        await Assert.That(harness.Result()).IsEqualTo("p p;");
    }

    [Test]
    public async Task GroupStartingWith_AnAlternativePattern_MatchesEveryAlternative()
    {
        var harness = new GroupHarness(Starting("*", "h|g", 10, Members(11)), "h p g p h");

        await Assert.That(harness.Result()).IsEqualTo("h p;g p;h;");
    }

    [Test]
    public async Task GroupEndingWith_SplitsTheSequenceAfterTheNodesThatMatchThePattern()
    {
        var harness = new GroupHarness(Ending("*", "e", 10, Members(11)), "p p e p e p");

        await Assert.That(harness.Result()).IsEqualTo("p p e;p e;p;");
    }

    [Test]
    public async Task GroupEndingWith_ASequenceThatEndsWithAMatch_HasNoEmptyGroupAfterIt()
    {
        var harness = new GroupHarness(Ending("*", "e", 10, Members(11)), "p e");

        await Assert.That(harness.Result()).IsEqualTo("p e;");
    }

    [Test]
    public async Task GroupEndingWith_TheFirstNodeMatching_IsAGroupOfItsOwn()
    {
        var harness = new GroupHarness(Ending("*", "e", 10, Members(11)), "e p");

        await Assert.That(harness.Result()).IsEqualTo("e;p;");
    }

    [Test]
    public async Task Patterns_AreCompiledAndMatchedByTheSharedMatcher_ThePredicateReachesTheBinding()
    {
        var harness = new GroupHarness(Starting("*", "i[@k = 'x']", 10, Members(11)), "i=a i=x i=b i=x i=c");

        await Assert.That(harness.Result()).IsEqualTo("i=a;i=x i=b;i=x i=c;");
        await Assert.That(harness.Binding.Compiled).Contains("@k = 'x'");
    }

    [Test]
    public async Task StartingAndEnding_GiveNoGroupingKey_UnderXslt2()
    {
        var starting = new GroupHarness(
            Starting("*", "h", 10, X("value-of", 11, A("select", "concat(count(current-grouping-key()), ';')"))),
            "h p h"
        );
        var ending = new GroupHarness(
            Ending("*", "e", 10, X("value-of", 11, A("select", "concat(count(current-grouping-key()), ';')"))),
            "p e p"
        );

        await Assert.That(starting.Result()).IsEqualTo("0;0;");
        await Assert.That(ending.Result()).IsEqualTo("0;0;");
        await Assert.That(starting.Run.Writes).Contains("key:-");
    }

    [Test]
    public async Task StartingAndEnding_AGroupingKeyUnderXslt3_IsTheDynamicErrorXtde1071()
    {
        var harness = new GroupHarness(
            Starting("*", "h", 10, X("value-of", 11, A("select", "string(current-grouping-key())"))),
            "h p",
            "xslt3"
        );

        await Assert.That(harness.Result()).IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed, "XTDE1071"));
    }

    [Test]
    public async Task StartingAndEnding_TheCurrentGroupIsStillBound_UnderXslt3()
    {
        var harness = new GroupHarness(Starting("*", "h", 10, Members(11)), "h p h", "xslt3");

        await Assert.That(harness.Result()).IsEqualTo("h p;h;");
    }

    [Test]
    public async Task GroupStartingWith_AnItemThatIsNoNode_IsTheTypeErrorXtte1120()
    {
        var harness = new GroupHarness(Starting("int:1, int:2", "h", 10, Members(11)));

        await Assert.That(harness.Result()).IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed, "XTTE1120"));
    }

    [Test]
    public async Task GroupEndingWith_AnItemThatIsNoNode_IsTheTypeErrorXtte1120()
    {
        var harness = new GroupHarness(Ending("'a'", "h", 10, Members(11)));

        await Assert.That(harness.Result()).IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed, "XTTE1120"));
    }

    [Test]
    public async Task Patterns_AProblemOfThePredicate_IsReported()
    {
        var starting = new GroupHarness(Starting("*", "i[error()]", 10, Members(11)), "i=a i=b");
        var ending = new GroupHarness(Ending("*", "i[error()]", 10, Members(11)), "i=a i=b");

        await Assert.That(starting.Result()).IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed, "FOER0000"));
        await Assert.That(ending.Result()).IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationFailed, "FOER0000"));
    }

    [Test]
    public async Task GroupStartingWith_MoreItemsThanTheSequenceLimit_IsTheCodedLimitError()
    {
        var harness = new GroupHarness(Starting("*", "h", 10, Members(11)), "h p p p");
        harness.Frame.MaxSequenceLength = 3;

        await Assert.That(harness.Result()).IsEqualTo(Problem(SchematronDiagnosticCodes.EvaluationLimitStop));
    }

    [Test]
    public async Task GroupStartingWith_EmptyPopulation_RunsTheBodyNever()
    {
        var harness = new GroupHarness(Starting("()", "h", 10, Members(11)));

        await Assert.That(harness.Result()).IsEqualTo(string.Empty);
    }
}
