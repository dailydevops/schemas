namespace NetEvolve.Schemas.Schematron.Expansion.AbstractPatterns;

using System.Text;

/// <summary>
/// The parameters of one <c>is-a</c> instance and their values, applied to expression text by textual substitution.
/// </summary>
/// <remarks>
/// At every dollar sign the value of the longest parameter name that follows it replaces the reference, so <c>$ab</c> is never
/// read as <c>$a</c> followed by <c>b</c> when both parameters exist (longest name first). A single pass does the work, so a
/// replaced value is not scanned again. Limitation: the match is textual and has no name boundary, so <c>$abc</c> with only the
/// parameter <c>a</c> becomes the value of <c>a</c> followed by <c>bc</c>.
/// </remarks>
internal sealed class SubstitutionTable
{
    private readonly string[] _names;
    private readonly string[] _values;
    private readonly Dictionary<string, int> _indexes = new(StringComparer.Ordinal);
    private readonly int[] _lengths;
#if NET9_0_OR_GREATER
    private readonly Dictionary<string, int>.AlternateLookup<ReadOnlySpan<char>> _lookup;
#endif

    internal SubstitutionTable(string[] names, string[] values)
    {
        ArgumentNullException.ThrowIfNull(names);
        ArgumentNullException.ThrowIfNull(values);

        _names = names;
        _values = values;

        // The first index of a name wins and empty names never match, as in the former linear scan.
        for (var i = 0; i < names.Length; i++)
        {
            if (names[i].Length != 0 && !_indexes.ContainsKey(names[i]))
            {
                _indexes.Add(names[i], i);
            }
        }

        var lengths = new HashSet<int>();
        foreach (var name in _indexes.Keys)
        {
            _ = lengths.Add(name.Length);
        }

        _lengths = new int[lengths.Count];
        lengths.CopyTo(_lengths);
        Array.Sort(_lengths);
        Array.Reverse(_lengths);
#if NET9_0_OR_GREATER
        _lookup = _indexes.GetAlternateLookup<ReadOnlySpan<char>>();
#endif
    }

    /// <summary>Gets the text with every known parameter reference replaced, or the same string when none occurs.</summary>
    internal string Apply(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        StringBuilder? result = null;
        var copied = 0;
        var at = Next(text, 0);
        while (at >= 0)
        {
            var best = Longest(text, at + 1);
            if (best < 0)
            {
                at = Next(text, at + 1);
                continue;
            }

            result ??= new StringBuilder(text.Length);
            _ = result.Append(text, copied, at - copied).Append(_values[best]);
            copied = at + 1 + _names[best].Length;
            at = Next(text, copied);
        }

        return result is null ? text : result.Append(text, copied, text.Length - copied).ToString();
    }

    private static int Next(string text, int from)
    {
        var offset = text.AsSpan(from).IndexOf('$');
        return offset < 0 ? -1 : from + offset;
    }

    private int Longest(string text, int start)
    {
        var remaining = text.Length - start;
        foreach (var length in _lengths)
        {
            if (length > remaining)
            {
                continue;
            }

#if NET9_0_OR_GREATER
            if (_lookup.TryGetValue(text.AsSpan(start, length), out var index))
#else
            if (_indexes.TryGetValue(text.Substring(start, length), out var index))
#endif
            {
                return index;
            }
        }

        return -1;
    }
}
