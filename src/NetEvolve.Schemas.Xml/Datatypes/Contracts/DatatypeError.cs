namespace NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>The stable datatype error codes; the value is the number of the <c>DTnnnn</c> code.</summary>
/// <remarks>
/// Codes are added, never renumbered. <c>DT0001</c> to <c>DT0009</c> are assigned by the packages that implement the lexical
/// scanners (DT-02, DT-03); <c>DT0012</c> is reserved for the fraction cap, which is not part of the approved design.
/// </remarks>
public enum DatatypeError
{
    /// <summary>No error.</summary>
    None = 0,

    /// <summary><c>DT0001</c>: the input is not a well-formed lexical form of the type, for example it is empty, has no digit, or has a second decimal point.</summary>
    InvalidLexicalForm = 1,

    /// <summary><c>DT0002</c>: the value violates a constraining facet; the facet is reported separately.</summary>
    FacetViolation = 2,

    /// <summary><c>DT0003</c>: an item of a list value is invalid; the item index is reported separately.</summary>
    InvalidListItem = 3,

    /// <summary><c>DT0004</c>: no member type of a union accepts the lexical form.</summary>
    NoUnionMember = 4,

    /// <summary><c>DT0010</c>: a lexical number exceeds the digit cap.</summary>
    DigitCap = 10,

    /// <summary><c>DT0011</c>: a year or duration component exceeds the 17-digit cap.</summary>
    YearOrComponentCap = 11,

    /// <summary><c>DT0013</c>: the input contains a character that the lexical space does not allow.</summary>
    InvalidCharacter = 13,
}
