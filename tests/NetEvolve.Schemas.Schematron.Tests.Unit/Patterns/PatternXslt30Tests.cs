namespace NetEvolve.Schemas.Schematron.Tests.Unit.Patterns;

using System.Globalization;
using NetEvolve.Schemas.Schematron.Bindings.Registry;
using NetEvolve.Schemas.Schematron.Patterns;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using static PatternFixture;

/// <summary>
/// The XSLT 3.0 pattern forms of DEF-60: predicates on a parenthesized pattern, <c>intersect</c>, and <c>except</c>. They
/// are accepted under every XSLT binding, also under <c>xslt</c> and <c>xslt2</c> (a documented superset), and rejected
/// under the <c>xpath</c> family.
/// </summary>
public class PatternXslt30Tests
{
    private static string Keys(string pattern) => MatchingKeys(pattern, new PatternTestBinding(true));

    private static string Priorities(string pattern)
    {
        var compiled = Compile(new PatternMatcher(new PatternTestBinding(true)), pattern);
        var rendered = new List<string>();
        foreach (var priority in PatternMatcher.DefaultPriorities(compiled))
        {
            rendered.Add(priority.ToString(CultureInfo.InvariantCulture));
        }

        return string.Join(";", rendered);
    }

    // A predicate on a parenthesized union or path pattern: the node matches the inner pattern and every predicate holds.
    [Test]
    [Arguments("(a|b)", "a1 b1 b2 a2 a2b")]
    [Arguments("(a)", "a1 a2")]
    [Arguments("((a|b))[@k]", "b2")]
    [Arguments("(a|b)[@n]", "a1 a2")]
    [Arguments("(a|b)[@k]", "b2")]
    [Arguments("(a|b)[@n = '2']", "a2")]
    [Arguments("(a|b)[@k = 'y']", "")]
    [Arguments("(a|b)[true()]", "a1 b1 b2 a2 a2b")]
    [Arguments("(a|b)[@n][@n = '1']", "a1")]
    [Arguments("(a|p:e)[true()]", "a1 a2 e")]
    [Arguments("(//a|//b)[@n = '1']", "a1")]
    [Arguments("(/root | /other)[@id = 'r']", "root")]
    [Arguments("(/root | /other)[@id = 'x']", "")]
    [Arguments("(/root/a)[@n = '1']", "a1")]
    [Arguments("(a/b)[@k]", "b2")]
    [Arguments("(@n | @k)[true()]", "a1n b2k a2n")]
    [Arguments("(a|b)[@k] | p:e", "b2 e")]
    public async Task Matches_PredicateOnParenthesizedPattern_MatchesInnerPatternAndPredicates(
        string pattern,
        string expected
    ) => await Assert.That(Keys(pattern)).IsEqualTo(expected);

    // intersect matches both patterns, except the left but not the right one; both bind tighter than a union.
    [Test]
    [Arguments("a intersect a[@n = '2']", "a2")]
    [Arguments("* intersect a", "a1 a2")]
    [Arguments("a intersect b", "")]
    [Arguments("node() intersect text()", "t1")]
    [Arguments("a except a[@n = '2']", "a1")]
    [Arguments("a except b", "a1 a2")]
    [Arguments("node() except *", "head t1 c pi")]
    [Arguments("* except (a|b)", "root e")]
    [Arguments("(a|b) except b", "a1 a2")]
    [Arguments("(a|b) intersect b[@k]", "b2")]
    [Arguments("a | b except b[@k]", "a1 b1 a2 a2b")]
    [Arguments("(a|b)[@n] except a[@n = '1']", "a2")]
    [Arguments("* except a except b", "root e")]
    [Arguments("a intersect b | p:e", "e")]
    [Arguments("a union b intersect b[@k]", "a1 b2 a2")]
    public async Task Matches_IntersectAndExcept_CombineTheNodeSetsOfThePatterns(string pattern, string expected) =>
        await Assert.That(Keys(pattern)).IsEqualTo(expected);

    // A union at the top level still splits into alternatives; any other form is one alternative of priority 0.5.
    [Test]
    [Arguments("(a|b)[@k]", "0.5")]
    [Arguments("a intersect b", "0.5")]
    [Arguments("a except b", "0.5")]
    [Arguments("a | (b)[@k]", "0;0.5")]
    [Arguments("a intersect b | c", "0.5;0")]
    public async Task DefaultPriorities_Xslt30Form_IsOneHalf(string pattern, string expected) =>
        await Assert.That(Priorities(pattern)).IsEqualTo(expected);

