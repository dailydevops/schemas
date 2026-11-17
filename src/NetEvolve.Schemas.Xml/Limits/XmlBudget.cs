namespace NetEvolve.Schemas.Xml.Limits;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>One budget for one resource that hostile input can inflate; the reader and the entity stack charge it.</summary>
/// <remarks>A mutable value type: copy it only deliberately. A refused charge leaves <see cref="Used"/> unchanged; the caller reports <see cref="Code"/>.</remarks>
internal struct XmlBudget
{
    private long _used;

    internal XmlBudget(long limit, XmlErrorCode code)
    {
        Limit = limit;
        Code = code;
        _used = 0;
    }

    /// <summary>Gets the most that may be charged.</summary>
    internal readonly long Limit { get; }

    /// <summary>Gets the amount charged so far.</summary>
    internal readonly long Used => _used;

    /// <summary>Gets the diagnostic code to report on overrun.</summary>
    internal readonly XmlErrorCode Code { get; }

    /// <summary>Charges <paramref name="amount"/> (not negative); returns <see langword="false"/> when it would exceed the limit.</summary>
    internal bool TryCharge(long amount)
    {
        if (amount > Limit - _used)
        {
            return false;
        }

        _used += amount;
        return true;
    }

    /// <summary>Gives back <paramref name="amount"/> previously charged, for budgets that track a level such as a depth.</summary>
    internal void Release(long amount) => _used -= amount;

    /// <summary>Gives back everything, for per-item budgets such as a name length.</summary>
    internal void Reset() => _used = 0;

    internal static XmlBudget ElementDepth(XmlReaderLimits limits) =>
        new(limits.MaxElementDepth, XmlErrorCodes.XML1102);

    internal static XmlBudget NameLength(XmlReaderLimits limits) => new(limits.MaxNameLength, XmlErrorCodes.XML1105);

    internal static XmlBudget AttributeCount(XmlReaderLimits limits) =>
        new(limits.MaxAttributesPerElement, XmlErrorCodes.XML1103);

    internal static XmlBudget TextLength(XmlReaderLimits limits) =>
        new(limits.MaxTextNodeLength, XmlErrorCodes.XML1107);

    internal static XmlBudget DocumentCharacters(XmlReaderLimits limits) =>
        new(limits.MaxDocumentCharacters, XmlErrorCodes.XML1101);

    internal static XmlBudget ExpansionCharacters(XmlReaderLimits limits) =>
        new(limits.MaxEntityExpansionCharacters, XmlErrorCodes.XML1109);

    internal static XmlBudget EntityDepth(XmlReaderLimits limits) => new(limits.MaxEntityDepth, XmlErrorCodes.XML1110);
}
