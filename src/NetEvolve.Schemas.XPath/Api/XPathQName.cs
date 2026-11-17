#pragma warning disable CA1054, CA1056 // Approved shape (#123): namespace URIs are passed as strings.

namespace NetEvolve.Schemas.XPath.Api;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;

/// <summary>An expanded name: namespace URI and local name. Equality is ordinal on both parts.</summary>
[StructLayout(LayoutKind.Auto)]
public readonly struct XPathQName : IEquatable<XPathQName>
{
    private readonly string? _namespaceUri;
    private readonly string? _localName;

    /// <summary>Initializes a new instance of the <see cref="XPathQName"/> struct.</summary>
    /// <param name="namespaceUri">The namespace URI, empty for no namespace.</param>
    /// <param name="localName">The local name.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="localName"/> is empty.</exception>
    public XPathQName(string namespaceUri, string localName)
    {
        Argument.ThrowIfNull(namespaceUri);
        Argument.ThrowIfNullOrEmpty(localName);

        _namespaceUri = namespaceUri;
        _localName = localName;
    }

    /// <summary>Gets the namespace URI, empty for <see langword="default"/>.</summary>
    public string NamespaceUri => _namespaceUri ?? string.Empty;

    /// <summary>Gets the local name, empty for <see langword="default"/>.</summary>
    public string LocalName => _localName ?? string.Empty;

    /// <inheritdoc />
    public bool Equals(XPathQName other) =>
        string.Equals(NamespaceUri, other.NamespaceUri, StringComparison.Ordinal)
        && string.Equals(LocalName, other.LocalName, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XPathQName other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        unchecked(
            (StringComparer.Ordinal.GetHashCode(NamespaceUri) * 31) + StringComparer.Ordinal.GetHashCode(LocalName)
        );

    /// <inheritdoc />
    public override string ToString() => $"Q{{{NamespaceUri}}}{LocalName}";

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
