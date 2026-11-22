namespace NetEvolve.Schemas.Xsd.TypeAlternatives;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// One attribute of the element that a type alternative test sees: its expanded name, where its value lies in a shared character
/// buffer, and whether its declaration is <c>inheritable</c>.
/// </summary>
/// <remarks>The value is a range of a buffer that the caller owns, so the validator hands in what it already copied (design #146, DP-11).</remarks>
[StructLayout(LayoutKind.Auto)]
internal readonly struct TypeAlternativeAttribute
{
    /// <summary>Initializes a new instance of the <see cref="TypeAlternativeAttribute"/> struct.</summary>
    /// <param name="name">The expanded name.</param>
    /// <param name="start">The index of the first character of the value in the buffer.</param>
    /// <param name="length">The number of characters of the value.</param>
    /// <param name="isInheritable">Whether the attribute declaration is <c>inheritable</c>.</param>
    internal TypeAlternativeAttribute(XPathQName name, int start, int length, bool isInheritable)
    {
        Name = name;
        Start = start;
        Length = length;
        IsInheritable = isInheritable;
    }

    /// <summary>Gets the expanded name.</summary>
    internal XPathQName Name { get; }

    /// <summary>Gets the index of the first character of the value in the buffer.</summary>
    internal int Start { get; }

    /// <summary>Gets the number of characters of the value.</summary>
    internal int Length { get; }

    /// <summary>Gets a value indicating whether the attribute declaration is <c>inheritable</c>.</summary>
    internal bool IsInheritable { get; }
}
