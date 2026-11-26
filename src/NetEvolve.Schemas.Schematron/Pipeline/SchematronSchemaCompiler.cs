namespace NetEvolve.Schemas.Schematron.Pipeline;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Bindings.Registry;
using NetEvolve.Schemas.Schematron.Evaluation.Patterns;
using NetEvolve.Schemas.Schematron.Evaluation.Subordinate;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Patterns;
using NetEvolve.Schemas.Schematron.Phases;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Modules;

/// <summary>
/// The stage Compile (design #137 AP-26): the binding is resolved, the phase selected, the parameters checked, the variables planned,
/// and the rule contexts, tests, and messages of every active pattern compiled. Every problem of the stage is reported, in the order of
/// the steps and, within a step, in document order.
/// </summary>
/// <remarks>
/// <para>
/// A parameter of the builder that the schema does not declare is <see cref="SchematronDiagnosticCodes.ParamUnknown"/>, and a
/// schema-level <c>param</c> without a default and without a value from the caller is <see cref="SchematronDiagnosticCodes.ParamMissing"/>
/// (design #137 AP-24), both at <c>Build</c>.
/// </para>
/// <para>
/// The XSLT declarations of a schema (<c>xsl:function</c>, <c>xsl:key</c>, <c>xsl:variable</c>, <c>xsl:param</c>, and
/// <c>xsl:decimal-format</c>) arrive as the module set of stage Resolve under an XSLT binding and compile in two passes, around the
/// planner of the scopes (see <see cref="XsltDeclarations"/>); the tables are kept in <see cref="CompiledProgram"/>. An element that the
/// registry does not know, such as <c>xsl:template</c>, is <see cref="SchematronDiagnosticCodes.XsltUnsupported"/> at the module that
/// holds it, and an element of another namespace is a data element and is ignored.
/// </para>
/// <para>
/// Limitations: the keys and the stylesheet functions are callable from the XPath of a rule (the keys through the key host slot that a run assigns, #776); the stylesheet functions use the binding that <see cref="XsltDeclarations.WithSignatures"/> derives before any body compiles; without a module set (the four-argument form) the XSLT children of the schema are
/// <see cref="SchematronDiagnosticCodes.XsltUnsupported"/>.
/// </para>
/// </remarks>
internal sealed class SchematronSchemaCompiler : ISchemaCompiler
{
    private const string XsltNamespace = "http://www.w3.org/1999/XSL/Transform";

    private readonly QueryBindingRegistry _bindings;

    internal SchematronSchemaCompiler(QueryBindingRegistry bindings)
    {
        ArgumentNullException.ThrowIfNull(bindings);

        _bindings = bindings;
    }

    public bool TryCompile(
        SchematronSchema effective,
        BuildSettings settings,
        IProblemSink problems,
        out CompiledSchema compiled
    ) => TryCompile(effective, settings, XsltModuleSet.Empty, problems, out compiled);

