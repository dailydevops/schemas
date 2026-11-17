#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas.Schematron for the public result types (design #137 section 2)

namespace NetEvolve.Schemas.Schematron;

using System;

/// <summary>The ids and rendered texts of the diagnostics or properties an assertion references. Valid only during the callback.</summary>
public readonly ref struct SchematronReferenceList
{
    private readonly ReadOnlySpan<string> _ids;
    private readonly ReadOnlySpan<string> _texts;

    internal SchematronReferenceList(ReadOnlySpan<string> ids, ReadOnlySpan<string> texts)
    {
        _ids = ids;
        _texts = texts;
    }

    /// <summary>Gets the number of references.</summary>
    public int Count => _ids.Length;

    /// <summary>Gets the id of a reference.</summary>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The id.</returns>
    public string IdAt(int index) => _ids[index];

    /// <summary>Gets the rendered text of a reference.</summary>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The text.</returns>
    public string TextAt(int index) => _texts[index];
}
