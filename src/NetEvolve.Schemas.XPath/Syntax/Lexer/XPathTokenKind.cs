namespace NetEvolve.Schemas.XPath.Syntax.Lexer;

/// <summary>The terminal symbols of XPath 2.0 (section A.2) as the lexer reports them.</summary>
/// <remarks>
/// Limitation: terminals added by XPath 3.0 and 3.1 are not part of the enumeration.
/// Reserved words are reported as keywords only where the grammar allows them, otherwise as <see cref="Name"/>.
/// </remarks>
internal enum XPathTokenKind
{
    /// <summary>The end of the input.</summary>
    EndOfInput,

    /// <summary>A lexical error; see <see cref="XPathToken.Error"/>. The lexer reports the end of the input afterwards.</summary>
    Error,

    /// <summary>IntegerLiteral.</summary>
    IntegerLiteral,

    /// <summary>DecimalLiteral.</summary>
    DecimalLiteral,

    /// <summary>DoubleLiteral.</summary>
    DoubleLiteral,

    /// <summary>StringLiteral, including the quotes and any doubled quote escapes.</summary>
    StringLiteral,

    /// <summary>NCName.</summary>
    Name,

    /// <summary>QName with a prefix.</summary>
    QName,

    /// <summary>The wildcard <c>*</c> in a name test.</summary>
    Wildcard,

    /// <summary>The wildcard <c>prefix:*</c>.</summary>
    PrefixWildcard,

    /// <summary>The wildcard <c>*:local</c>.</summary>
    LocalWildcard,

    /// <summary>The multiplication operator or an occurrence indicator <c>*</c>.</summary>
    Star,

    /// <summary><c>,</c>.</summary>
    Comma,

    /// <summary><c>(</c>.</summary>
    LeftParen,

    /// <summary><c>)</c>.</summary>
    RightParen,

    /// <summary><c>[</c>.</summary>
    LeftBracket,

    /// <summary><c>]</c>.</summary>
    RightBracket,

    /// <summary><c>.</c>.</summary>
    Dot,

    /// <summary><c>..</c>.</summary>
    DotDot,

    /// <summary><c>/</c>.</summary>
    Slash,

    /// <summary><c>//</c>.</summary>
    SlashSlash,

    /// <summary><c>@</c>.</summary>
    At,

    /// <summary><c>|</c>.</summary>
    Pipe,

    /// <summary>The addition operator or an occurrence indicator <c>+</c>.</summary>
    Plus,

    /// <summary><c>-</c>.</summary>
    Minus,

    /// <summary><c>=</c>.</summary>
    Equal,

    /// <summary><c>!=</c>.</summary>
    NotEqual,

    /// <summary><c>&lt;</c>.</summary>
    Less,

    /// <summary><c>&lt;=</c>.</summary>
    LessOrEqual,

    /// <summary><c>&gt;</c>.</summary>
    Greater,

    /// <summary><c>&gt;=</c>.</summary>
    GreaterOrEqual,

    /// <summary><c>&lt;&lt;</c>.</summary>
    Precedes,

    /// <summary><c>&gt;&gt;</c>.</summary>
    Follows,

    /// <summary><c>$</c>.</summary>
    Dollar,

    /// <summary><c>::</c>.</summary>
    ColonColon,

    /// <summary>The occurrence indicator <c>?</c>.</summary>
    Question,

    /// <summary>Keyword <see langword="and"/>.</summary>
    And,

    /// <summary>Keyword <see langword="or"/>.</summary>
    Or,

    /// <summary>Keyword <c>div</c>.</summary>
    Div,

    /// <summary>Keyword <c>idiv</c>.</summary>
    IDiv,

    /// <summary>Keyword <c>mod</c>.</summary>
    Mod,

    /// <summary>Keyword <c>union</c>.</summary>
    Union,

    /// <summary>Keyword <c>intersect</c>.</summary>
    Intersect,

    /// <summary>Keyword <c>except</c>.</summary>
    Except,

    /// <summary>Keyword <c>to</c>.</summary>
    To,

    /// <summary>Keyword <see langword="for"/>.</summary>
    For,

    /// <summary>Keyword <see langword="in"/>.</summary>
    In,

    /// <summary>Keyword <see langword="return"/>.</summary>
    Return,

    /// <summary>Keyword <c>some</c>.</summary>
    Some,

    /// <summary>Keyword <c>every</c>.</summary>
    Every,

    /// <summary>Keyword <c>satisfies</c>.</summary>
    Satisfies,

    /// <summary>Keyword <see langword="if"/>.</summary>
    If,

    /// <summary>Keyword <c>then</c>.</summary>
    Then,

    /// <summary>Keyword <see langword="else"/>.</summary>
    Else,

    /// <summary>Keyword <c>instance</c>.</summary>
    Instance,

    /// <summary>Keyword <c>treat</c>.</summary>
    Treat,

    /// <summary>Keyword <c>castable</c>.</summary>
    Castable,

    /// <summary>Keyword <c>cast</c>.</summary>
    Cast,

    /// <summary>Keyword <c>of</c> after <c>instance</c>.</summary>
    Of,

    /// <summary>Keyword <see langword="as"/> after <c>treat</c>, <c>cast</c>, or <c>castable</c>.</summary>
    As,

    /// <summary>Keyword <c>eq</c>.</summary>
    Eq,

    /// <summary>Keyword <c>ne</c>.</summary>
    Ne,

    /// <summary>Keyword <c>lt</c>.</summary>
    Lt,

    /// <summary>Keyword <c>le</c>.</summary>
    Le,

    /// <summary>Keyword <c>gt</c>.</summary>
    Gt,

    /// <summary>Keyword <c>ge</c>.</summary>
    Ge,

    /// <summary>Keyword <see langword="is"/>.</summary>
    Is,
}
