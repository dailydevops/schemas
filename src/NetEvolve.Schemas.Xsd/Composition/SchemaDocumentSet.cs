namespace NetEvolve.Schemas.Xsd.Composition;

using System.Collections.Generic;

/// <summary>The ordered units of a composed schema: the root document first, then the documents in the order they were reached.</summary>
internal sealed class SchemaDocumentSet
{
    internal SchemaDocumentSet(List<SchemaUnit> units, List<RedefinitionRestriction> restrictions)
    {
        Units = units;
        Restrictions = restrictions;
    }

    /// <summary>Gets the units.</summary>
    internal IReadOnlyList<SchemaUnit> Units { get; }

    /// <summary>Gets the redefinitions of a group or attribute group without a self-reference, which the component checks compare with the original.</summary>
    internal IReadOnlyList<RedefinitionRestriction> Restrictions { get; }
}
