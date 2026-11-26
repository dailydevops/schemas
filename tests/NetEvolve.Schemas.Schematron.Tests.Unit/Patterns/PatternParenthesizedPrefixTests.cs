namespace NetEvolve.Schemas.Schematron.Tests.Unit.Patterns;

using System.Globalization;
using NetEvolve.Schemas.Schematron.Patterns;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;
using static PatternFixture;

/// <summary>
/// A parenthesized pattern as the prefix of a path pattern (#1285, XSLT 3.0 section 5.5): the node matches the steps after the
/// prefix and its parent, or after <c>//</c> one of its ancestors, matches the prefix.
/// </summary>
public class PatternParenthesizedPrefixTests
{
    private static string Keys(string pattern) => MatchingKeys(pattern, new PatternTestBinding(true));

    [Test]
    [Arguments("(a)/b", "b1 b2 a2b")]
    [Arguments("(a|e)/b", "b1 b2 a2b")]
    [Arguments("(e|a)/b", "b1 b2 a2b")]
    [Arguments("(e)/b", "")]
    [Arguments("(b)/a", "")]
    [Arguments("(a)/b[@k]", "b2")]
    [Arguments("(a)/@n", "a1n a2n")]
    [Arguments("(a)/text()", "t1")]
    [Arguments("(root)/a/b", "b1 b2 a2b")]
    [Arguments("(root)/b", "")]
    [Arguments("((a|e))/b", "b1 b2 a2b")]
    [Arguments("((a)|(e))/b", "b1 b2 a2b")]
    [Arguments("(/root/a)/b", "b1 b2 a2b")]
    [Arguments("(//a)/b", "b1 b2 a2b")]
    [Arguments("(/root | /other)/a", "a1 a2")]
    [Arguments("(/other)/a", "")]
    public async Task Matches_ParenthesizedPrefixAndChildStep_MatchesTheParent(string pattern, string expected) =>
        await Assert.That(Keys(pattern)).IsEqualTo(expected);

    // Predicates inside the union and on the parenthesized pattern itself.
    [Test]
    [Arguments("(a[@n = '2'] | e)/b", "a2b")]
    [Arguments("(a[@n = '3'] | e)/b", "")]
    [Arguments("(a|b)[@n]/b", "b1 b2 a2b")]
    [Arguments("(a|b)[@k]/b", "")]
    [Arguments("(root|a)[@n = '1']/b", "b1 b2")]
    [Arguments("(a | e)[true()]/b", "b1 b2 a2b")]
    [Arguments("(a)[@n = '1']/b[@k]", "b2")]
    [Arguments("(a)[@n = '1']/b[@k = 'y']", "")]
    public async Task Matches_PrefixWithPredicates_AppliesThePredicatesToTheParent(string pattern, string expected) =>
        await Assert.That(Keys(pattern)).IsEqualTo(expected);

    // After "//" any ancestor may match the prefix.
    [Test]
    [Arguments("(root)//b", "b1 b2 a2b")]
    [Arguments("(a)//b", "b1 b2 a2b")]
    [Arguments("(a[@n = '2'])//b", "a2b")]
    [Arguments("(e)//b", "")]
    [Arguments("(b)//b", "")]
    [Arguments("(/root)//a/b", "b1 b2 a2b")]
    [Arguments("(a)//b[@k]", "b2")]
    [Arguments("(root[@id = 'x'])//b", "")]
    [Arguments("(root | e)//b", "b1 b2 a2b")]
    public async Task Matches_ParenthesizedPrefixAndDescendantStep_MatchesAnAncestor(string pattern, string expected) =>
        await Assert.That(Keys(pattern)).IsEqualTo(expected);

    // The prefix is one alternative of a union and mixes with the existing forms.
    [Test]
    [Arguments("(a)/b | p:e", "b1 b2 a2b e")]
    [Arguments("p:e | (a)/b[@k]", "b2 e")]
    [Arguments("(a)/b | (root)/p:e", "b1 b2 a2b e")]
    [Arguments("(a)/b except b[@k]", "b1 a2b")]
    [Arguments("(a)/b intersect b[@k]", "b2")]
    public async Task Matches_ParenthesizedPrefixInAUnionOrSetOperation_CombinesWithTheOtherForms(
        string pattern,
        string expected
    ) => await Assert.That(Keys(pattern)).IsEqualTo(expected);

    [Test]
    [Arguments("(a|e)/b", "0.5")]
    [Arguments("(a)//b", "0.5")]
    [Arguments("(a)/b | c", "0.5;0")]
    public async Task DefaultPriorities_ParenthesizedPrefix_IsOneHalf(string pattern, string expected)
    {
        var compiled = Compile(new PatternMatcher(new PatternTestBinding(true)), pattern);
        var rendered = new List<string>();
        foreach (var priority in PatternMatcher.DefaultPriorities(compiled))
        {
            rendered.Add(priority.ToString(CultureInfo.InvariantCulture));
        }

        await Assert.That(string.Join(";", rendered)).IsEqualTo(expected);
    }

