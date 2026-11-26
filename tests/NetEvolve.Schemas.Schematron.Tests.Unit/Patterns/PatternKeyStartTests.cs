namespace NetEvolve.Schemas.Schematron.Tests.Unit.Patterns;

using System.Globalization;
using NetEvolve.Schemas.Schematron.Patterns;
using NetEvolve.Schemas.Schematron.Seams;
using static PatternFixture;

/// <summary>
/// The pattern start <c>key('name', 'value')</c> (#780) as the compile step sees it: the call is compiled as a node expression
/// under an XSLT binding, the plan gets the priority 0.5 and the prefilter of its last step, and the forms outside
/// the IdKeyPattern production are reported as unsupported. The match itself is covered by the run tests of the pipeline.
/// </summary>
public class PatternKeyStartTests
{
    private const string AllKinds = "Document, Element, Attribute, Text, Comment, ProcessingInstruction";

    private static string Describe(string pattern)
    {
        var binding = new PatternTestBinding(true);
        var compiled = Compile(new PatternMatcher(binding), pattern);
        var priorities = new List<string>();
        foreach (var priority in PatternMatcher.DefaultPriorities(compiled))
        {
            priorities.Add(priority.ToString(CultureInfo.InvariantCulture));
        }

        var names = new List<string>();
        foreach (var name in compiled.CandidateNames)
        {
            names.Add(name.LocalName);
        }

        return $"{string.Join(";", priorities)}|{compiled.NodeKinds}|{string.Join(",", names)}|{string.Join(";", binding.Compiled)}";
    }

    [Test]
    [Arguments("key('k', 'v')", "0.5|" + AllKinds + "||key('k', 'v')")]
    [Arguments("key('k', $v)", "0.5|" + AllKinds + "||key('k', $v)")]
    [Arguments("key('k', 'v')/a", "0.5|Element|a|key('k', 'v')")]
    [Arguments("key('k', 'v')//a", "0.5|Element|a|key('k', 'v')")]
    [Arguments("key('k', 'v')/a/@b", "0.5|Attribute|b|key('k', 'v')")]
    [Arguments("key('k', 'v')/a[@b]", "0.5|Element|a|key('k', 'v');@b")]
    [Arguments("(key('k', 'v'))", "0.5|" + AllKinds + "||key('k', 'v')")]
    [Arguments("key('k', 'v') | a", "0.5;0|" + AllKinds + "||key('k', 'v')")]
    public async Task TryCompile_KeyStart_BuildsAPlanWithPriorityHalf(string pattern, string expected) =>
        await Assert.That(Describe(pattern)).IsEqualTo(expected);

    [Test]
    [Arguments("key('k')", "key", 1, 1)]
    [Arguments("key('k', 'v', .)", "key", 1, 1)]
    [Arguments("key('k', @id)", "key", 1, 1)]
    [Arguments("key('k', 1)", "key", 1, 1)]
    [Arguments("a | key(@k, 'v')", "key", 1, 5)]
    [Arguments("key('k', 'v')/ancestor::a", "axis", 1, 15)]
    [Arguments("key('k', 'v')/id('x')", "step-kind", 1, 15)]
    [Arguments("key('k', 'v')[1]", "step-kind", 1, 1)]
    [Arguments("fn:key('k', 'v')", "step-kind", 1, 1)]
    public async Task TryCompile_KeyStartOutsideIdKeyPattern_ReportsUnsupportedPattern(
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
    public async Task TryCompile_KeyStartUnderAnXPathBinding_StaysUnsupported()
    {
        var matcher = new PatternMatcher(new PatternTestBinding());

        var compiled = matcher.TryCompile("key('k', 'v')/a", PatternUse.RuleContext, Scope, out _, out var problem);

        await Assert.That(compiled).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.PatternUnsupported);
        await Assert.That(problem.Constraint).IsEqualTo("step-kind");
    }

    [Test]
    public async Task TryCompile_KeyStartThatDoesNotCompile_ReportsTheProblemOfTheBinding()
    {
        var matcher = new PatternMatcher(new PatternTestBinding(true));

        var compiled = matcher.TryCompile("key('invalid', 'v')", PatternUse.RuleContext, Scope, out _, out var problem);

        await Assert.That(compiled).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.ExpressionInvalid);
    }
}
