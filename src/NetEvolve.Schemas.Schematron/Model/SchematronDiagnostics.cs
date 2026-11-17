namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

/// <summary>The <c>diagnostics</c> element.</summary>
public sealed class SchematronDiagnostics : SchematronNode
{
    internal SchematronDiagnostics(DocumentLocation location, IReadOnlyList<SchematronDiagnostic> items)
        : base(location) => Items = items;

    /// <summary>Gets the diagnostics.</summary>
    public IReadOnlyList<SchematronDiagnostic> Items { get; }
}
