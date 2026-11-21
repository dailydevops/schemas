namespace NetEvolve.Schemas.Xsd.Derivation.Subsumption11;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Elements;

/// <summary>One content model of a subsumption check. Compile-only shape; the construction follows.</summary>
internal sealed class SubsumptionSide
{
    private SubsumptionSide(Particle root, int names)
    {
        Root = root;
        Names = names;
    }

    internal Particle Root { get; }

    internal int Names { get; }

    internal static SubsumptionSide Build(Particle root, Dictionary<XPathQName, int> ids, List<XPathQName> names) =>
        new(root, ids.Count + names.Count);
}
