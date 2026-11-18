namespace NetEvolve.Schemas.XPath.Xdm.Values;

using System;
using NetEvolve.Schemas.Xml.Datatypes.QNames;

/// <summary>
/// The value of an <c>xs:QName</c> item (XDM, 3.3.2): the expanded name and the prefix it was written with. The prefix is not part of the
/// identity, so two prefixes bound to one namespace give equal values; it is kept because the string value of a QName shows it.
/// </summary>
/// <remarks>Immutable and thread-safe.</remarks>
internal sealed class XdmQName : IEquatable<XdmQName>
{
    /// <summary>Initializes a new instance of the <see cref="XdmQName"/> class.</summary>
    /// <param name="name">The expanded name.</param>
    /// <param name="prefix">The prefix, <see langword="null"/> when the name was written without one.</param>
    public XdmQName(XsdQName name, string? prefix)
    {
        Name = name;
        Prefix = prefix;
    }

    /// <summary>Gets the expanded name.</summary>
    public XsdQName Name { get; }

    /// <summary>Gets the prefix, <see langword="null"/> when the name has none.</summary>
    public string? Prefix { get; }

    /// <summary>Gets the lexical form: <c>prefix:local</c>, or the local name alone.</summary>
    /// <returns>The lexical form.</returns>
    public override string ToString() => Prefix is null ? Name.LocalName : Prefix + ":" + Name.LocalName;

    /// <inheritdoc />
    public bool Equals(XdmQName? other) => other is not null && Name.Equals(other.Name);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XdmQName other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => Name.GetHashCode();
}
