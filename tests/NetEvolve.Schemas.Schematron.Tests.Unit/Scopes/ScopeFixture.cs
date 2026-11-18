namespace NetEvolve.Schemas.Schematron.Tests.Unit.Scopes;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Phases;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;

/// <summary>Selects the phase, plans the scopes with the fake binding, and keeps what the tests inspect.</summary>
internal sealed class ScopeFixture
{
    private ScopeFixture(bool ok, ScopePlan plan, FakeProblemSink problems, ScopeTestBinding binding)
    {
        Ok = ok;
        Plan = plan;
        Problems = problems;
        Binding = binding;
    }

    internal bool Ok { get; }

    internal ScopePlan Plan { get; }

    internal FakeProblemSink Problems { get; }

    internal ScopeTestBinding Binding { get; }

    internal static ScopeFixture Create(SchematronSchema schema, string phase = "#ALL")
    {
        var problems = new FakeProblemSink();
        var binding = new ScopeTestBinding();
        if (!PhaseSelector.TryResolve(schema, phase, problems, out var selection))
        {
            throw new System.InvalidOperationException("The phase of the test schema does not resolve.");
        }

        var ok = ScopePlanner.TryPlan(schema, selection, binding, problems, out var plan);
        return new ScopeFixture(ok, plan, problems, binding);
    }

    /// <summary>The visible variable names of a compile scope, in slot order.</summary>
    internal static string Visible(QueryStaticScope scope)
    {
        var names = new List<string>();
        foreach (var name in scope.VariableNames)
        {
            if (name.Length > 0)
            {
                names.Add(name);
            }
        }

        return string.Join(",", names);
    }

    /// <summary>The slot a name resolves to in a scope, or -1 when the name is not visible.</summary>
    internal static int SlotOf(QueryStaticScope scope, string name) => ScopeTestBinding.Resolve(scope, name);

    internal static string Codes(FakeProblemSink problems)
    {
        var codes = new List<string>();
        foreach (var problem in problems.Problems)
        {
            codes.Add(problem.Code.ToString());
        }

        return string.Join(",", codes);
    }
}
