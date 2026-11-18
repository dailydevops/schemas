namespace NetEvolve.Schemas.Schematron.Scopes;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Phases;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>The state of one planning pass: the names visible at the current position, indexed by slot.</summary>
internal sealed class PlanRun
{
    private readonly SchematronSchema _schema;
    private readonly IQueryBinding _binding;
    private readonly IProblemSink _problems;
    private string[] _names = [];
    private bool _ok = true;

    internal PlanRun(SchematronSchema schema, IQueryBinding binding, IProblemSink problems)
    {
        Argument.ThrowIfNull(schema);
        Argument.ThrowIfNull(binding);
        Argument.ThrowIfNull(problems);

        _schema = schema;
        _binding = binding;
        _problems = problems;
    }

    internal bool Execute(PhaseSelection selection, out ScopePlan plan)
    {
        Argument.ThrowIfNull(selection);

        var schemaCount = _schema.Parameters.Count + _schema.Lets.Count;
        var patternStart = schemaCount + selection.Lets.Count;
        var maxPatternLets = 0;
        var maxRuleLets = 0;
        foreach (var pattern in selection.Patterns)
        {
            maxPatternLets = Math.Max(maxPatternLets, pattern.Lets.Count);
            foreach (var rule in pattern.Rules)
            {
                maxRuleLets = Math.Max(maxRuleLets, rule.Lets.Count);
            }
        }

        var ruleStart = patternStart + maxPatternLets;
        _names = new string[ruleStart + maxRuleLets];
        Clear(0);

        var globals = new List<VariableSlot>(schemaCount);
        DeclareParameters(globals);
        DeclareLets(_schema.Lets, _schema.Parameters.Count, 0, ScopeLevel.Schema, globals);
        var globalScope = Snapshot(schemaCount, null);

        var phaseLets = new List<VariableSlot>(selection.Lets.Count);
        DeclareLets(selection.Lets, schemaCount, schemaCount, ScopeLevel.Phase, phaseLets);

        var patterns = new List<PatternScope>(selection.Patterns.Count);
        foreach (var pattern in selection.Patterns)
        {
            patterns.Add(PlanPattern(pattern, patternStart, ruleStart));
        }

        plan = new ScopePlan(globals, phaseLets, patterns, globalScope, patternStart, ruleStart, _names.Length);
        return _ok;
    }

    private PatternScope PlanPattern(SchematronPatternBase pattern, int patternStart, int ruleStart)
    {
        Clear(patternStart);
        var lets = new List<VariableSlot>(pattern.Lets.Count);
        DeclareLets(pattern.Lets, patternStart, patternStart, ScopeLevel.Pattern, lets);
        var scope = Snapshot(patternStart + pattern.Lets.Count, null);

        var rules = new List<RuleScope>(pattern.Rules.Count);
        foreach (var rule in pattern.Rules)
        {
            if (rule.IsAbstract)
            {
                continue;
            }

            Clear(ruleStart);
            var ruleLets = new List<VariableSlot>(rule.Lets.Count);
            DeclareLets(rule.Lets, ruleStart, ruleStart, ScopeLevel.Rule, ruleLets);
            rules.Add(new RuleScope(rule, ruleLets, Snapshot(ruleStart + rule.Lets.Count, null)));
        }

        return new PatternScope(pattern, lets, scope, rules);
    }

    private void DeclareParameters(List<VariableSlot> into)
    {
        for (var i = 0; i < _schema.Parameters.Count; i++)
        {
            var parameter = _schema.Parameters[i];
            if (IsDuplicate(parameter.Name, 0, i))
            {
                Duplicate(parameter.Name, parameter.Location);
                continue;
            }

            var value = parameter.Value is null ? null : Compile(parameter.Value, i, null);
            _names[i] = parameter.Name;
            into.Add(
                new VariableSlot(
                    parameter.Name,
                    i,
                    ScopeLevel.Schema,
                    parameter.Location,
                    value,
                    null,
                    null,
                    isParameter: true
                )
            );
        }
    }

    // The slot of the i-th let of a level is first + i; levelStart is the first slot of the level, for the duplicate check.
    private void DeclareLets(
        IReadOnlyList<SchematronLet> lets,
        int first,
        int levelStart,
        ScopeLevel level,
        List<VariableSlot> into
    )
    {
        for (var i = 0; i < lets.Count; i++)
        {
            var let = lets[i];
            var slot = first + i;
            if (IsDuplicate(let.Name, levelStart, slot))
            {
                Duplicate(let.Name, let.Location);
                continue;
            }

            var declared = DeclaredType(let);
            var value = Compile(let.Value, slot, declared);
            var problem = ConstantProblem(let, declared);
            _names[slot] = let.Name;
            if (problem is { } found && level <= ScopeLevel.Phase)
            {
                _problems.Add(in found);
                _ok = false;
                problem = null;
            }

            if (value is not null)
            {
                into.Add(new VariableSlot(let.Name, slot, level, let.Location, value, declared, problem));
            }
        }
    }

    // Compiles a value in the scope of its position: everything declared before slot, and nothing after it.
    private QueryExpression? Compile(string text, int slot, string? declaredType)
    {
        var scope = Snapshot(slot, declaredType);
        if (_binding.TryCompile(text.AsSpan(), QueryExpressionKind.Value, scope, out var expression, out var problem))
        {
            return expression;
        }

        _problems.Add(in problem);
        _ok = false;
        return null;
    }

    private string? DeclaredType(SchematronLet let)
    {
        if (let.DeclaredType is null)
        {
            return null;
        }

        if (ConstantValue.TryGetSupportedType(let.DeclaredType, _schema.Namespaces, out var supported))
        {
            return supported;
        }

        var problem = new SchematronProblem(
            SchematronDiagnosticCodes.Unsupported,
            $"The type '{let.DeclaredType}' of the variable '{let.Name}' is not supported; only xs:string, xs:boolean, xs:double, and xs:integer are checked.",
            let.Location
        );
        _problems.Add(in problem);
        _ok = false;
        return null;
    }

    private SchematronProblem? ConstantProblem(SchematronLet let, string? declared)
    {
        if (declared is null || !ConstantValue.TryGetType(let.Value.AsSpan(), _schema.Namespaces, out var constant))
        {
            return null;
        }

        if (ConstantValue.Satisfies(constant!, declared))
        {
            return null;
        }

        return new SchematronProblem(
            SchematronDiagnosticCodes.LetType,
            $"The constant {let.Value} of the variable '{let.Name}' is a {constant}, which does not match the declared type '{declared}'.",
            let.Location,
            "XPTY0004"
        );
    }

    private bool IsDuplicate(string name, int levelStart, int slot)
    {
        for (var i = levelStart; i < slot; i++)
        {
            if (string.Equals(_names[i], name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private void Duplicate(string name, DocumentLocation location)
    {
        var problem = new SchematronProblem(
            SchematronDiagnosticCodes.LetDuplicate,
            $"The variable '{name}' is declared more than once at the same level.",
            location
        );
        _problems.Add(in problem);
        _ok = false;
    }

    // Empties the names from first on, so a block that the next pattern or rule reuses starts without the last one's variables.
    private void Clear(int first)
    {
        for (var i = first; i < _names.Length; i++)
        {
            _names[i] = string.Empty;
        }
    }

    private QueryStaticScope Snapshot(int length, string? declaredType)
    {
        var names = new string[length];
        Array.Copy(_names, names, length);
        return new QueryStaticScope(_schema.Namespaces, names, _schema.Location.Source, _schema.Edition, declaredType);
    }
}
