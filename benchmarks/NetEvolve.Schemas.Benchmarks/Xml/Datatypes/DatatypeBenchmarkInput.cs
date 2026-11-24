namespace NetEvolve.Schemas.Benchmarks.Xml.Datatypes;

using System.Text;

/// <summary>
/// The lexical forms of the datatype benchmarks, in three sizes. Every form is inside the range of the matching .NET
/// type, so <c>System.Xml</c> parses the same text without an exception and the baseline does the same work.
/// </summary>
internal static class DatatypeBenchmarkInput
{
    public const string Short = "Short";
    public const string Typical = "Typical";
    public const string Long = "Long";

    /// <summary>A lexical form of <c>xs:decimal</c> within the range of <see cref="decimal"/>.</summary>
    public static string Decimal(string size) =>
        size switch
        {
            Short => "1.5",
            Typical => "12345.6789",
            _ => "-1234567890123456789.0123456789",
        };

    /// <summary>A lexical form of <c>xs:decimal</c> beyond the range of <see cref="decimal"/>; there is no baseline for it.</summary>
    public static string WideDecimal(string size) =>
        size switch
        {
            Short => "123456789012345678901234567890.5",
            Typical => "-" + new string('7', 60) + "." + new string('3', 20),
            _ => new string('9', 300) + "." + new string('1', 100),
        };

    /// <summary>A lexical form of a bounded integer (<c>xs:int</c>).</summary>
    public static string Integer(string size) =>
        size switch
        {
            Short => "7",
            Typical => "123456",
            _ => "-2147483648",
        };

    /// <summary>A lexical form of <c>xs:double</c>.</summary>
    public static string Double(string size) =>
        size switch
        {
            Short => "1",
            Typical => "3.14159",
            _ => "-1.7976931348623157E308",
        };

    /// <summary>A lexical form of <c>xs:dateTime</c>.</summary>
    public static string DateTime(string size) =>
        size switch
        {
            Short => "2024-01-01T00:00:00",
            Typical => "2024-05-17T13:45:30Z",
            _ => "2024-05-17T13:45:30.1234567+05:30",
        };

    /// <summary>A lexical form of <c>xs:duration</c>.</summary>
    public static string Duration(string size) =>
        size switch
        {
            Short => "P1D",
            Typical => "P1Y2M3DT4H5M6S",
            _ => "P12Y11M30DT23H59M59.1234567S",
        };

    /// <summary>A lexical form of <c>xs:base64Binary</c>: 4, 64, and 4096 characters.</summary>
    public static string Base64(string size) =>
        size switch
        {
            Short => "AAEC",
            Typical => Encode(48),
            _ => Encode(3072),
        };

    private static string Encode(int octets)
    {
        var bytes = new byte[octets];
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)(i * 31);
        }

        return Convert.ToBase64String(bytes);
    }
}
