namespace NetEvolve.Schemas.Xsd.Contracts;

using System.Runtime.InteropServices;

/// <summary>The input of <see cref="ValidationListener.EndElement"/>, delivered once the content validity is known.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct ElementEndInfo
{
    internal ElementEndInfo(int ordinal, bool isValid, bool isNilled, IXsdComponent? type)
    {
        Ordinal = ordinal;
        IsValid = isValid;
        IsNilled = isNilled;
        Type = type;
    }

    /// <summary>Gets the ordinal of the element start event.</summary>
    public int Ordinal { get; }

    /// <summary>Gets a value indicating whether the element and its content are valid.</summary>
    public bool IsValid { get; }

    /// <summary>Gets a value indicating whether the element is nilled.</summary>
    public bool IsNilled { get; }

    /// <summary>Gets the type of the element, <see langword="null"/> when none was found.</summary>
    public IXsdComponent? Type { get; }
}
