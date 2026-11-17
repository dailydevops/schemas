namespace NetEvolve.Schemas.Xml.Datatypes.Contracts;

using System.Runtime.InteropServices;
using NetEvolve.Arguments;

/// <summary>The outcome of parsing a lexical form: a value, or a coded error with the offset where it was found. Invalid input never throws.</summary>
/// <typeparam name="TValue">The value type, always a non-ref struct.</typeparam>
[StructLayout(LayoutKind.Auto)]
public readonly record struct ParseResult<TValue>
{
    /// <summary>Initializes a successful result.</summary>
    /// <param name="value">The parsed value.</param>
    public ParseResult(TValue value)
    {
        Success = true;
        Value = value;
        Error = DatatypeError.None;
        ErrorOffset = 0;
    }

    /// <summary>Initializes a failed result.</summary>
    /// <param name="error">The error.</param>
    /// <param name="errorOffset">The zero-based offset of the error in the lexical form.</param>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="errorOffset"/> is negative.</exception>
    public ParseResult(DatatypeError error, int errorOffset)
    {
        Argument.ThrowIfLessThan(errorOffset, 0);
        Success = false;
        Value = default!;
        Error = error;
        ErrorOffset = errorOffset;
    }

    /// <summary>Gets a value indicating whether the lexical form is valid.</summary>
    public bool Success { get; }

    /// <summary>Gets the value, <see langword="default"/> unless <see cref="Success"/>.</summary>
    public TValue Value { get; }

    /// <summary>Gets the error, <see cref="DatatypeError.None"/> when <see cref="Success"/>.</summary>
    public DatatypeError Error { get; }

    /// <summary>Gets the zero-based offset of the error in the lexical form, zero when <see cref="Success"/>.</summary>
    public int ErrorOffset { get; }
}
