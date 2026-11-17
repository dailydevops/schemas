#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas.Schematron for the public result types (design #137 section 2)

namespace NetEvolve.Schemas.Schematron;

using System;

/// <summary>An active pattern or group. Text members are valid only during the callback.</summary>
public readonly ref struct SchematronPatternEvent
{
    internal SchematronPatternEvent(
        ReadOnlySpan<char> id,
        ReadOnlySpan<char> documents,
        ReadOnlySpan<char> name,
        ReadOnlySpan<char> role,
        bool isGroup
    )
    {
        Id = id;
        Documents = documents;
        Name = name;
        Role = role;
        IsGroup = isGroup;
    }

    /// <summary>Gets the id, empty when none.</summary>
    public ReadOnlySpan<char> Id { get; }

    /// <summary>Gets the <c>documents</c> expression, empty when none.</summary>
    public ReadOnlySpan<char> Documents { get; }

    /// <summary>Gets the title text, empty when none.</summary>
    public ReadOnlySpan<char> Name { get; }

    /// <summary>Gets the role, empty when none.</summary>
    public ReadOnlySpan<char> Role { get; }

    /// <summary>Gets a value indicating whether the active element is a <see langword="group"/>.</summary>
    public bool IsGroup { get; }
}
