namespace NetEvolve.Schemas.Schematron.Expansion.AbstractPatterns;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>Builds the concrete pattern of one <c>is-a</c> instantiation, or reports why it cannot be built.</summary>
internal static class PatternInstance
{
    internal static bool TryCreate(
        SchematronPatternBase site,
        IReadOnlyDictionary<string, SchematronPatternBase> catalog,
        IProblemSink problems,
        out SchematronPatternBase instance
    )
    {
        Argument.ThrowIfNull(site);
        Argument.ThrowIfNull(catalog);
        Argument.ThrowIfNull(problems);

        instance = site;
        var id = site.IsA!;
        if (!catalog.TryGetValue(id, out var definition))
        {
            Report(
                problems,
                SchematronDiagnosticCodes.IdrefUnresolved,
                $"The is-a attribute names the pattern '{id}', which does not exist.",
                site.Location
            );
            return false;
        }

        if (!definition.IsAbstract)
        {
            Report(
                problems,
                SchematronDiagnosticCodes.AbstractTargetInvalid,
                $"The is-a attribute names the pattern '{id}', which is not abstract.",
                site.Location
            );
            return false;
        }

        if (!TryBind(site, definition, problems, out var table))
        {
            return false;
        }

        instance = Build(site, definition, new PatternCopier(table, site.Location));
        return true;
    }

    private static SchematronPatternBase Build(
        SchematronPatternBase site,
        SchematronPatternBase definition,
        PatternCopier copier
    )
    {
        var lets = new List<SchematronLet>(copier.Lets(definition.Lets));
        lets.AddRange(site.Lets);
        var paragraphs = site.Paragraphs.Count > 0 ? site.Paragraphs : definition.Paragraphs;
        var rules = copier.Rules(definition.Rules);
        var documents = site.Documents ?? definition.Documents;
        var role = site.Role ?? definition.Role;
        var title = site.Title ?? definition.Title;

        return site is SchematronGroup
            ? new SchematronGroup(
                definition.Location,
                site.Id,
                false,
                site.IsA,
                documents,
                role,
                title,
                paragraphs,
                site.Parameters,
                lets,
                rules,
                site.Location
            )
            : new SchematronPattern(
                definition.Location,
                site.Id,
                false,
                site.IsA,
                documents,
                role,
                title,
                paragraphs,
                site.Parameters,
                lets,
                rules,
                site.Location
            );
    }

    // Pairs every parameter with its value; every problem of the instance is reported, not only the first.
    private static bool TryBind(
        SchematronPatternBase site,
        SchematronPatternBase definition,
        IProblemSink problems,
        out SubstitutionTable table
    )
    {
        var names = new List<string>();
        var values = new List<string>();
        var ok = true;
        var declared = definition.Parameters;
        if (declared.Count == 0)
        {
            foreach (var supplied in site.Parameters)
            {
                if (supplied.Value is null)
                {
                    ok = false;
                    Missing(problems, supplied.Name, supplied.Location);
                    continue;
                }

                names.Add(supplied.Name);
                values.Add(supplied.Value);
            }
        }
        else
        {
            foreach (var supplied in site.Parameters)
            {
                if (Find(declared, supplied.Name) is null)
                {
                    ok = false;
                    Report(
                        problems,
                        SchematronDiagnosticCodes.ParamUnknown,
                        $"The pattern '{definition.Id}' declares no parameter '{supplied.Name}'.",
                        supplied.Location
                    );
                }
            }

            foreach (var parameter in declared)
            {
                var supplied = Find(site.Parameters, parameter.Name);
                var value = supplied?.Value ?? parameter.Value;
                if (value is null)
                {
                    ok = false;
                    Missing(problems, parameter.Name, supplied?.Location ?? site.Location);
                    continue;
                }

                names.Add(parameter.Name);
                values.Add(value);
            }
        }

        table = new SubstitutionTable(names.ToArray(), values.ToArray());
        return ok;
    }

    private static SchematronParameter? Find(IReadOnlyList<SchematronParameter> parameters, string name)
    {
        foreach (var parameter in parameters)
        {
            if (string.Equals(parameter.Name, name, StringComparison.Ordinal))
            {
                return parameter;
            }
        }

        return null;
    }

    private static void Missing(IProblemSink problems, string name, DocumentLocation at) =>
        Report(
            problems,
            SchematronDiagnosticCodes.ParamMissing,
            $"The parameter '{name}' has neither a value nor a default.",
            at
        );

    private static void Report(IProblemSink problems, DiagnosticCode code, string message, DocumentLocation at) =>
        problems.Add(new SchematronProblem(code, message, at));
}
