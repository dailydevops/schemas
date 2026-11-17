namespace NetEvolve.Schemas.Schematron.Tests.Unit.Patterns;

using NetEvolve.Schemas.Schematron.Patterns;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Testing.Allocations;
using static PatternFixture;

public class PatternMatchingTests
{
    // Every node kind matches its own pattern and none of the others (XSLT 2.0 section 5.5).
    [Test]
    [Arguments("a", "a1 a2")]
    [Arguments("child::a", "a1 a2")]
    [Arguments("e", "")]
    [Arguments("p:e", "e")]
    [Arguments("p:*", "e")]
    [Arguments("*:e", "e")]
    [Arguments("*", "root a1 b1 b2 a2 a2b e")]
    [Arguments("element()", "root a1 b1 b2 a2 a2b e")]
    [Arguments("element(a)", "a1 a2")]
    [Arguments("@n", "a1n a2n")]
    [Arguments("attribute::n", "a1n a2n")]
    [Arguments("@*", "rootId a1n b2k a2n")]
    [Arguments("attribute()", "rootId a1n b2k a2n")]
    [Arguments("attribute(n)", "a1n a2n")]
    [Arguments("text()", "t1")]
    [Arguments("comment()", "head c")]
    [Arguments("processing-instruction()", "pi")]
    [Arguments("processing-instruction(pi)", "pi")]
    [Arguments("processing-instruction('pi')", "pi")]
    [Arguments("processing-instruction(x)", "")]
    [Arguments("/", "doc")]
    [Arguments("document-node()", "doc")]
    [Arguments("document-node(element(root))", "doc")]
    [Arguments("document-node(element(a))", "")]
    [Arguments("node()", "head root a1 t1 b1 b2 c pi a2 a2b e")]
    public async Task Matches_SingleStepPattern_MatchesOnlyItsNodeKind(string pattern, string expected) =>
        await Assert.That(MatchingKeys(pattern)).IsEqualTo(expected);

    // Right to left over / and // steps, anchored by a leading / or //.
    [Test]
    [Arguments("/root", "root")]
    [Arguments("/a", "")]
    [Arguments("/root/a", "a1 a2")]
    [Arguments("a/b", "b1 b2 a2b")]
    [Arguments("root/a/b", "b1 b2 a2b")]
    [Arguments("/root/a/b", "b1 b2 a2b")]
    [Arguments("/a//b", "")]
    [Arguments("//b", "b1 b2 a2b")]
    [Arguments("root//b", "b1 b2 a2b")]
    [Arguments("/root//b", "b1 b2 a2b")]
    [Arguments("a//b", "b1 b2 a2b")]
    [Arguments("//a/b", "b1 b2 a2b")]
    [Arguments("a/*", "b1 b2 a2b")]
    [Arguments("root/*", "a1 a2 e")]
    [Arguments("a/@n", "a1n a2n")]
    [Arguments("root/@id", "rootId")]
    [Arguments("root//@k", "b2k")]
    [Arguments("a/text()", "t1")]
    [Arguments("root//text()", "t1")]
    [Arguments("/comment()", "head")]
    [Arguments("//comment()", "head c")]
    [Arguments("b/a", "")]
    public async Task Matches_MultiStepPattern_MatchesRightToLeft(string pattern, string expected) =>
        await Assert.That(MatchingKeys(pattern)).IsEqualTo(expected);

    // A predicate reads an attribute or the position of the node among the nodes the step selects from its parent.
    [Test]
    [Arguments("b[@k]", "b2")]
    [Arguments("b[@k = 'x']", "b2")]
    [Arguments("b[@k = 'y']", "")]
    [Arguments("a[@n = '2']/b", "a2b")]
    [Arguments("b[2]", "b2")]
    [Arguments("b[1]", "b1 a2b")]
    [Arguments("a[2]", "a2")]
    [Arguments("a[2]/b", "a2b")]
    [Arguments("b[last()]", "b2 a2b")]
    [Arguments("*[2]", "b2 a2")]
    [Arguments("*[last()]", "root b2 a2b e")]
    [Arguments("b[position() = 2]", "b2")]
    [Arguments("b[@k][1]", "b2")]
    [Arguments("b[1][@k]", "")]
    [Arguments("@*[1]", "rootId a1n b2k a2n")]
    [Arguments("@*[2]", "")]
    public async Task Matches_PatternWithPredicates_EvaluatesThemThroughTheBinding(string pattern, string expected) =>
        await Assert.That(MatchingKeys(pattern)).IsEqualTo(expected);

    [Test]
    [Arguments("a|text()", "a1 t1 a2")]
    [Arguments("@n|b", "a1n b1 b2 a2n a2b")]
    [Arguments("e|/|comment()", "doc head c e")]
    [Arguments("a union text()", "a1 t1 a2")]
    public async Task Matches_UnionPattern_MatchesWhenAnyAlternativeMatches(string pattern, string expected) =>
        await Assert.That(MatchingKeys(pattern)).IsEqualTo(expected);

