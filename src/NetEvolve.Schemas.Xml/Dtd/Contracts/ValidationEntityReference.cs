namespace NetEvolve.Schemas.Xml.Dtd.Contracts;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>An entity reference in content as a validation step sees it; it arrives before any replacement event of the reference.</summary>
/// <remarks>The memory is valid only during the call that delivers the reference.</remarks>
[StructLayout(LayoutKind.Auto)]
public readonly record struct ValidationEntityReference
{
    /// <summary>Initializes a new instance of the <see cref="ValidationEntityReference"/> struct.</summary>
    /// <param name="name">The entity name.</param>
    /// <param name="position">The position of the reference.</param>
    /// <param name="outcome">What the lookup of the entity found.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outcome"/> is not a defined value.</exception>
    public ValidationEntityReference(ReadOnlyMemory<char> name, XmlPosition position, DtdEntityOutcome outcome)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)outcome, (int)DtdEntityOutcome.Undeclared);
        ArgumentOutOfRangeException.ThrowIfLessThan((int)outcome, (int)DtdEntityOutcome.DeclaredInternal);

        Name = name;
        Position = position;
        Outcome = outcome;
    }

    /// <summary>Gets the entity name.</summary>
    public ReadOnlyMemory<char> Name { get; }

    /// <summary>Gets the position of the reference.</summary>
    public XmlPosition Position { get; }

    /// <summary>Gets what the lookup of the entity found.</summary>
    public DtdEntityOutcome Outcome { get; }
}
