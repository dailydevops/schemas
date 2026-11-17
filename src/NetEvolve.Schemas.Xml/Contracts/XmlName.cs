namespace NetEvolve.Schemas.Xml.Contracts;

using System;
using System.Runtime.InteropServices;

/// <summary>An element, attribute, or target name as three <see cref="XmlSymbol"/> handles of one <see cref="XmlNameTable"/>.</summary>
/// <remarks>Equality compares the three handles, so it is only meaningful for names of the same table.</remarks>
[StructLayout(LayoutKind.Auto)]
public readonly struct XmlName : IEquatable<XmlName>
{
    /// <summary>Initializes a new instance of the <see cref="XmlName"/> struct.</summary>
    /// <param name="prefix">The prefix, empty for none.</param>
    /// <param name="local">The local name.</param>
    /// <param name="namespaceUri">The namespace URI, empty for none.</param>
    public XmlName(XmlSymbol prefix, XmlSymbol local, XmlSymbol namespaceUri)
    {
        Prefix = prefix;
        Local = local;
        NamespaceUri = namespaceUri;
    }

    /// <summary>Gets the prefix, empty for none.</summary>
    public XmlSymbol Prefix { get; }

    /// <summary>Gets the local name.</summary>
    public XmlSymbol Local { get; }

    /// <summary>Gets the namespace URI, empty for none.</summary>
    public XmlSymbol NamespaceUri { get; }

    /// <inheritdoc />
    public bool Equals(XmlName other) =>
        Prefix == other.Prefix && Local == other.Local && NamespaceUri == other.NamespaceUri;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XmlName other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => unchecked((((Prefix.Value * 31) + Local.Value) * 31) + NamespaceUri.Value);

    /// <summary>Determines whether two names are equal.</summary>
    /// <param name="left">The first name.</param>
    /// <param name="right">The second name.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(XmlName left, XmlName right) => left.Equals(right);

    /// <summary>Determines whether two names differ.</summary>
    /// <param name="left">The first name.</param>
    /// <param name="right">The second name.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(XmlName left, XmlName right) => !left.Equals(right);
}
