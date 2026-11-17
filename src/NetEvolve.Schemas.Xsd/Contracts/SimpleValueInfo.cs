namespace NetEvolve.Schemas.Xsd.Contracts;

using System;

/// <summary>The input of <see cref="ValidationListener.SimpleValue"/>, valid only during the call.</summary>
internal readonly ref struct SimpleValueInfo
{
    internal SimpleValueInfo(
        int ordinal,
        ReadOnlySpan<char> value,
        IXsdComponent? type,
        int memberIndex,
        bool isValid,
        bool isDefaulted
    )
    {
        Ordinal = ordinal;
        Value = value;
        Type = type;
        MemberIndex = memberIndex;
        IsValid = isValid;
        IsDefaulted = isDefaulted;
    }

    /// <summary>Gets the ordinal of the element that holds the value.</summary>
    public int Ordinal { get; }

    /// <summary>Gets the value text.</summary>
    public ReadOnlySpan<char> Value { get; }

    /// <summary>Gets the simple type, <see langword="null"/> when none was found.</summary>
    public IXsdComponent? Type { get; }

    /// <summary>Gets the index of the accepting union member, <c>-1</c> when the type is no union.</summary>
    public int MemberIndex { get; }

    /// <summary>Gets a value indicating whether the value is valid.</summary>
    public bool IsValid { get; }

    /// <summary>Gets a value indicating whether the value comes from a default or fixed value.</summary>
    public bool IsDefaulted { get; }
}
