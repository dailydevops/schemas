namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf.Output;

/// <summary>Compares the bytes of the writer output with the bytes of the canonical output file of a case.</summary>
internal static class XmlConfOutputComparer
{
    /// <summary>Compares two byte sequences.</summary>
    /// <param name="expected">The bytes of the output file of the suite.</param>
    /// <param name="actual">The bytes the writer produced.</param>
    /// <returns><see langword="null"/> when the bytes are equal; otherwise a message with the first differing offset.</returns>
    public static string? Compare(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual)
    {
        var offset = expected.CommonPrefixLength(actual);
        if (offset == expected.Length && offset == actual.Length)
        {
            return null;
        }

        var prefix = $"The output differs at offset {offset}: ";
        return offset == actual.Length ? prefix + $"the actual output ends, expected 0x{expected[offset]:X2}."
            : offset == expected.Length ? prefix + $"the expected output ends, actual 0x{actual[offset]:X2}."
            : prefix + $"expected 0x{expected[offset]:X2} but was 0x{actual[offset]:X2}.";
    }
}
