namespace NetEvolve.Schemas.Xsd.Elements;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xsd.Components;

/// <summary>One by-name reference of a nested component, addressed through the component that owns it.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct RefSite
{
    internal RefSite(IReferencingComponent owner, int index)
    {
        Owner = owner;
        Index = index;
    }

    /// <summary>Gets the component that owns the reference.</summary>
    public IReferencingComponent Owner { get; }

    /// <summary>Gets the index of the reference in the owner.</summary>
    public int Index { get; }
}
