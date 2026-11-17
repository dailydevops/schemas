namespace NetEvolve.Schemas.Schematron.Seams;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;

/// <summary>The immutable compile-time scope of an expression: namespaces, declared variables, base URI, and edition.</summary>
internal sealed class QueryStaticScope
{
    internal QueryStaticScope(
        IReadOnlyList<SchematronNamespace> namespaces,
        IReadOnlyList<string> variableNames,
        string? baseUri,
        SchematronEdition edition
    )
    {
        Namespaces = namespaces;
        VariableNames = variableNames;
        BaseUri = baseUri;
        Edition = edition;
    }

    internal IReadOnlyList<SchematronNamespace> Namespaces { get; }

    internal IReadOnlyList<string> VariableNames { get; }

    internal string? BaseUri { get; }

    internal SchematronEdition Edition { get; }
}
