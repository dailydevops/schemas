namespace NetEvolve.Schemas.Xsd.Attributes.Declarations;

using System.Runtime.InteropServices;

/// <summary>A value constraint of an attribute declaration or use; <see langword="default"/> is no constraint.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct AttributeValueConstraint
{
    internal AttributeValueConstraint(AttributeValueConstraintKind kind, string value)
    {
        Kind = kind;
        Value = value;
    }

    /// <summary>Gets the kind.</summary>
    public AttributeValueConstraintKind Kind { get; }

    /// <summary>Gets the lexical value, <see langword="null"/> without a constraint.</summary>
    public string? Value { get; }
}
