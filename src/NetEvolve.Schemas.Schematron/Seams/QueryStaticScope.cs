namespace NetEvolve.Schemas.Schematron.Seams;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Model;

/// <summary>
/// The immutable compile-time scope of an expression: namespaces, declared variables, base URI, and edition. The variable slot is
/// the index in <see cref="VariableNames"/>; an empty entry is a slot that is not visible at this position, and for a name
/// that occurs more than once the last entry wins, so an inner declaration shadows an outer one.
/// </summary>
internal sealed class QueryStaticScope
{
    internal QueryStaticScope(
        IReadOnlyList<SchematronNamespace> namespaces,
        IReadOnlyList<string> variableNames,
        string? baseUri,
        SchematronEdition edition,
        string? declaredType = null
    )
    {
        Namespaces = namespaces;
        VariableNames = variableNames;
        BaseUri = baseUri;
        Edition = edition;
        DeclaredType = declaredType;
    }

    internal IReadOnlyList<SchematronNamespace> Namespaces { get; }

    internal IReadOnlyList<string> VariableNames { get; }

    internal string? BaseUri { get; }

    internal SchematronEdition Edition { get; }

    /// <summary>
    /// Gets the <see langword="as"/> type of the variable the expression is the value of, or <see langword="null"/>; the binding checks the
    /// value against it. The name is canonical: <c>xs:</c> plus the local name, whatever prefix the schema used.
    /// </summary>
    internal string? DeclaredType { get; }
}