    public bool TryCompile(
        SchematronSchema effective,
        BuildSettings settings,
        XsltModuleSet modules,
        IProblemSink problems,
        out CompiledSchema compiled
    )
    {
        ArgumentNullException.ThrowIfNull(effective);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(modules);
        ArgumentNullException.ThrowIfNull(problems);

        compiled = null!;
        if (!_bindings.TryResolve(effective, out var resolved, out var unsupported))
        {
            problems.Add(in unsupported);
            return false;
        }

        // Declarations that the resolver linked are compiled; without a module set the XSLT children of the schema are not supported.
        // The per-schema derivation (stylesheet function signatures) wraps the resolved binding first, so the recorder stays the outer layer.
        var linked = resolved.AllowsXsltInstructions && modules.Declarations.Count > 0;
        var binding = new CompilingBinding(
            linked ? XsltDeclarations.WithSignatures(resolved, effective, modules) : resolved
        );

        ScopePlan plan;
        var ok = linked || CheckDeclarations(effective, binding, problems);
        ok &= CheckParameters(effective, settings, problems);
        ok &= PhaseSelector.TryResolve(effective, settings.Phase, problems, out var selection);
        ok &= SubordinateConstructs.TryCheck(selection.Patterns, settings.Resolver, problems);
        XsltModuleTables? tables = null;
        if (linked)
        {
            ok &= XsltDeclarations.TryCompileGlobals(effective, modules, binding, problems, out var globals);
            ok &= ScopePlanner.TryPlan(
                effective,
                selection,
                globals.Globals.Declarations,
                binding,
                problems,
                out var plannedGlobals,
                settings.GlobalPatternLets
            );
            plan = plannedGlobals;
            ok &= XsltDeclarations.TryCompileFunctions(
                effective,
                modules,
                binding,
                plan,
                globals,
                problems,
                out var withFunctions
            );
            tables = withFunctions;
        }
        else
        {
            ok &= ScopePlanner.TryPlan(effective, selection, binding, problems, out plan, settings.GlobalPatternLets);
        }

        var matcher = new PatternMatcher(binding);
        var sets = new List<CompiledRuleSet>(plan.Patterns.Count);
        foreach (var scope in plan.Patterns)
        {
            // The pattern matcher is a seam with one implementation, so the contexts are recorded here.
            foreach (var rule in scope.Rules)
            {
                binding.Record((rule.Rule.Context ?? string.Empty).AsSpan(), rule.Scope);
            }

            var compiledSet = RuleSetCompiler.TryCompile(effective, scope, matcher, binding, problems, out var set);
            ok &= compiledSet;
            if (compiledSet)
            {
                sets.Add(set);
            }
        }

        if (!ok)
        {
            return false;
        }

        compiled = new CompiledProgram(
            effective,
            selection.EffectivePhase,
            RuleSetCompiler.TitleText(effective.Title),
            binding,
            matcher,
            new QueryResources(settings.Resolver, settings.Limits),
            plan,
            sets,
            tables
        );
        return true;
    }

    private static bool CheckDeclarations(SchematronSchema schema, CompilingBinding binding, IProblemSink problems)
    {
        var ok = true;
        if (!binding.AllowsXsltInstructions)
        {
            return ok;
        }

        foreach (var element in schema.ForeignElements)
        {
            if (string.Equals(element.NamespaceUri, XsltNamespace, StringComparison.Ordinal))
            {
                problems.Add(
                    new SchematronProblem(
                        SchematronDiagnosticCodes.XsltUnsupported,
                        $"The XSLT declaration 'xsl:{element.LocalName}' is not supported: the XSLT modules of a schema are not linked and compiled yet.",
                        element.Location
                    )
                );
                ok = false;
            }
        }

        return ok;
    }

    private static bool CheckParameters(SchematronSchema schema, BuildSettings settings, IProblemSink problems)
    {
        var ok = true;

        // The order of the parameters of the builder is unspecified; the issues of two builds have to be identical.
        var names = new List<string>(settings.Parameters.Keys);
        names.Sort(StringComparer.Ordinal);
        foreach (var name in names)
        {
            if (!Declares(schema, name))
            {
                problems.Add(
                    new SchematronProblem(
                        SchematronDiagnosticCodes.ParamUnknown,
                        $"The schema declares no parameter '{name}'.",
                        schema.Location
                    )
                );
                ok = false;
            }
        }

        foreach (var parameter in schema.Parameters)
        {
            if (parameter.Value is null && !settings.Parameters.ContainsKey(parameter.Name))
            {
                problems.Add(
                    new SchematronProblem(
                        SchematronDiagnosticCodes.ParamMissing,
                        $"The parameter '{parameter.Name}' has neither a default value nor a value from the caller.",
                        parameter.Location
                    )
                );
                ok = false;
            }
        }

        return ok;
    }

    private static bool Declares(SchematronSchema schema, string name)
    {
        foreach (var parameter in schema.Parameters)
        {
            if (string.Equals(parameter.Name, name, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
