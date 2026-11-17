#pragma warning disable CA1815 // Approved shape (#123): the struct defines no value equality.

namespace NetEvolve.Schemas.XPath.Abstractions;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;

/// <summary>The groups captured by a regular expression match: the subject and one range per group; group zero is the whole match.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct XPathRegexGroups
{
    private readonly string? _subject;
    private readonly TextRange[]? _groups;

    /// <summary>Initializes a new instance of the <see cref="XPathRegexGroups"/> struct.</summary>
    /// <param name="subject">The text that was matched.</param>
    /// <param name="groups">The range of every group inside <paramref name="subject"/>; the array is copied.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    public XPathRegexGroups(string subject, TextRange[] groups)
    {
        Argument.ThrowIfNull(subject);
        Argument.ThrowIfNull(groups);

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
        Argument.ThrowIfLessThan(index, 0);
        Argument.ThrowIfGreaterThanOrEqual(index, Count);

        return _groups![index].Slice(Subject.AsSpan());
    }
}
