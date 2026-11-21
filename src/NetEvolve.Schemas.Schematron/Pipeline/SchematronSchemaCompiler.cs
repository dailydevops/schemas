namespace NetEvolve.Schemas.Schematron.Pipeline;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Bindings.Registry;
using NetEvolve.Schemas.Schematron.Evaluation.Patterns;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Patterns;
using NetEvolve.Schemas.Schematron.Phases;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;

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
/// Limitation: the XSLT declarations of a schema (<c>xsl:function</c>, <c>xsl:key</c>, <c>xsl:variable</c>, <c>xsl:include</c>, and the
/// others) are reported as <see cref="SchematronDiagnosticCodes.XsltUnsupported"/> for a binding of the XSLT family, because the modules
/// are not linked and compiled yet (#832); an element of another namespace is a data element and is ignored.
/// </para>
/// </remarks>
internal sealed class SchematronSchemaCompiler : ISchemaCompiler
{
    private const string XsltNamespace = "http://www.w3.org/1999/XSL/Transform";

    private readonly QueryBindingRegistry _bindings;

    internal SchematronSchemaCompiler(QueryBindingRegistry bindings)
    {
        Argument.ThrowIfNull(bindings);

        _bindings = bindings;
    }

    public bool TryCompile(
        SchematronSchema effective,
        BuildSettings settings,
        IProblemSink problems,
        out CompiledSchema compiled
    )
    {
        Argument.ThrowIfNull(effective);
        Argument.ThrowIfNull(settings);
        Argument.ThrowIfNull(problems);

        compiled = null!;
        if (!_bindings.TryResolve(effective, out var binding, out var unsupported))
        {
            problems.Add(in unsupported);
            return false;
        }

        var ok = CheckDeclarations(effective, binding, problems);
        ok &= CheckParameters(effective, settings, problems);
        ok &= PhaseSelector.TryResolve(effective, settings.Phase, problems, out var selection);
        ok &= ScopePlanner.TryPlan(effective, selection, binding, problems, out var plan);

        var matcher = new PatternMatcher(binding);
        var sets = new List<CompiledRuleSet>(plan.Patterns.Count);
        foreach (var scope in plan.Patterns)
        {
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
            TitleOf(effective.Title),
            binding,
            matcher,
            new QueryResources(settings.Resolver),
            plan,
            sets
        );
        return true;
    }

    private static bool CheckDeclarations(SchematronSchema schema, IQueryBinding binding, IProblemSink problems)
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

    // The text of the title: the plain text parts in order; the span and emphasis parts of a title carry no information a report needs.
    private static string TitleOf(SchematronTitle? title)
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
