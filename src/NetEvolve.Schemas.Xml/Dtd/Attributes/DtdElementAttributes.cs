namespace NetEvolve.Schemas.Xml.Dtd.Attributes;

using System;
using System.Collections.Generic;
using System.Text;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Text.NameTable;

/// <summary>
/// The attribute definitions of one element type, compiled for the reader: a lookup of a definition by the attribute name
/// as written, which allocates nothing, the definitions that carry a default or a fixed value, and the literal of each
/// default with its line ends normalized.
/// </summary>
/// <remarks>The first definition of an attribute binds; the declaration model already drops the later ones.</remarks>
internal sealed class DtdElementAttributes
{
    private readonly DtdAttributeDefinition[] _definitions;
    private readonly string?[] _defaultTexts;
    private readonly SpanInterningNameTable _names;
    private readonly int[] _indexBySymbol;
    private readonly int[] _defaulted;

    /// <summary>Initializes a new instance of the <see cref="DtdElementAttributes"/> class.</summary>
    /// <param name="definitions">The definitions of the element type in declaration order.</param>
    internal DtdElementAttributes(IReadOnlyList<DtdAttributeDefinition> definitions)
    {
        Argument.ThrowIfNull(definitions);

        _definitions = new DtdAttributeDefinition[definitions.Count];
        _defaultTexts = new string?[definitions.Count];
        _names = new SpanInterningNameTable(definitions.Count + 1, definitions.Count + 1);
        _indexBySymbol = new int[definitions.Count + 1];
        var defaulted = new List<int>();
        for (var i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            _definitions[i] = definition;
            _ = _names.TryIntern(definition.Name.AsSpan(), out var symbol);
            _indexBySymbol[symbol.Value] = i;
            if (definition.DefaultKind is DtdDefaultKind.Default or DtdDefaultKind.Fixed)
            {
                _defaultTexts[i] = NormalizeLineEnds(definition.DefaultValue ?? string.Empty);
                defaulted.Add(i);
            }
        }

        _defaulted = [.. defaulted];
    }

    /// <summary>Gets the number of definitions.</summary>
    internal int Count => _definitions.Length;

    /// <summary>Gets the indexes of the definitions that carry a default or a fixed value, in declaration order.</summary>
    internal ReadOnlySpan<int> Defaulted => _defaulted;

    /// <summary>Gets a definition.</summary>
    /// <param name="index">The index, from <see cref="TryFind"/> or <see cref="Defaulted"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not the index of a definition.</exception>
    internal DtdAttributeDefinition this[int index]
    {
        get
        {
            Argument.ThrowIfLessThan(index, 0);
            Argument.ThrowIfGreaterThanOrEqual(index, _definitions.Length);

            return _definitions[index];
        }
    }

    /// <summary>Finds the definition of an attribute.</summary>
    /// <param name="name">The attribute name as written, with its prefix.</param>
    /// <param name="index">The index of the definition.</param>
    /// <returns><see langword="true"/> when the attribute is declared.</returns>
    internal bool TryFind(ReadOnlySpan<char> name, out int index)
    {
        if (_names.TryGet(name, out var symbol))
        {
            index = _indexBySymbol[symbol.Value];
            return true;
        }

        index = -1;
        return false;
    }

    /// <summary>Gets the default literal of a definition: the text as written, with CR LF and a lone CR as LF (section 2.11).</summary>
    /// <param name="index">The index of a definition in <see cref="Defaulted"/>.</param>
    /// <returns>The literal, empty for a definition without a default.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is not the index of a definition.</exception>
    internal string DefaultText(int index)
    {
        Argument.ThrowIfLessThan(index, 0);
        Argument.ThrowIfGreaterThanOrEqual(index, _definitions.Length);

        return _defaultTexts[index] ?? string.Empty;
    }

    private static string NormalizeLineEnds(string text)
    {
        var first = text.AsSpan().IndexOf('\r');
        if (first < 0)
        {
            return text;
        }

        var builder = new StringBuilder(text.Length);
        builder.Append(text, 0, first);
        for (var i = first; i < text.Length; i++)
        {
            if (text[i] != '\r')
            {
                builder.Append(text[i]);
                continue;
            }

            builder.Append('\n');
            if (i + 1 < text.Length && text[i + 1] == '\n')
            {
                i++;
            }
        }

        return builder.ToString();
    }
}
