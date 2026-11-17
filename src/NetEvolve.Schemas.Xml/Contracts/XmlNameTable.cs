namespace NetEvolve.Schemas.Xml.Contracts;

using System;

/// <summary>
/// Interns the prefixes, local names, and namespace URIs of one event stream and resolves <see cref="XmlSymbol"/> handles
/// back to strings. The sink owns the table and exposes it as <see cref="IXmlSink.Names"/>.
/// </summary>
/// <remarks>
/// A library-owned table belongs to one document and freezes when its tree builder completes; a caller-supplied table can
/// be shared across documents and is never frozen by the library. <see cref="TryIntern"/> never throws; only the
/// convenience <c>Intern</c> members throw, for programming errors. The sealed implementation ships with XML-51.
/// </remarks>
public abstract class XmlNameTable
{
    /// <summary>Initializes a new instance of the <see cref="XmlNameTable"/> class.</summary>
    protected XmlNameTable() { }

    /// <summary>Gets the number of interned strings, including the empty string.</summary>
    public abstract int Count { get; }

    /// <summary>Gets a value indicating whether the table is frozen: no new string is accepted, lookups stay allowed.</summary>
    public abstract bool IsFrozen { get; }

    /// <summary>Interns a string. The empty string is always <see langword="default"/>(<see cref="XmlSymbol"/>).</summary>
    /// <param name="text">The characters of the string.</param>
    /// <param name="symbol">The handle, <see langword="default"/> unless the status is <see cref="XmlNameTableStatus.Ok"/>.</param>
    /// <returns>The status: <see cref="XmlNameTableStatus.LimitExceeded"/> maps to <c>XML1111</c>, <see cref="XmlNameTableStatus.Frozen"/> to <c>XML1116</c>.</returns>
    public abstract XmlNameTableStatus TryIntern(ReadOnlySpan<char> text, out XmlSymbol symbol);

    /// <summary>Looks a string up without interning it.</summary>
    /// <param name="text">The characters of the string.</param>
    /// <param name="symbol">The handle when found.</param>
    /// <returns><see langword="true"/> when the table contains the string.</returns>
    public abstract bool TryGet(ReadOnlySpan<char> text, out XmlSymbol symbol);

    /// <summary>Gets the string of a handle.</summary>
    /// <param name="symbol">A handle issued by this table.</param>
    /// <returns>The interned string.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="symbol"/> was not issued by this table.</exception>
    public abstract string GetString(XmlSymbol symbol);

    /// <summary>Gets the string of a handle without throwing.</summary>
    /// <param name="symbol">A handle.</param>
    /// <param name="text">The interned string when the table issued the handle.</param>
    /// <returns><see langword="true"/> when the table issued the handle.</returns>
    public abstract bool TryGetString(XmlSymbol symbol, out string? text);

    /// <summary>Freezes the table: no new string is accepted afterwards, lookups stay allowed.</summary>
    public abstract void Freeze();

    /// <summary>Returns the interned string for a span without interning and without allocating.</summary>
    /// <param name="text">The characters of the string.</param>
    /// <returns>The interned string, or <see langword="null"/> when the table does not contain it.</returns>
    public string? Lookup(ReadOnlySpan<char> text) =>
        TryGet(text, out var symbol) && TryGetString(symbol, out var value) ? value : null;

    /// <summary>Interns a string and throws when that is not possible; for code that builds events by hand.</summary>
    /// <param name="text">The characters of the string.</param>
    /// <returns>The handle.</returns>
    /// <exception cref="InvalidOperationException">The table is frozen and lacks the string, or is full.</exception>
    public XmlSymbol Intern(ReadOnlySpan<char> text)
    {
        var status = TryIntern(text, out var symbol);

        return status == XmlNameTableStatus.Ok
            ? symbol
            : throw new InvalidOperationException(
                status == XmlNameTableStatus.Frozen
                    ? "The name table is frozen and does not contain the name."
                    : "The name table exceeds the maximum number of distinct names."
            );
    }

    /// <summary>Interns the three parts of a name and throws when that is not possible.</summary>
    /// <param name="prefix">The prefix, empty for none.</param>
    /// <param name="local">The local name.</param>
    /// <param name="namespaceUri">The namespace URI, empty for none.</param>
    /// <returns>The name.</returns>
    /// <exception cref="InvalidOperationException">The table is frozen and lacks a part, or is full.</exception>
    public XmlName Intern(ReadOnlySpan<char> prefix, ReadOnlySpan<char> local, ReadOnlySpan<char> namespaceUri) =>
        new(Intern(prefix), Intern(local), Intern(namespaceUri));
}
