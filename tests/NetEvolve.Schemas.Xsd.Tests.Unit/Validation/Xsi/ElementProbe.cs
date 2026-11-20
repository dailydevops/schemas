namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Xsi;

using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>Records the type of every element at its start hook, and its nilled flag and validity at its end hook.</summary>
internal sealed class ElementProbe : ValidationListener
{
    public List<IXsdComponent?> StartTypes { get; } = [];

    public List<bool> Nilled { get; } = [];

    public List<bool> Valid { get; } = [];

    public int SimpleValues { get; private set; }

    public override ListenerInterest Interest => ListenerInterest.All;

    public override void StartElement(in ElementStartInfo info) => StartTypes.Add(info.Type);

    public override void SimpleValue(in SimpleValueInfo info) => SimpleValues++;

    public override void EndElement(in ElementEndInfo info)
    {
        Nilled.Add(info.IsNilled);
        Valid.Add(info.IsValid);
    }
}
