namespace NetEvolve.Schemas.Schematron.Model;

using System.Collections.Generic;

/// <summary>The <c>properties</c> element.</summary>
public sealed class SchematronProperties : SchematronNode
{
    internal SchematronProperties(DocumentLocation location, IReadOnlyList<SchematronProperty> items)
        : base(location) => Items = items;

    /// <summary>Gets the properties.</summary>
    public IReadOnlyList<SchematronProperty> Items { get; }
}