    // The prefilter of the plan: a node of the right side of intersect and except is a node of the left side too.
    [Test]
    [Arguments("(a|@n)[true()]", "Element, Attribute", "a;n")]
    [Arguments("a intersect *", "Element", "a")]
    [Arguments("a except @n", "Element", "a")]
    [Arguments("* except a", "Element", "")]
    [Arguments("(a|*)[true()]", "Element", "")]
    public async Task Compile_Xslt30Form_PrefiltersByTheLeftAndUnionSides(string pattern, string kinds, string names)
    {
        var compiled = Compile(new PatternMatcher(new PatternTestBinding(true)), pattern);
        var rendered = new List<string>();
        foreach (var name in compiled.CandidateNames)
        {
            rendered.Add(name.LocalName);
        }

        await Assert.That(compiled.NodeKinds.ToString()).IsEqualTo(kinds);
        await Assert.That(string.Join(";", rendered)).IsEqualTo(names);
    }

    // The Peppol-shaped context: the predicate text reaches the binding unchanged, with its variables.
    [Test]
    public async Task TryCompile_PeppolShapedContext_HandsThePredicateToTheBinding()
    {
        var binding = new PatternTestBinding(true);
        var matcher = new PatternMatcher(binding);

        _ = Compile(
            matcher,
            "(/ubl-invoice:Invoice | /ubl-creditnote:CreditNote)[$supplierCountryIsDE and $customerCountryIsDE]",
            new QueryStaticScope(
                [
                    new NetEvolve.Schemas.Schematron.Model.SchematronNamespace(
                        Fakes.FakeModel.Origin,
                        "ubl-invoice",
                        "urn:i"
                    ),
                    new NetEvolve.Schemas.Schematron.Model.SchematronNamespace(
                        Fakes.FakeModel.Origin,
                        "ubl-creditnote",
                        "urn:c"
                    ),
                ],
                ["supplierCountryIsDE", "customerCountryIsDE"],
                null,
                NetEvolve.Schemas.Schematron.Model.SchematronEdition.Iso2016
            )
        );

        await Assert.That(binding.Compiled).Contains("$supplierCountryIsDE and $customerCountryIsDE");
    }

    [Test]
    public async Task TryCompile_Xslt30FormAsKeyMatch_IsAcceptedLikeARuleContext()
    {
        var matcher = new PatternMatcher(new PatternTestBinding(true));

        var compiled = matcher.TryCompile("(a|b)[@k]", PatternUse.KeyMatch, Scope, out _, out _);

        await Assert.That(compiled).IsTrue();
    }

