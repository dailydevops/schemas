namespace NetEvolve.Schemas.XPath.Syntax.Lexer;

/// <summary>Why the lexer reported an <see cref="XPathTokenKind.Error"/> token.</summary>
internal enum XPathLexicalError
{
    /// <summary>No error.</summary>
    None,

    /// <summary>A string literal has no closing quote; the token starts at the opening quote.</summary>
    UnterminatedString,

    /// <summary>A comment has no closing delimiter; the token starts at the outermost unclosed opening delimiter.</summary>
    UnterminatedComment,

    /// <summary>A character that starts no terminal.</summary>
    IllegalCharacter,

    /// <summary>A numeric literal is immediately followed by a name character.</summary>
    InvalidNumber,
}
