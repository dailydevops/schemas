namespace NetEvolve.Schemas.Schematron.Phases;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>
/// Resolves the requested phase of a built schema (design #137 AP-8): <c>#ALL</c> activates every non-abstract pattern and group,
/// <c>#DEFAULT</c> means <c>defaultPhase</c> or <c>#ALL</c>, any other name is a phase id. <c>#ANY</c> is an ordinary unknown name
/// (<c>SCH-PHASE-UNKNOWN</c>). <c>phase/@from</c> and <c>phase/@when</c> are deferred (#385, #386): a phase that carries one is
/// <c>SCH-UNSUPPORTED</c>, whether or not it is selected, because the parser keeps both as raw text. Active patterns keep schema
/// document order and an <c>active</c> element naming an abstract pattern activates nothing; an id that names no pattern or group is
/// <c>SCH-IDREF-UNRESOLVED</c>. Limitation: the problems are raised in the compile stage, not the load stage of the design.
/// </summary>
internal static class PhaseSelector
{
    private const string All = "#ALL";
    private const string Default = "#DEFAULT";

    internal static bool TryResolve(
        SchematronSchema schema,
        string requested,
        IProblemSink problems,
        out PhaseSelection selection
    )
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(requested);
        ArgumentNullException.ThrowIfNull(problems);

        selection = new PhaseSelection(All, [], []);
        var ok = true;
        foreach (var phase in schema.Phases)
        {
            ok &= Check(phase, "from", phase.From, problems);
            ok &= Check(phase, "when", phase.When, problems);
        }

        var name = requested;
        if (Same(name, Default))
        {
            name = schema.DefaultPhase ?? All;
        }

        if (Same(name, All))
        {
            selection = new PhaseSelection(All, Concrete(schema.Patterns), []);
            return ok;
        }

        SchematronPhase? selected = null;
        foreach (var phase in schema.Phases)
        {
            if (Same(phase.Id, name))
            {
                selected = phase;
                break;
            }
        }

        if (selected is null)
        {
            // An unresolved defaultPhase is a dangling idref; a requested name that matches nothing is an unknown phase.
            var code = Same(requested, Default)
                ? SchematronDiagnosticCodes.IdrefUnresolved
                : SchematronDiagnosticCodes.PhaseUnknown;
            problems.Add(new SchematronProblem(code, $"The phase '{name}' is not declared.", schema.Location));
            return false;
        }

        ok &= TryActivate(schema, selected, problems, out var patterns);

        selection = new PhaseSelection(selected.Id, patterns, selected.Lets);
        return ok;
    }

    private static bool TryActivate(
        SchematronSchema schema,
        SchematronPhase selected,
        IProblemSink problems,
        out List<SchematronPatternBase> patterns
    )
    {
        var ok = true;
        var active = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in selected.Actives)
        {
            if (!Exists(schema.Patterns, item.Pattern))
            {
                problems.Add(
                    new SchematronProblem(
                        SchematronDiagnosticCodes.IdrefUnresolved,
                        $"The active element names '{item.Pattern}', which matches no pattern or group.",
                        item.Location
                    )
                );
                ok = false;
            }
            else
            {
                _ = active.Add(item.Pattern);
            }
        }

        patterns = new List<SchematronPatternBase>();
        foreach (var pattern in Concrete(schema.Patterns))
        {
            if (pattern.Id is not null && active.Contains(pattern.Id))
            {
                patterns.Add(pattern);
            }
        }

        return ok;
    }

    private static bool Same(string? left, string? right) => string.Equals(left, right, StringComparison.Ordinal);

    private static bool Check(SchematronPhase phase, string attribute, string? value, IProblemSink problems)
    {
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        problems.Add(
            new SchematronProblem(
                SchematronDiagnosticCodes.Unsupported,
                $"The attribute '{attribute}' of phase '{phase.Id}' is not supported.",
                phase.Location
            )
        );
        return false;
    }

    private static bool Exists(IReadOnlyList<SchematronPatternBase> patterns, string id)
    {
        foreach (var pattern in patterns)
        {
            if (Same(pattern.Id, id))
            {
                return true;
            }
        }

        return false;
    }

    private static List<SchematronPatternBase> Concrete(IReadOnlyList<SchematronPatternBase> patterns)
    {
        var result = new List<SchematronPatternBase>(patterns.Count);
        foreach (var pattern in patterns)
        {
            if (!pattern.IsAbstract)
            {
                result.Add(pattern);
            }
        }

        return result;
    }
}
