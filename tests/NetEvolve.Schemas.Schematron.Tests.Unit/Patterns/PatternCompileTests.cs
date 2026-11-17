namespace NetEvolve.Schemas.Schematron.Tests.Unit.Patterns;

using System.Globalization;
using NetEvolve.Schemas.Schematron.Patterns;
using NetEvolve.Schemas.Schematron.Seams;
using static PatternFixture;

public class PatternCompileTests
{
    private static string Priorities(string pattern)
    {
        var compiled = Compile(new PatternMatcher(new PatternTestBinding()), pattern);
        var rendered = new List<string>();
        foreach (var priority in PatternMatcher.DefaultPriorities(compiled))
        {
            rendered.Add(priority.ToString(CultureInfo.InvariantCulture));
        }

        return string.Join(";", rendered);
    }

    // XSLT 2.0 section 6.4: the default priority of every alternative.
    [Test]
    [Arguments("/", "-0.5")]
    [Arguments("a", "0")]
    [Arguments("child::a", "0")]
    [Arguments("@a", "0")]
    [Arguments("attribute::a", "0")]
    [Arguments("p:e", "0")]
    [Arguments("processing-instruction(x)", "0")]
    [Arguments("processing-instruction('x')", "0")]
    [Arguments("processing-instruction()", "-0.5")]
    [Arguments("*", "-0.5")]
    [Arguments("@*", "-0.5")]
    [Arguments("p:*", "-0.25")]
    [Arguments("@p:*", "-0.25")]
    [Arguments("*:a", "-0.25")]
    [Arguments("text()", "-0.5")]
    [Arguments("comment()", "-0.5")]
    [Arguments("node()", "-0.5")]
    [Arguments("element()", "-0.5")]
    [Arguments("element(a)", "0")]
    [Arguments("attribute()", "-0.5")]
    [Arguments("attribute(a)", "0")]
    [Arguments("document-node()", "-0.5")]
    [Arguments("document-node(element(a))", "0")]
    [Arguments("a/b", "0.5")]
    [Arguments("/a", "0.5")]
    [Arguments("//a", "0.5")]
    [Arguments("a//b", "0.5")]
    [Arguments("a[1]", "0.5")]
    [Arguments("a[@k]", "0.5")]
    [Arguments("@a[1]", "0.5")]
    [Arguments("a|*|a/b|text()", "0;-0.5;0.5;-0.5")]
    [Arguments("@a|p:*|*:b", "0;-0.25;-0.25")]
    public async Task DefaultPriorities_Pattern_EqualTheValuesOfXslt20Section64(string pattern, string expected) =>
        await Assert.That(Priorities(pattern)).IsEqualTo(expected);

    // Forms outside the subset: the unsupported-pattern code, the rule name, and the position inside the pattern text.
    [Test]
    [Arguments("id('x')", "step-kind", 1, 1)]
    [Arguments("a/id('x')", "step-kind", 1, 3)]
    [Arguments("key('k', 'v')", "step-kind", 1, 1)]
    [Arguments(".", "step-kind", 1, 1)]
    [Arguments("a/.", "step-kind", 1, 3)]
    [Arguments("$v/a", "step-kind", 1, 1)]
    [Arguments("(a|b)", "step-kind", 1, 1)]
    [Arguments("a intersect b", "set-operator", 1, 2)]
    [Arguments("a except b", "set-operator", 1, 2)]
    [Arguments("ancestor::a", "axis", 1, 1)]
    [Arguments("a/following::b", "axis", 1, 3)]
    [Arguments("a/descendant::b", "axis", 1, 3)]
    [Arguments("a/parent::b", "axis", 1, 3)]
    [Arguments("schema-element(a)", "node-test", 1, 1)]
    [Arguments("schema-attribute(a)", "node-test", 1, 1)]
    [Arguments("element(a, xs:string)", "node-test", 1, 1)]
    [Arguments("a\n/id('x')", "step-kind", 2, 2)]
    [Arguments("a |\n  b/id('x')", "step-kind", 2, 5)]
    public async Task TryCompile_FormOutsideTheSubset_ReportsUnsupportedPatternWithLocation(
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

    [Test]
    [Arguments("a[")]
    [Arguments("a/")]
    [Arguments("")]
    [Arguments("a |")]
    public async Task TryCompile_SyntaxError_ReportsInvalidExpression(string pattern)
    {
        var matcher = new PatternMatcher(new PatternTestBinding());

        var compiled = matcher.TryCompile(pattern, PatternUse.RuleContext, Scope, out _, out var problem);

        await Assert.That(compiled).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.ExpressionInvalid);
    }

    [Test]
    public async Task TryCompile_UnboundPrefix_ReportsInvalidExpression()
    {
        var matcher = new PatternMatcher(new PatternTestBinding());

        var compiled = matcher.TryCompile("q:a", PatternUse.RuleContext, Scope, out _, out var problem);

        await Assert.That(compiled).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.ExpressionInvalid);
        await Assert.That(problem.Constraint).IsEqualTo("XPST0081");
    }
}
