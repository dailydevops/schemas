namespace NetEvolve.Schemas.XPath.Casting.Core;

/// <summary>The outcome of a cast. A failed cast is a status, not an exception, so that <c>castable as</c> never throws.</summary>
internal enum CastStatus
{
    /// <summary>The cast succeeded.</summary>
    Ok = 0,

    /// <summary>The source type has no row for the target type (XPTY0004).</summary>
    NotAllowed = 1,

    /// <summary>The lexical form is not in the lexical space of the target type (FORG0001).</summary>
    InvalidLexical = 2,

    /// <summary>The value has no counterpart in the target type, such as NaN or an infinity as a decimal or an integer (FOCA0002).</summary>
    InvalidValue = 3,

    /// <summary>The value is beyond an implementation limit of the decimal (FOCA0001).</summary>
    DecimalOverflow = 4,

    /// <summary>The value does not fit the 64 bits of the integer item, or a lexical integer exceeds the digit cap (FOCA0003).</summary>
    IntegerOverflow = 5,

    /// <summary>The lexical form is valid, but a year or a fraction is beyond an implementation limit of the date and time types (FODT0001).</summary>
    DateTimeOverflow = 6,

    /// <summary>The lexical form is valid, but a component or a fraction is beyond an implementation limit of the duration types (FODT0002).</summary>
    DurationOverflow = 7,

    /// <summary>The prefix of a lexical QName is not bound in the namespaces of the expression (FONS0004).</summary>
    UnboundPrefix = 8,
}
