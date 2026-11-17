namespace NetEvolve.Schemas.Xml.Datatypes.Decimals.Arithmetic;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>The outcome of a fallible numeric operation: a value, or a coded error. Invalid input never throws.</summary>
/// <typeparam name="TValue">The value type, always a non-ref struct.</typeparam>
[StructLayout(LayoutKind.Auto)]
public readonly record struct ArithmeticResult<TValue>
{
    /// <summary>Initializes a successful result.</summary>
    /// <param name="value">The value.</param>
    public ArithmeticResult(TValue value)
    {
        Success = true;
        Value = value;
        Error = DatatypeError.None;
    }

    /// <summary>Initializes a failed result.</summary>
    /// <param name="error">The error code.</param>
    public ArithmeticResult(DatatypeError error)
    {
        Success = false;
        Value = default!;
        Error = error;
    }

    /// <summary>Gets a value indicating whether the operation succeeded.</summary>
    public bool Success { get; }

    /// <summary>Gets the value, <see langword="default"/> unless <see cref="Success"/>.</summary>
    public TValue Value { get; }

    /// <summary>Gets the error code, <see cref="DatatypeError.None"/> when <see cref="Success"/>.</summary>
    public DatatypeError Error { get; }
}
