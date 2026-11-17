namespace NetEvolve.Schemas.Schematron.Patterns;

/// <summary>Converts offsets in a pattern text to locations.</summary>
/// <param name="text">The pattern text.</param>
/// <param name="baseUri">The base URI of the schema the pattern is from, reported as the source.</param>
internal sealed class PatternLocator(string text, string? baseUri)
{
    /// <summary>Converts an offset to a one-based line and column.</summary>
    /// <param name="offset">The zero-based offset, negative for unknown.</param>
    /// <returns>The location.</returns>
    internal DocumentLocation Locate(int offset)
    {
        var line = 1;
        var column = 1;
        for (var i = 0; i < offset && i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }

        return new DocumentLocation(baseUri, line, column);
    }
}
