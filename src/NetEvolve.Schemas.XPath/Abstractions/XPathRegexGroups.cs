namespace NetEvolve.Schemas.XPath.Abstractions;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>The groups captured by a regular expression match: the subject and one range per group; group zero is the whole match.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct XPathRegexGroups : IEquatable<XPathRegexGroups>
{
    private readonly string? _subject;
    private readonly TextRange[]? _groups;

    /// <summary>Initializes a new instance of the <see cref="XPathRegexGroups"/> struct.</summary>
    /// <param name="subject">The text that was matched.</param>
    /// <param name="groups">The range of every group inside <paramref name="subject"/>; the array is copied.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public XPathRegexGroups(string subject, TextRange[] groups)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(groups);

        _subject = subject;
        _groups = (TextRange[])groups.Clone();
    }

    /// <summary>Gets the text that was matched, empty for <see langword="default"/>.</summary>
    public string Subject => _subject ?? string.Empty;

    /// <summary>Gets the number of groups.</summary>
    public int Count => _groups?.Length ?? 0;

    /// <summary>Gets the text of a group.</summary>
    /// <param name="index">The zero-based group number.</param>
    /// <returns>A view of the group inside <see cref="Subject"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the groups.</exception>
    public ReadOnlySpan<char> GetGroup(int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);

        return _groups![index].Slice(Subject.AsSpan());
    }

    /// <inheritdoc />
    public bool Equals(XPathRegexGroups other) =>
        string.Equals(Subject, other.Subject, StringComparison.Ordinal) && GroupsEqual(other);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XPathRegexGroups other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => unchecked((StringComparer.Ordinal.GetHashCode(Subject) * 31) + Count);

    /// <summary>Determines whether two group sets are equal.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(XPathRegexGroups left, XPathRegexGroups right) => left.Equals(right);

    /// <summary>Determines whether two group sets differ.</summary>
    /// <param name="left">The first value.</param>
    /// <param name="right">The second value.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(XPathRegexGroups left, XPathRegexGroups right) => !left.Equals(right);

    private bool GroupsEqual(XPathRegexGroups other)
    {
        for (var i = 0; i < Count; i++)
        {
            if (Count != other.Count || _groups![i] != other._groups![i])
            {
                return false;
            }
        }

        return Count == other.Count;
    }
}
