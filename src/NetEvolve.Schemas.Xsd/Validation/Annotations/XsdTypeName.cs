namespace NetEvolve.Schemas.Xsd.Validation.Annotations;

using System;
using NetEvolve.Arguments;

/// <summary>The expanded name of a type annotation.</summary>
/// <remarks>
/// Limitation: an anonymous type has no name yet. The design proposes a synthetic name that numbers the anonymous types of a schema
/// document, and a component carries neither its document nor that number, so <see cref="IsAnonymous"/> is <see langword="true"/> and both
/// names are empty until the component model supplies them. The typed value and the data model type of such a node are not affected,
/// because they follow the nearest built-in ancestor.
/// </remarks>
internal readonly struct XsdTypeName : IEquatable<XsdTypeName>
{
    /// <summary>The name of an anonymous type.</summary>
    internal static readonly XsdTypeName Anonymous = new(string.Empty, string.Empty, true);

    private readonly string? _namespaceName;
    private readonly string? _localName;

    internal XsdTypeName(string namespaceName, string localName, bool isAnonymous)
    {
        Argument.ThrowIfNull(namespaceName);
        Argument.ThrowIfNull(localName);

        _namespaceName = namespaceName;
        _localName = localName;
        IsAnonymous = isAnonymous;
    }

    /// <summary>Gets the namespace name, empty for no namespace and for <see langword="default"/>.</summary>
    public string NamespaceName => _namespaceName ?? string.Empty;

    /// <summary>Gets the local name, empty for <see langword="default"/>.</summary>
    public string LocalName => _localName ?? string.Empty;

    /// <summary>Gets a value indicating whether the type is anonymous.</summary>
    public bool IsAnonymous { get; }

    /// <inheritdoc />
    public bool Equals(XsdTypeName other) =>
        IsAnonymous == other.IsAnonymous
        && string.Equals(NamespaceName, other.NamespaceName, StringComparison.Ordinal)
        && string.Equals(LocalName, other.LocalName, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is XsdTypeName other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() =>
        unchecked(
            (
                (
                    (StringComparer.Ordinal.GetHashCode(NamespaceName) * 31)
                    + StringComparer.Ordinal.GetHashCode(LocalName)
                ) * 31
            ) + (IsAnonymous ? 1 : 0)
        );

    /// <summary>Determines whether two names are equal.</summary>
    /// <param name="left">The first name.</param>
    /// <param name="right">The second name.</param>
    /// <returns><see langword="true"/> when equal.</returns>
    public static bool operator ==(XsdTypeName left, XsdTypeName right) => left.Equals(right);

    /// <summary>Determines whether two names differ.</summary>
    /// <param name="left">The first name.</param>
    /// <param name="right">The second name.</param>
    /// <returns><see langword="true"/> when different.</returns>
    public static bool operator !=(XsdTypeName left, XsdTypeName right) => !left.Equals(right);
}
