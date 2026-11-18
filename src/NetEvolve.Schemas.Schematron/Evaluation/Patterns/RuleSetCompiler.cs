namespace NetEvolve.Schemas.Schematron.Evaluation.Patterns;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>Compiles the rules of the active patterns: contexts through the pattern matcher, assertions through <see cref="Assertions.AssertionCompiler"/>.</summary>
internal static class RuleSetCompiler
{
    /// <summary>Compiles one pattern or group; every problem is reported and compiling continues, so one call finds them all.</summary>
    internal static bool TryCompile(
        SchematronSchema schema,
        PatternScope pattern,
        IPatternMatcher matcher,
        IQueryBinding binding,
        IProblemSink problems,
        out CompiledRuleSet compiled
    ) => throw NotImplementedFailure.For(nameof(RuleSetCompiler));
}
