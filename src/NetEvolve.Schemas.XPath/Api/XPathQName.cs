namespace NetEvolve.Schemas.XPath.Api;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;

/// <summary>An expanded name: namespace name (the namespace URI) and local name. Equality is ordinal on both parts.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct XPathQName : IEquatable<XPathQName>
{
    private readonly string? _namespaceName;
    private readonly string? _localName;

    /// <summary>Initializes a new instance of the <see cref="XPathQName"/> struct.</summary>
    /// <param name="namespaceName">The namespace name, empty for no namespace.</param>
    /// <param name="localName">The local name.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="localName"/> is empty.</exception>
    public XPathQName(string namespaceName, string localName)
    {
        Argument.ThrowIfNull(namespaceName);
        Argument.ThrowIfNullOrEmpty(localName);

        _namespaceName = namespaceName;
        _localName = localName;
    }

    /// <summary>Gets the namespace name, empty for <see langword="default"/>.</summary>
    public string NamespaceName => _namespaceName ?? string.Empty;

    /// <summary>Gets the local name, empty for <see langword="default"/>.</summary>
    public string LocalName => _localName ?? string.Empty;

    /// <inheritdoc />
    public bool Equals(XPathQName other) =>
        string.Equals(NamespaceName, other.NamespaceName, StringComparison.Ordinal)
        && string.Equals(LocalName, other.LocalName, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XPathQName other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        unchecked(
            (StringComparer.Ordinal.GetHashCode(NamespaceName) * 31) + StringComparer.Ordinal.GetHashCode(LocalName)
        );

    /// <inheritdoc />
    public override string ToString() => $"Q{{{NamespaceName}}}{LocalName}";

    /// <summary>Determines whether two names are equal.</summary>
    /// <param name="left">The first name.</param>
    /// <param name="right">The second name.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(XPathQName left, XPathQName right) => left.Equals(right);

    /// <summary>Determines whether two names differ.</summary>
    /// <param name="left">The first name.</param>
    /// <param name="right">The second name.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(XPathQName left, XPathQName right) => !left.Equals(right);
}
