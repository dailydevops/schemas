#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas.Schematron for the public result types (design #137 section 2)

namespace NetEvolve.Schemas.Schematron;

using System;

/// <summary>A fired rule. Text members are valid only during the callback.</summary>
public readonly ref struct SchematronRuleEvent
{
    internal SchematronRuleEvent(
        ReadOnlySpan<char> id,
        ReadOnlySpan<char> context,
        ReadOnlySpan<char> role,
        ReadOnlySpan<char> flags,
        ReadOnlySpan<char> document
    )
    {
        Id = id;
        Context = context;
        Role = role;
        Flags = flags;
        Document = document;
    }

    /// <summary>Gets the rule id, empty when none.</summary>
    public ReadOnlySpan<char> Id { get; }

    /// <summary>Gets the raw context expression.</summary>
    public ReadOnlySpan<char> Context { get; }

    /// <summary>Gets the role, empty when none.</summary>
    public ReadOnlySpan<char> Role { get; }

    /// <summary>Gets the flag tokens joined with one space, empty when none.</summary>
    public ReadOnlySpan<char> Flags { get; }

    /// <summary>Gets the system identifier of the document the rule fired in, empty when none.</summary>
    public ReadOnlySpan<char> Document { get; }
}
