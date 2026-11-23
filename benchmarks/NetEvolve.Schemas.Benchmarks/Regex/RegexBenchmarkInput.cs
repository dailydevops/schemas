namespace NetEvolve.Schemas.Benchmarks.Regex;

/// <summary>
/// The pattern set of the regex benchmarks: pattern facets typical for e-invoicing rule sets and schemas (identifiers,
/// codes, dates, amounts), each with one input that the pattern accepts. Both engines see the same patterns and inputs.
/// Limitation: only accepted inputs are measured, because <c>System.Xml</c> reports a rejection by throwing an exception.
/// </summary>
internal static class RegexBenchmarkInput
{
    /// <summary>Gets the patterns, in XSD syntax.</summary>
    public static string[] Patterns { get; } =
    [
        "INV-[0-9]{4}-[0-9]{6}", // invoice number
        "[A-Z]{2}[0-9]{2}[A-Z0-9]{1,30}", // IBAN
        "[A-Z]{2}[0-9A-Za-z+*.]{2,12}", // VAT identification number
        "[A-Z]{3}", // ISO 4217 currency code
        "[A-Z]{2}", // ISO 3166 country code
        "[0-9]{1,3}", // code list value
        "[0-9]{4}-(0[1-9]|1[0-2])-(0[1-9]|[12][0-9]|3[01])", // date
        "-?[0-9]{1,12}(\\.[0-9]{1,2})?", // amount
        "[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}", // UUID
        "\\S+@\\S+\\.\\S+", // e-mail address
    ];

    /// <summary>Gets the inputs; index <c>i</c> is accepted by pattern <c>i</c>.</summary>
    public static string[] Inputs { get; } =
    [
        "INV-2026-000123",
        "DE89370400440532013000",
        "DE123456789",
        "EUR",
        "DE",
        "380",
        "2026-11-22",
        "-1234567.89",
        "123e4567-e89b-12d3-a456-426614174000",
        "billing@example.org",
    ];
}
