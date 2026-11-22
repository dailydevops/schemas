namespace NetEvolve.Schemas.XPath.Errors;

using System;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;

/// <summary>An error code as a QName: namespace URI and local name.</summary>
/// <remarks>Equality is ordinal on both parts, so the same local name in different namespaces is a different code.</remarks>
[StructLayout(LayoutKind.Auto)]
public readonly struct XPathErrorCode : IEquatable<XPathErrorCode>
{
    /// <summary>The standard error namespace of XPath 2.0 and Functions and Operators.</summary>
    public const string StandardNamespace = "http://www.w3.org/2005/xqt-errors";

    private readonly string? _namespaceName;
    private readonly string? _localName;

    /// <summary>Initializes a new instance of the <see cref="XPathErrorCode"/> struct in the standard error namespace.</summary>
    /// <param name="localName">The local name, for example <c>XPST0003</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="localName"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="localName"/> is empty.</exception>
    public XPathErrorCode(string localName)
        : this(StandardNamespace, localName) { }

    /// <summary>Initializes a new instance of the <see cref="XPathErrorCode"/> struct.</summary>
    /// <param name="namespaceName">The namespace URI.</param>
    /// <param name="localName">The local name.</param>
    /// <exception cref="ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="localName"/> is empty.</exception>
    public XPathErrorCode(string namespaceName, string localName)
    {
        ArgumentNullException.ThrowIfNull(namespaceName);
        ArgumentNullException.ThrowIfNullOrEmpty(localName);

        _namespaceName = namespaceName;
        _localName = localName;
    }

    /// <summary>Gets the namespace URI, empty for <see langword="default"/>.</summary>
    public string Namespace => _namespaceName ?? string.Empty;

    /// <summary>Gets the local name, empty for <see langword="default"/>.</summary>
    public string LocalName => _localName ?? string.Empty;

    /// <inheritdoc />
    public bool Equals(XPathErrorCode other) =>
        string.Equals(Namespace, other.Namespace, StringComparison.Ordinal)
        && string.Equals(LocalName, other.LocalName, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XPathErrorCode other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        unchecked((StringComparer.Ordinal.GetHashCode(Namespace) * 31) + StringComparer.Ordinal.GetHashCode(LocalName));

    /// <summary>Returns the local name for the standard namespace and <c>{namespace}local</c> otherwise.</summary>
    /// <returns>The text form of the code.</returns>
    public override string ToString() =>
        _localName is null || string.Equals(Namespace, StandardNamespace, StringComparison.Ordinal)
            ? LocalName
            : "{" + Namespace + "}" + LocalName;

    /// <summary>Determines whether two codes are equal.</summary>
    /// <param name="left">The first code.</param>
    /// <param name="right">The second code.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(XPathErrorCode left, XPathErrorCode right) => left.Equals(right);

    /// <summary>Determines whether two codes differ.</summary>
    /// <param name="left">The first code.</param>
    /// <param name="right">The second code.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(XPathErrorCode left, XPathErrorCode right) => !left.Equals(right);
}
