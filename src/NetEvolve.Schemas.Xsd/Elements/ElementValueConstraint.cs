namespace NetEvolve.Schemas.Xsd.Elements;

using System.Runtime.InteropServices;

/// <summary>The value constraint of an element declaration: none, a default, or a fixed value in its lexical form.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct ElementValueConstraint
{
    internal ElementValueConstraint(ElementValueConstraintKind kind, string value)
    {
        Kind = kind;
        Value = value;
    }

    /// <summary>Gets the kind.</summary>
    public ElementValueConstraintKind Kind { get; }

    /// <summary>Gets the lexical value, empty when there is no constraint.</summary>
    public string Value { get; }
}
