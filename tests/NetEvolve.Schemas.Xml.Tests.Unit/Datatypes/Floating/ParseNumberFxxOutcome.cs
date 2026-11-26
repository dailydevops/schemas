namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Floating;

/// <summary>The result of checking one line of the parse-number-fxx test data against the <c>xs:double</c> and <c>xs:float</c> parsers.</summary>
internal enum ParseNumberFxxOutcome
{
    /// <summary>The input is in the lexical space and both parsed values carry the expected bits.</summary>
    Passed,

    /// <summary>The input is outside the lexical space and both parsers reject it.</summary>
    Rejected,

    /// <summary>A parsed value differs, an input in the lexical space is rejected, or an input outside it is accepted.</summary>
    Mismatch,

    /// <summary>The line does not have the layout of the data files.</summary>
    Malformed,
}