    // Only the steps after the prefix decide the prefilter.
    [Test]
    public async Task Compile_ParenthesizedPrefix_PrefiltersByTheLastStep()
    {
        var compiled = Compile(new PatternMatcher(new PatternTestBinding(true)), "(a|e)/b");

        await Assert.That(compiled.NodeKinds.ToString()).IsEqualTo("Element");
        await Assert.That(compiled.CandidateNames.Count).IsEqualTo(1);
        await Assert.That(compiled.CandidateNames[0].LocalName).IsEqualTo("b");
    }

    [Test]
    public async Task TryCompile_ParenthesizedPrefixAsKeyMatch_IsAcceptedLikeARuleContext()
    {
        var matcher = new PatternMatcher(new PatternTestBinding(true));

        var compiled = matcher.TryCompile("(a|e)/b", PatternUse.KeyMatch, Scope, out _, out _);

        await Assert.That(compiled).IsTrue();
    }

    // Rejected under the xpath family with the rule of the XSLT 2.0 profile.
    [Test]
    public async Task TryCompile_ParenthesizedPrefixUnderXPathBinding_ReportsUnsupportedPattern()
    {
        var matcher = new PatternMatcher(new PatternTestBinding());

        var compiled = matcher.TryCompile("(a|b)/c", PatternUse.RuleContext, Scope, out _, out var problem);

        await Assert.That(compiled).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.PatternUnsupported);
        await Assert.That(problem.Constraint).IsEqualTo("step-kind");
    }

    // Forms around the prefix that stay outside the subset, with the rule and the column in the pattern text.
    [Test]
    [Arguments("(a|b)/$v", "step-kind", 1, 7)]
    [Arguments("(a|b)/ancestor::c", "axis", 1, 7)]
    [Arguments("(a|$v)/c", "step-kind", 1, 4)]
    [Arguments("(a|b)[1]/c", "predicate", 1, 7)]
    [Arguments("(a|b)[position() = 1]/c", "predicate", 1, 7)]
    public async Task TryCompile_FormOutsideTheSubsetAroundAPrefix_ReportsUnsupportedPattern(
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
    [Arguments("(a|b)[invalid]/c")]
    public async Task TryCompile_InvalidPredicateOfAPrefix_ReportsInvalidExpression(string pattern)
    {
        var matcher = new PatternMatcher(new PatternTestBinding(true));

        var compiled = matcher.TryCompile(pattern, PatternUse.RuleContext, Scope, out _, out var problem);

        await Assert.That(compiled).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.ExpressionInvalid);
    }

    [Test]
    [Arguments("(a)[bad]/b", "b1")]
    [Arguments("(a)[bad]//b", "b1")]
    [Arguments("(root)//a[bad]/b", "b1")]
    public async Task Matches_FailingPredicateInThePrefix_ReportsTheProblemStatus(string pattern, string key)
    {
        var binding = new PatternTestBinding(true);
        var matcher = new PatternMatcher(binding);
        var compiled = Compile(matcher, pattern);
        using var run = binding.BeginRun(null!, default);

        var status = matcher.Matches(compiled, run, Node(key), out var matched);

        await Assert.That(status).IsEqualTo(QueryStatus.Problem);
        await Assert.That(matched).IsFalse();
    }

    // The real bindings: the XRechnung shape builds under xslt2 and fires on the item of an invoice line only.
    [Test]
    public async Task Validate_XRechnungShapedContextUnderXslt2_FiresOnTheItemOfALineOnly()
    {
        var schema = PipelineFixture.Wrap(
            """
            <ns prefix="i" uri="urn:i"/>
            <ns prefix="c" uri="urn:c"/>
            <pattern id="items">
              <let name="flag" value="true()"/>
              <rule context="(/i:Invoice[$flag]/c:Line | /i:CreditNote[$flag]/c:CreditLine)/c:Item">
                <assert test="false()">Item.</assert>
              </rule>
            </pattern>
            """,
            " queryBinding=\"xslt2\""
        );
        var validator = PipelineFixture.Build(schema);

        var invoice = PipelineFixture.Validate(
            validator,
            "<Invoice xmlns=\"urn:i\" xmlns:c=\"urn:c\"><c:Line><c:Item/></c:Line><c:Other><c:Item/></c:Other></Invoice>"
        );
        var credit = PipelineFixture.Validate(
            validator,
            "<CreditNote xmlns=\"urn:i\" xmlns:c=\"urn:c\"><c:CreditLine><c:Item/></c:CreditLine></CreditNote>"
        );
        var crossed = PipelineFixture.Validate(
            validator,
            "<Invoice xmlns=\"urn:i\" xmlns:c=\"urn:c\"><c:CreditLine><c:Item/></c:CreditLine></Invoice>"
        );

        await Assert.That(invoice.Issues.Count).IsEqualTo(1);
        await Assert.That(credit.Issues.Count).IsEqualTo(1);
        await Assert.That(crossed.IsValid).IsTrue();
    }
}
