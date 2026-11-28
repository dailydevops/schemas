namespace NetEvolve.Schemas.Xml.Dtd.Contracts;

/// <summary>
/// The limits that DTD validation adds to the reader and core limits (design #122, section 9), each with one default and
/// one error code. A limit of zero never disables it; exceeding one is a fatal status, never an issue.
/// </summary>
/// <remarks>Every value must be at least one; <c>With*</c> methods reject zero and negative values.</remarks>
public sealed class DtdValidationLimits
{
    private int _maxExternalEntityRequests = 32;
    private int _maxContentModelStates = 65_536;
    private int _maxDeclarations = 100_000;

    private DtdValidationLimits() { }

    /// <summary>Gets the default limits.</summary>
    public static DtdValidationLimits Default { get; } = new();

    /// <summary>Gets the maximum number of resolver calls per document; the default is 32 and the error is <see cref="DtdLimitCodes.XML1117"/>.</summary>
    public int MaxExternalEntityRequests => _maxExternalEntityRequests;

    /// <summary>Gets the maximum number of compiled automaton positions per content model; the default is 65,536 and the error is <see cref="DtdLimitCodes.XML1118"/>.</summary>
    public int MaxContentModelStates => _maxContentModelStates;

    /// <summary>Gets the maximum number of element, attribute, entity, and notation declarations per document; the default is 100,000 and the error is <see cref="DtdLimitCodes.XML1119"/>.</summary>
    public int MaxDeclarations => _maxDeclarations;

    /// <summary>Returns a copy with another <see cref="MaxExternalEntityRequests"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public DtdValidationLimits WithMaxExternalEntityRequests(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);

        var copy = (DtdValidationLimits)MemberwiseClone();
        copy._maxExternalEntityRequests = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="MaxContentModelStates"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public DtdValidationLimits WithMaxContentModelStates(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);

        var copy = (DtdValidationLimits)MemberwiseClone();
        copy._maxContentModelStates = value;

        return copy;
    }

    /// <summary>Returns a copy with another <see cref="MaxDeclarations"/>.</summary>
    /// <param name="value">The new value.</param>
    /// <returns>The new limits.</returns>
    public DtdValidationLimits WithMaxDeclarations(int value)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);

        var copy = (DtdValidationLimits)MemberwiseClone();
        copy._maxDeclarations = value;

        return copy;
    }
}
