namespace NetEvolve.Schemas.XPath.Functions.Strings;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Limits;

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
