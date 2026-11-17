namespace NetEvolve.Schemas.Xsd.Composition;

using System.Collections.Generic;

/// <summary>The ordered units of a composed schema: the root document first, then the documents in the order they were reached.</summary>
internal sealed class SchemaDocumentSet
{
    internal SchemaDocumentSet(List<SchemaUnit> units) => Units = units;

    /// <summary>Gets the units.</summary>
    internal IReadOnlyList<SchemaUnit> Units { get; }
}
