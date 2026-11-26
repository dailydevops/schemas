namespace NetEvolve.Schemas.XPath.Functions.Strings;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// <c>fn:translate</c> (Functions and Operators 1.0, 7.4.7): every code point of the argument that occurs in the map string is replaced
/// by the code point at the same position of the translate string, or removed when that string is shorter. The first occurrence in the
/// map string counts when a code point occurs twice. Positions count code points. A string that no map code point touches is returned as it is.
/// </summary>
/// <remarks>The work is the length of the argument times the length of the map string: a map string is short in practice, and the string length limit bounds both.</remarks>
internal sealed class TranslateFunction : TextFunction
{
    private const int InitialBuffer = 256;

    /// <summary>Initializes a new instance of the <see cref="TranslateFunction"/> class.</summary>
    public TranslateFunction()
        : base("translate", 3, 3, [OptionalString, OneString, OneString]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var text = Text(invocation.Arguments[0]);
        var map = Text(invocation.Arguments[1]);
        var translation = Text(invocation.Arguments[2]);
        Return(ref invocation, Translate(text, map, translation, invocation.Context.Limits));
        return XPathEvaluationStatus.Ok;
    }

    private static string Translate(string text, string map, string translation, XPathLimits limits)
    {
        if (text.Length == 0 || map.Length == 0)
        {
            return text;
        }

        var source = text.AsSpan();
        var mapSpan = map.AsSpan();
        var translationSpan = translation.AsSpan();
        Span<char> initial = stackalloc char[InitialBuffer];
        var builder = new TextBuilder(initial, limits);
        try
        {
            // Without a surrogate in the map and the translate string, every code unit is one code point and the position in the map
            // string is the position in the translate string; a surrogate in the argument is never in the map, so it is copied as it is.
            if (!HasSurrogate(mapSpan) && !HasSurrogate(translationSpan))
            {
                return TranslateUnits(source, mapSpan, translationSpan, text, limits, ref builder);
            }

            var changed = false;
            var index = 0;
            while (index < source.Length)
            {
                var codePoint = CodePoints.Next(source, ref index);
                if (TryFind(mapSpan, translationSpan, codePoint, out var replacement))
                {
                    changed = true;
                    if (replacement >= 0)
                    {
                        builder.AppendCodePoint(replacement);
                    }
                }
                else
                {
                    builder.AppendCodePoint(codePoint);
                }
            }

            return changed ? builder.ToString() : text;
        }
        finally
        {
            builder.Dispose();
        }
    }

    private static string TranslateUnits(
        ReadOnlySpan<char> source,
        ReadOnlySpan<char> map,
        ReadOnlySpan<char> translation,
        string text,
        XPathLimits limits,
        ref TextBuilder builder
    )
    {
        var changed = false;
        var runStart = 0;
        for (var i = 0; i < source.Length; i++)
        {
            var position = map.IndexOf(source[i]);
            if (position < 0)
            {
                continue;
            }

            changed = true;
            builder.Append(source.Slice(runStart, i - runStart));
            runStart = i + 1;
            if (position < translation.Length)
            {
                builder.Append(translation[position]);
            }
        }

        if (!changed)
        {
            // The general path builds the unchanged string against the limit before it returns it; keep that.
            limits.ThrowIfStringTooLong(source.Length, new SourceSpan(-1, 0));
            return text;
        }

        builder.Append(source.Slice(runStart));
        return builder.ToString();
    }

    private static bool HasSurrogate(ReadOnlySpan<char> text)
    {
        foreach (var unit in text)
        {
            if (char.IsSurrogate(unit))
            {
                return true;
            }
        }

        return false;
    }

    // Looks the code point up in the map string, reading the translate string in step: the replacement is the code point of the same
    // position, or -1 (remove) when the translate string is shorter. The first occurrence wins.
    private static bool TryFind(
        ReadOnlySpan<char> map,
        ReadOnlySpan<char> translation,
        int codePoint,
        out int replacement
    )
    {
        var mapIndex = 0;
        var translationIndex = 0;
        while (mapIndex < map.Length)
        {
            var mapped = CodePoints.Next(map, ref mapIndex);
            var target =
                translationIndex < translation.Length ? CodePoints.Next(translation, ref translationIndex) : -1;
            if (mapped == codePoint)
            {
                replacement = target;
                return true;
            }
        }

        replacement = -1;
        return false;
    }
}