    // Rejected under the xpath family with the rule of the XSLT 2.0 profile.
    [Test]
    [Arguments("(a|b)[@k]", "step-kind", 1, 1)]
    [Arguments("a intersect b", "set-operator", 1, 2)]
    [Arguments("a except b", "set-operator", 1, 2)]
    [Arguments("a | (b)[@k]", "step-kind", 1, 5)]
    public async Task TryCompile_Xslt30FormUnderXPathBinding_ReportsUnsupportedPattern(
        string pattern,
        string rule,
        int line,
        int column
    )
    {
        var matcher = new PatternMatcher(new PatternTestBinding());

        var compiled = matcher.TryCompile(pattern, PatternUse.RuleContext, Scope, out _, out var problem);

        await Assert.That(compiled).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.PatternUnsupported);
        await Assert.That(problem.Constraint).IsEqualTo(rule);
        await Assert.That(problem.Location.Line).IsEqualTo(line);
        await Assert.That(problem.Location.Column).IsEqualTo(column);
    }

    // Forms that stay outside the subset under an XSLT binding, with the rule and the position in the pattern text.
    [Test]
    [Arguments("$v/a", "step-kind", 1, 1)]
    [Arguments("(a|$v)[@k]", "step-kind", 1, 4)]
    [Arguments("()", "step-kind", 1, 1)]
    [Arguments("(a|ancestor::b)[@k]", "axis", 1, 4)]
    [Arguments("a intersect id('x')", "step-kind", 1, 13)]
    [Arguments("(a|b)[1]", "predicate", 1, 7)]
    [Arguments("(a|b)[@k][last()]", "predicate", 1, 11)]
    [Arguments("(a|b)[position() = 1]", "predicate", 1, 7)]
    [Arguments("(a|b)[1 + 1]", "predicate", 1, 7)]
    [Arguments("a except schema-element(a)", "node-test", 1, 10)]
    public async Task TryCompile_FormOutsideTheSubsetUnderXsltBinding_ReportsUnsupportedPattern(
        string pattern,
        string rule,
        int line,
        int column
    )
    {
        var matcher = new PatternMatcher(new PatternTestBinding(true));

        var compiled = matcher.TryCompile(pattern, PatternUse.RuleContext, Scope, out _, out var problem);

        await Assert.That(compiled).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.PatternUnsupported);
        await Assert.That(problem.Constraint).IsEqualTo(rule);
        await Assert.That(problem.Location.Line).IsEqualTo(line);
        await Assert.That(problem.Location.Column).IsEqualTo(column);
    }

    [Test]
    public async Task TryCompile_InvalidPredicateOfAParenthesizedPattern_ReportsInvalidExpression()
    {
        var matcher = new PatternMatcher(new PatternTestBinding(true));

        var compiled = matcher.TryCompile("(a|b)[invalid]", PatternUse.RuleContext, Scope, out _, out var problem);

        await Assert.That(compiled).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.ExpressionInvalid);
    }

    [Test]
    [Arguments("(a|b)[bad]")]
    [Arguments("a intersect (a|b)[bad]")]
    [Arguments("(a|b)[bad] except b")]
    public async Task Matches_FailingPredicate_ReportsTheProblemStatus(string pattern)
    {
        var binding = new PatternTestBinding(true);
        var matcher = new PatternMatcher(binding);
        var compiled = Compile(matcher, pattern);
        using var run = binding.BeginRun(null!, default);

        var status = matcher.Matches(compiled, run, Node("a1"), out var matched);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(matched).IsFalse();
    }

    [Test]
    public async Task Matches_ProblemOfTheRightSide_ReportsTheProblemStatus()
    {
        var binding = new PatternTestBinding(true);
        var matcher = new PatternMatcher(binding);
        var compiled = Compile(matcher, "a intersect (a|b)[bad]");
        using var run = binding.BeginRun(null!, default);

        var status = matcher.Matches(compiled, run, Node("a1"), out var matched);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(matched).IsFalse();
    }

    // The real bindings: every XSLT name accepts the forms, the xpath family rejects them.
    [Test]
    [Arguments("xslt", true)]
    [Arguments("xslt2", true)]
    [Arguments("xslt3", true)]
    [Arguments("xpath2", false)]
    public async Task Build_Xslt30RuleContext_IsAcceptedUnderXsltBindingsOnly(string binding, bool accepted)
    {
        var schema = PipelineFixture.Wrap(
            "<pattern id=\"p\"><rule context=\"(a | b)[@k]\"><assert test=\"false()\">Hit.</assert></rule></pattern>",
            " queryBinding=\"" + binding + "\""
        );

        if (accepted)
        {
            var validator = PipelineFixture.Build(schema);
            var result = PipelineFixture.Validate(validator, "<a><b/><b k=\"1\"/></a>");

            await Assert.That(result.Issues.Count).IsEqualTo(1);
        }
        else
        {
            var failure = PipelineFixture.BuildFailure(schema);

            await Assert.That(failure.Issues[0].Code).IsEqualTo(SchematronDiagnosticCodes.PatternUnsupported);
        }
    }

    // The Peppol-shaped rule: a union of two roots with a predicate over a global variable, under queryBinding xslt2.
    [Test]
    public async Task Validate_PeppolShapedContextUnderXslt2_FiresOnTheMatchingRootOnly()
    {
        var schema = PipelineFixture.Wrap(
            """
            <ns prefix="i" uri="urn:i"/>
            <ns prefix="c" uri="urn:c"/>
            <pattern id="german-rules">
              <let name="supplierCountryIsDE" value="string(//@country) = 'DE'"/>
              <rule context="(/i:Invoice | /c:CreditNote)[$supplierCountryIsDE and exists(@country)]">
                <assert test="false()">German rule.</assert>
              </rule>
            </pattern>
            """,
            " queryBinding=\"xslt2\""
        );
        var validator = PipelineFixture.Build(schema);

        var invoice = PipelineFixture.Validate(validator, "<Invoice xmlns=\"urn:i\" country=\"DE\"/>");
        var credit = PipelineFixture.Validate(validator, "<CreditNote xmlns=\"urn:c\" country=\"DE\"/>");
        var foreign = PipelineFixture.Validate(validator, "<Invoice xmlns=\"urn:i\" country=\"SE\"/>");
        var other = PipelineFixture.Validate(validator, "<Order xmlns=\"urn:i\" country=\"DE\"/>");

        await Assert.That(invoice.Issues.Count).IsEqualTo(1);
        await Assert.That(credit.Issues.Count).IsEqualTo(1);
        await Assert.That(foreign.IsValid).IsTrue();
        await Assert.That(other.IsValid).IsTrue();
    }
}
