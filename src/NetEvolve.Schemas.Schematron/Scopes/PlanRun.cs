namespace NetEvolve.Schemas.Schematron.Scopes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Phases;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>The state of one planning pass: the names visible at the current position, indexed by slot.</summary>
internal sealed class PlanRun
{
    private readonly SchematronSchema _schema;
    private readonly IQueryBinding _binding;
    private readonly IProblemSink _problems;
    private readonly IReadOnlyList<GlobalDeclaration> _globals;
    private readonly bool _hoist;
    private string[] _names = [];
    private bool _ok = true;

    internal PlanRun(
        SchematronSchema schema,
        IQueryBinding binding,
        IProblemSink problems,
        IReadOnlyList<GlobalDeclaration> globals,
        bool hoistPatternLets = false
    )
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(binding);
        ArgumentNullException.ThrowIfNull(problems);
        ArgumentNullException.ThrowIfNull(globals);

        _schema = schema;
        _binding = binding;
        _problems = problems;
        _globals = globals;
        _hoist = hoistPatternLets;
    }

    internal bool Execute(PhaseSelection selection, out ScopePlan plan)
    {
        ArgumentNullException.ThrowIfNull(selection);

        var hoisted = 0;
        var maxPatternLets = 0;
        var maxRuleLets = 0;
        foreach (var pattern in selection.Patterns)
        {
            if (_hoist)
            {
                hoisted += pattern.Lets.Count;
            }
            else
            {
                maxPatternLets = Math.Max(maxPatternLets, pattern.Lets.Count);
            }

            foreach (var rule in pattern.Rules)
            {
                maxRuleLets = Math.Max(maxRuleLets, rule.Lets.Count);
            }
        }

        var letCount = _schema.Lets.Count;
        var schemaCount = _schema.Parameters.Count + _globals.Count + letCount + hoisted;
        var patternStart = schemaCount + selection.Lets.Count;
        var ruleStart = patternStart + maxPatternLets;
        _names = new string[ruleStart + maxRuleLets];
        Clear(0);

        var globals = new List<VariableSlot>(schemaCount);
        DeclareParameters(globals);
        var globalStart = _schema.Parameters.Count;
        ReserveGlobals(globalStart);
        DeclareLets(_schema.Lets, globalStart + _globals.Count, 0, ScopeLevel.Schema, globals);
        DeclareHoisted(selection, globalStart + _globals.Count + letCount, globals);
        DeclareGlobals(globalStart, schemaCount, globals);
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

    // The lets of the active patterns become schema-level lets after the schema lets, in document order (#615, option B of #150 EP-4): the
    // duplicate check runs from the first schema slot, so a collision with a parameter, a global, a schema let, or an earlier pattern let is
    // SCH-LET-DUPLICATE. A phase let is declared later at its own level and still shadows.
    private void DeclareHoisted(PhaseSelection selection, int first, List<VariableSlot> into)
    {
        if (!_hoist)
        {
            return;
        }

        var slot = first;
        foreach (var pattern in selection.Patterns)
        {
            DeclareLets(pattern.Lets, slot, 0, ScopeLevel.Schema, into);
            slot += pattern.Lets.Count;
        }
    }

    private PatternScope PlanPattern(SchematronPatternBase pattern, int patternStart, int ruleStart)
    {
        Clear(patternStart);
        var lets = new List<VariableSlot>(_hoist ? 0 : pattern.Lets.Count);
        var letCount = _hoist ? 0 : pattern.Lets.Count;
        if (!_hoist)
        {
            DeclareLets(pattern.Lets, patternStart, patternStart, ScopeLevel.Pattern, lets);
        }

        var scope = Snapshot(patternStart + letCount, null, pattern.BaseUri);

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
            rules.Add(new RuleScope(rule, ruleLets, Snapshot(ruleStart + rule.Lets.Count, null, rule.BaseUri)));
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

            var value = parameter.Value is null ? null : Compile(parameter.Value, parameter.Location, i, null);
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

    // The names of the global xsl:variable and xsl:param declarations are entered before any value is compiled, so that the
    // declarations are order independent: a let, a parameter default after them, and every global sees all of them (XSLT 2.0 section 9.5).
    private void ReserveGlobals(int first)
    {
        for (var i = 0; i < _globals.Count; i++)
        {
            var global = _globals[i];
            if (IsDuplicate(global.Name, 0, first + i))
            {
                Duplicate(global.Name, global.Location);
                continue;
            }

            _names[first + i] = global.Name;
        }
    }

    // A global value is compiled in the scope of the whole schema level, so it may read any parameter, let, or global wherever it is
    // declared; a read of itself or a cycle is found when the value is used (XTDE0640), not here.
    private void DeclareGlobals(int first, int schemaCount, List<VariableSlot> into)
    {
        for (var i = 0; i < _globals.Count; i++)
        {
            var global = _globals[i];
            var slot = first + i;
            if (_names[slot].Length == 0)
            {
                continue;
            }

            var value = global.Value is null
                ? null
                : Compile(global.Value, global.Location, schemaCount, global.DeclaredType);
            if (global.Value is null || value is not null)
            {
                into.Add(
                    new VariableSlot(
                        global.Name,
                        slot,
                        ScopeLevel.Schema,
                        global.Location,
                        value,
                        global.DeclaredType,
                        null,
                        global.IsParameter
                    )
                );
            }
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

            if (let.Content is not null)
            {
                DeclareContent(let, slot, level, into);
                continue;
            }

            var declared = DeclaredType(let);
            var value = Compile(let.Value, let.Location, slot, declared, let.BaseUri);
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

    // A let with element content has no expression to compile: its value is a temporary tree that the binder builds when the slot is
    // read. The name is visible to the later values at once. An as type is not supported with content yet (SCH-84, #289).
    private void DeclareContent(SchematronLet let, int slot, ScopeLevel level, List<VariableSlot> into)
    {
        _names[slot] = let.Name;
        if (let.DeclaredType is not null)
        {
            var problem = new SchematronProblem(
                SchematronDiagnosticCodes.Unsupported,
                $"The variable '{let.Name}' has element content and the type '{let.DeclaredType}'; a type is not supported with element content.",
                let.Location
            );
            _problems.Add(in problem);
            _ok = false;
            return;
        }

        into.Add(new VariableSlot(let.Name, slot, level, let.Location, null, null, null, content: let.Content));
    }

    // Compiles a value in the scope of its position: everything declared before slot, and nothing after it.
    private QueryExpression? Compile(
        string text,
        DocumentLocation location,
        int slot,
        string? declaredType,
        string? baseUri = null
    )
    {
        var scope = Snapshot(slot, declaredType, baseUri);
        if (_binding.TryCompile(text.AsSpan(), QueryExpressionKind.Value, scope, out var expression, out var problem))
        {
            return expression;
        }

        problem = problem.At(location);
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

    private QueryStaticScope Snapshot(int length, string? declaredType, string? baseUri = null)
    {
        var names = new string[length];
        Array.Copy(_names, names, length);
        return new QueryStaticScope(
            _schema.Namespaces,
            names,
            baseUri ?? _schema.Location.Source,
            _schema.Edition,
            declaredType
        );
    }
}