    // A numeric predicate is a position test; every other predicate is handed over unchanged.
    [Test]
    [Arguments("b[2]", "position() = (2)")]
    [Arguments("b[last()]", "position() = (last())")]
    [Arguments("b[1 + 1]", "position() = (1 + 1)")]
    [Arguments("b[@k]", "@k")]
    [Arguments("b[position() = 2]", "position() = 2")]
    public async Task TryCompile_Predicate_NumericBecomesPositionTest(string pattern, string expected)
    {
        var binding = new PatternTestBinding();
        _ = MatchingKeys(pattern, binding);

        await Assert.That(binding.Compiled).IsEquivalentTo([expected]);
    }

    [Test]
    public async Task TryCompile_PredicateTheBindingRejects_ReturnsTheBindingProblem()
    {
        var matcher = new PatternMatcher(new PatternTestBinding());

        var compiled = matcher.TryCompile("b[invalid]", PatternUse.RuleContext, Scope, out _, out var problem);

        await Assert.That(compiled).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.ExpressionInvalid);
    }

    [Test]
    public async Task Matches_PredicateTheBindingFails_ReturnsProblemStatus()
    {
        var binding = new PatternTestBinding();
        var matcher = new PatternMatcher(binding);
        var compiled = Compile(matcher, "b[bad]");
        using var run = binding.BeginRun(null!, default);

        var status = matcher.Matches(compiled, run, Node("b1"), out var matched);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(matched).IsFalse();
        await Assert.That(run.GetProblem().Code).IsEqualTo(SchematronDiagnosticCodes.EvaluationFailed);
    }

    [Test]
    public async Task Matches_PrefilterNotMatching_DoesNotEvaluateAnyPredicate()
    {
        var binding = new PatternTestBinding();

        _ = MatchingKeys("b[@k]", binding);

        // Only the three b elements reach the predicate; every other node fails the name test first.
        await Assert.That(binding.EvaluateCalls).IsEqualTo(3);
    }

    [Test]
    public async Task TryCompile_KeyMatchUse_CompilesTheSamePattern()
    {
        var matcher = new PatternMatcher(new PatternTestBinding());

        var compiled = matcher.TryCompile("a/b", PatternUse.KeyMatch, Scope, out var pattern, out _);

        await Assert.That(compiled).IsTrue();
        await Assert.That(pattern.NodeKinds).IsEqualTo(QueryNodeKinds.Element);
    }

    [Test]
    [Arguments("a", "Element", "a")]
    [Arguments("a|b", "Element", "a b")]
    [Arguments("a/b|p:e", "Element", "b {urn:p}e")]
    [Arguments("@n", "Attribute", "n")]
    [Arguments("a|text()", "Element, Text", "")]
    [Arguments("a|*", "Element", "")]
    [Arguments("p:*", "Element", "")]
    [Arguments("text()", "Text", "")]
    [Arguments("comment()", "Comment", "")]
    [Arguments("processing-instruction()", "ProcessingInstruction", "")]
    [Arguments("/", "Document", "")]
    [Arguments("a/@n", "Attribute", "n")]
    [Arguments("node()", "Element, Text, Comment, ProcessingInstruction", "")]
    public async Task TryCompile_Pattern_ReportsNodeKindsAndCandidateNames(string pattern, string kinds, string names)
    {
        var compiled = Compile(new PatternMatcher(new PatternTestBinding()), pattern);

        var rendered = new List<string>();
        foreach (var name in compiled.CandidateNames.ToArray())
        {
            rendered.Add(name.NamespaceName.Length == 0 ? name.LocalName : $"{{{name.NamespaceName}}}{name.LocalName}");
        }

        await Assert.That(compiled.NodeKinds).IsEqualTo(Enum.Parse<QueryNodeKinds>(kinds));
        await Assert.That(string.Join(" ", rendered)).IsEqualTo(names);
    }

    [Test]
    [Arguments("/")]
    [Arguments("a/b")]
    [Arguments("//b")]
    [Arguments("@n")]
    [Arguments("text()")]
    [Arguments("p:e")]
    [Arguments("a|b")]
    [Arguments("root/a/@n")]
    public async Task Matches_PatternWithoutPredicates_AllocatesNothing(string pattern)
    {
        var binding = new PatternTestBinding();
        var matcher = new PatternMatcher(binding);
        var compiled = Compile(matcher, pattern);
        using var run = binding.BeginRun(null!, default);
        var matches = 0;

        AllocationAssert.NoAllocation(() =>
        {
            matches = 0;
            foreach (var pair in Nodes)
            {
                _ = matcher.Matches(compiled, run, pair.Value, out var matched);
                if (matched)
                {
                    matches++;
                }
            }
        });

        await Assert.That(matches).IsGreaterThan(0);
    }
}
