namespace NetEvolve.Schemas.XPath.Errors;

/// <summary>The class of an XPath error, derived from the prefix of its code.</summary>
public enum XPathErrorKind
{
    /// <summary>A static error (<c>XPST</c>), raised during static analysis.</summary>
    Static = 0,

    /// <summary>A type error (<c>XPTY</c>), raised during static analysis or dynamic evaluation.</summary>
    Type = 1,

    /// <summary>A dynamic error (<c>XPDY</c>), raised during dynamic evaluation.</summary>
    Dynamic = 2,

    /// <summary>A Functions and Operators error (<c>FO</c> prefix), raised by a function or operator.</summary>
    Function = 3,

    /// <summary>An engine condition the specifications do not name, outside the standard error namespace.</summary>
    Engine = 4,
}
