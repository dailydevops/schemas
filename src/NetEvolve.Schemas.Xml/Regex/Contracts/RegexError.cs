namespace NetEvolve.Schemas.Xml.Regex.Contracts;

/// <summary>The stable regex pattern error codes; the value is the number of the <c>RXnnnn</c> code.</summary>
/// <remarks>Codes are added, never renumbered. A step ceiling is not an error code: it is <see cref="RegexMatchResult.Aborted"/>.</remarks>
public enum RegexError
{
    /// <summary>No error.</summary>
    None = 0,

    /// <summary><c>RX0001</c>: unexpected end of the pattern.</summary>
    UnexpectedEnd = 1,

    /// <summary><c>RX0002</c>: unbalanced parenthesis or bracket.</summary>
    Unbalanced = 2,

    /// <summary><c>RX0003</c>: invalid quantifier or range, for example <c>{n,m}</c> with n greater than m.</summary>
    InvalidQuantifier = 3,

    /// <summary><c>RX0004</c>: invalid escape.</summary>
    InvalidEscape = 4,

    /// <summary><c>RX0005</c>: unknown category or block name.</summary>
    UnknownCategoryOrBlock = 5,

    /// <summary><c>RX0006</c>: invalid character class or subtraction.</summary>
    InvalidCharacterClass = 6,

    /// <summary><c>RX0007</c>: construct not allowed in this dialect.</summary>
    ConstructNotAllowed = 7,

    /// <summary><c>RX0008</c>: invalid back reference.</summary>
    InvalidBackReference = 8,

    /// <summary><c>RX0009</c>: pattern length, nesting, or program size limit exceeded.</summary>
    LimitExceeded = 9,

    /// <summary><c>RX0010</c>: invalid or unsupported flag.</summary>
    InvalidFlag = 10,

    /// <summary><c>RX0011</c>: invalid UTF-16 in the pattern.</summary>
    InvalidUtf16 = 11,
}
