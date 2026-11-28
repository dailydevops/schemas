namespace NetEvolve.Schemas.Schematron.Evaluation.Patterns;

using System.Collections.Generic;
using System.Text;
using NetEvolve.Schemas.Schematron.Evaluation.Annotations;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// Compiles the rules of an active pattern or group: every <c>context</c> through the <see cref="IPatternMatcher"/> in the scope of
/// the pattern (it may read the variables of the pattern, schema, and phase, design #150 EP-4), and every assertion through
/// <see cref="AssertionCompiler"/> in the scope of its rule.
/// </summary>
/// <remarks>
/// The ordinal of the pattern in the assertion identifier counts every non-abstract pattern and group of the schema, so a phase
/// never renumbers it (design #137 AP-13). Limitation: the expansion ordinal of an assertion copied from an abstract definition is
/// 0 here, because the effective model does not carry it yet.
/// </remarks>
internal static class RuleSetCompiler
{
    /// <summary>Compiles one pattern or group; every problem is reported and compiling continues, so one call finds them all.</summary>
    /// <param name="schema">The effective schema, which declares the diagnostics and properties and fixes the pattern ordinal.</param>
    /// <param name="pattern">The pattern or group with its scopes.</param>
    /// <param name="matcher">Compiles the rule contexts.</param>
    /// <param name="binding">Compiles the expressions of the assertions.</param>
    /// <param name="problems">Receives every problem.</param>
    /// <param name="compiled">The compiled rules; valid only when the method returns <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when no problem was reported.</returns>
    internal static bool TryCompile(
        SchematronSchema schema,
        PatternScope pattern,
        IPatternMatcher matcher,
        IQueryBinding binding,
        IProblemSink problems,
        out CompiledRuleSet compiled
    )
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(matcher);
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(problems);

        // A pattern that is not part of the schema is a caller defect: its ordinal is 0 and the guard rejects it.
        var ordinal = OrdinalOf(schema, pattern.Pattern);
        ArgumentOutOfRangeException.ThrowIfLessThan(ordinal, 1, nameof(pattern));

        var rules = new CompiledRule[pattern.Rules.Count];
        var ok = true;
        for (var i = 0; i < rules.Length; i++)
        {
            ok &= TryCompileRule(
                schema,
                pattern,
                pattern.Rules[i],
                ordinal,
                i + 1,
                matcher,
                binding,
                problems,
                out var rule
            );
            rules[i] = rule;
        }

        ok &= AnnotationCompiler.TryCompile(
            pattern.Pattern.Role,
            null,
            null,
            pattern.Pattern.Location,
            pattern.Scope,
            binding,
            problems,
            out var annotations
        );

        compiled = ok
            ? new CompiledRuleSet(
                pattern,
                pattern.Pattern.Id ?? string.Empty,
                pattern.Pattern.Documents ?? string.Empty,
                TitleText(pattern.Pattern.Title),
                pattern.Pattern.IsGroup,
                rules,
                annotations
            )
            : null!;
        return ok;
    }

    private static bool TryCompileRule(
        SchematronSchema schema,
        PatternScope pattern,
        RuleScope scope,
        int patternOrdinal,
        int ruleOrdinal,
        IPatternMatcher matcher,
        IQueryBinding binding,
        IProblemSink problems,
        out CompiledRule rule
    )
    {
        var text = scope.Rule.Context ?? string.Empty;
        var ok = matcher.TryCompile(
            text.AsSpan(),
            PatternUse.RuleContext,
            pattern.Scope,
            out var context,
            out var problem
        );
        if (!ok)
        {
            problem = problem.At(scope.Rule.Location);
            problems.Add(in problem);
        }

        var assertions = new List<CompiledAssertion>(scope.Rule.Items.Count);
        var ordinal = 0;
        foreach (var item in scope.Rule.Items)
        {
            if (item is not SchematronAssertion assertion)
            {
                continue;
            }

            // An assertion that does not compile still counts, so the ordinals after it stay the ones the author sees.
            ordinal++;
            var position = new AssertionPosition(patternOrdinal, ruleOrdinal, ordinal);
            if (
                AssertionCompiler.TryCompile(assertion, scope, schema, binding, in position, problems, out var compiled)
            )
            {
                assertions.Add(compiled);
            }
            else
            {
                ok = false;
            }
        }

        ok &= AnnotationCompiler.TryCompile(
            scope.Rule.Role,
            scope.Rule.Flags,
            null,
            scope.Rule.Location,
            scope.Scope,
            binding,
            problems,
            out var annotations
        );

        rule = ok
            ? new CompiledRule(scope, context, scope.Rule.Id ?? string.Empty, text, [.. assertions], annotations)
            : null!;
        return ok;
    }

    private static int OrdinalOf(SchematronSchema schema, SchematronPatternBase pattern)
    {
        var ordinal = 0;
        foreach (var candidate in schema.Patterns)
        {
            if (candidate.IsAbstract)
            {
                continue;
            }

            ordinal++;
            if (ReferenceEquals(candidate, pattern))
            {
                return ordinal;
            }
        }

        return 0;
    }

    // The text of the title: the plain text parts in order; the span and emphasis parts of a title carry no information a report needs.
    internal static string TitleText(SchematronTitle? title)
    {
        if (title is null)
        {
            return string.Empty;
        }

        var text = new StringBuilder();
        foreach (var part in title.Parts)
        {
            if (part is SchematronText plain)
            {
                _ = text.Append(plain.Text);
            }
        }

        return text.ToString();
    }
}
