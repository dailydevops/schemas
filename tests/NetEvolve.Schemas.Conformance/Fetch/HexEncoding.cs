namespace NetEvolve.Schemas.Conformance.Fetch;

/// <summary>Hexadecimal encoding that is identical on every supported target framework.</summary>
public static class HexEncoding
{
    private const string Digits = "0123456789abcdef";

    /// <summary>Encodes bytes as lowercase hexadecimal; <c>Convert.ToHexStringLower</c> needs .NET 9.</summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The lowercase hexadecimal string.</returns>
    public static string ToLower(ReadOnlySpan<byte> bytes)
    {
        var chars = new char[bytes.Length * 2];
        for (var i = 0; i < bytes.Length; i++)
        {
            chars[i * 2] = Digits[bytes[i] >> 4];
            chars[(i * 2) + 1] = Digits[bytes[i] & 0xF];
        }

        return new string(chars);
    }
}
