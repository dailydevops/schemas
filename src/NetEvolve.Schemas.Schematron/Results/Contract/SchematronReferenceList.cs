#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas.Schematron for the public result types (design #137 section 2)

namespace NetEvolve.Schemas.Schematron;

using System;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>The ids and rendered texts of the diagnostics or properties an assertion references. Valid only during the callback.</summary>
public readonly ref struct SchematronReferenceList
{
    private readonly ReadOnlySpan<string> _ids;
    private readonly ReadOnlySpan<string> _texts;
    private readonly ReadOnlySpan<IFindingContent?> _contents;
    private readonly ReadOnlySpan<string> _languages;

    internal SchematronReferenceList(
        ReadOnlySpan<string> ids,
        ReadOnlySpan<string> texts,
        ReadOnlySpan<IFindingContent?> contents = default,
        ReadOnlySpan<string> languages = default
    )
    {
        _ids = ids;
        _texts = texts;
        _contents = contents;
        _languages = languages;
    }

    /// <summary>Gets the language in effect for a reference (the nearest <c>xml:lang</c>), or an empty string when it has none.</summary>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The language.</returns>
    internal string LanguageAt(int index) => (uint)index < (uint)_languages.Length ? _languages[index] : string.Empty;

    /// <summary>
    /// Gets the structured content of a reference, which a property with <c>xsl:copy-of</c> has besides its text, or <see langword="null"/>
    /// when the reference has none (SCH-84, #289). It is an owned snapshot, so the report writer may keep it after the callback.
    /// </summary>
    /// <param name="index">The zero-based index.</param>
    /// <returns>The content, or <see langword="null"/>.</returns>
    internal IFindingContent? ContentAt(int index) => (uint)index < (uint)_contents.Length ? _contents[index] : null;

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
