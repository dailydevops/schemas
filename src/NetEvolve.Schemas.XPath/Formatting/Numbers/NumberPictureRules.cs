namespace NetEvolve.Schemas.XPath.Formatting.Numbers;

/// <summary>The rule set that reads a picture string of <c>format-number</c>.</summary>
internal enum NumberPictureRules
{
    /// <summary>XSLT 2.0, 16.4: the zero digit is the only digit sign, no exponent separator, and the original sub-picture rules.</summary>
    Xslt20 = 0,

    /// <summary>Functions and Operators 3.1, 4.7: every digit of the family is a digit sign, the exponent separator, and the changed sub-picture rules.</summary>
    Xpath31 = 1,
}
