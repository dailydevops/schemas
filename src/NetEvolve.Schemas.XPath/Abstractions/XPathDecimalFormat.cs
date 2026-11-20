namespace NetEvolve.Schemas.XPath.Abstractions;

/// <summary>A decimal format as <c>fn:format-number</c> reads it; the defaults are those of the specification.</summary>
public sealed class XPathDecimalFormat
{
    /// <summary>Gets or sets the decimal separator.</summary>
    public char DecimalSeparator { get; set; } = '.';

    /// <summary>Gets or sets the grouping separator.</summary>
    public char GroupingSeparator { get; set; } = ',';

    /// <summary>Gets or sets the exponent separator of Functions and Operators 3.1 (4.7.1); the XSLT 2.0 rules of <c>format-number</c> do not read it.</summary>
    public char ExponentSeparator { get; set; } = 'e';

    /// <summary>Gets or sets the minus sign.</summary>
    public char MinusSign { get; set; } = '-';

    /// <summary>Gets or sets the percent sign.</summary>
    public char Percent { get; set; } = '%';

    /// <summary>Gets or sets the per-mille sign.</summary>
    public char PerMille { get; set; } = '‰';

    /// <summary>Gets or sets the zero digit.</summary>
    public char ZeroDigit { get; set; } = '0';

    /// <summary>Gets or sets the digit placeholder of a picture.</summary>
    public char Digit { get; set; } = '#';

    /// <summary>Gets or sets the separator of the positive and negative sub-pictures.</summary>
    public char PatternSeparator { get; set; } = ';';

    /// <summary>Gets or sets the text of positive infinity.</summary>
    public string Infinity { get; set; } = "Infinity";

    /// <summary>Gets or sets the text of not-a-number.</summary>
    public string NaN { get; set; } = "NaN";
}
