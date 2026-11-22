namespace NetEvolve.Schemas.Schematron.Expansion.AbstractPatterns;

using System.Text;
using NetEvolve.Arguments;

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

    internal SubstitutionTable(string[] names, string[] values)
    {
        ArgumentNullException.ThrowIfNull(names);
        ArgumentNullException.ThrowIfNull(values);

        _names = names;
        _values = values;
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
        for (var i = from; i < text.Length; i++)
        {
            if (text[i] == '$')
            {
                return i;
            }
        }

        return -1;
    }

    private int Longest(string text, int start)
    {
        var best = -1;
        for (var i = 0; i < _names.Length; i++)
        {
            var name = _names[i];
            if (
                name.Length != 0
                && (best < 0 || name.Length > _names[best].Length)
                && string.CompareOrdinal(text, start, name, 0, name.Length) == 0
            )
            {
                best = i;
            }
        }

        return best;
    }
}
