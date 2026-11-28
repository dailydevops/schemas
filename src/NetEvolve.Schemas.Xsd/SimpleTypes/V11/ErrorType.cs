namespace NetEvolve.Schemas.Xsd.SimpleTypes.V11;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>
/// The built-in type <c>xs:error</c> of XSD 1.1 (Structures section 3.16.7): a union of no member types, so it has no valid value.
/// </summary>
/// <remarks>
/// <para>
/// The Xml package registry holds the other 1.1 datatypes (<c>dateTimeStamp</c>, <c>yearMonthDuration</c>, <c>dayTimeDuration</c>, and
/// <c>anyAtomicType</c>) but not this one: its datatype is a union without members, which the union datatype of that package does not allow.
/// The type is registered next to the built-in types of the version and exists in 1.1 only, so a reference to it is an unresolved
/// reference (<c>src-resolve</c>) in strict 1.0 mode.
/// </para>
/// <para>
/// Limitation: the call that registers the built-in types of a version (<c>XsdBuiltInTypes.Register</c>) does not add this type; the
/// 1.1 pipeline registers it with <see cref="Register"/> (XSD-78).
/// </para>
/// </remarks>
internal static class ErrorType
{
    private static readonly Lazy<XsdBuiltInType> Shared = new(static () => Create());

    /// <summary>Gets the type; the same instance on every call, because the type is immutable.</summary>
    internal static XsdBuiltInType Instance => Shared.Value;

    /// <summary>Registers the type when the version has it.</summary>
    /// <param name="registry">The registry.</param>
    /// <param name="version">The XSD version; the type is registered for 1.1 only.</param>
    internal static void Register(IComponentRegistry registry, XsdVersion version)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentOutOfRangeException.ThrowIfLessThan((int)version, (int)XsdVersion.V10);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)version, (int)XsdVersion.V11);

        if (version == XsdVersion.V11)
        {
            registry.Add(Instance);
        }
    }

    private static XsdBuiltInType Create()
    {
        var types = XsdBuiltInTypes.For(XsdVersion.V11);
        XsdBuiltInType? anySimpleType = null;
        for (var i = 0; i < types.Count; i++)
        {
            if (string.Equals(types[i].Name.LocalName, "anySimpleType", StringComparison.Ordinal))
            {
                anySimpleType = (XsdBuiltInType)types[i];
            }
        }

        return new XsdBuiltInType(XsdComponentKind.SimpleType, "error", new NoValueDatatype(), anySimpleType);
    }

    // A union without member types: no lexical form is valid.
    private sealed class NoValueDatatype : IDatatypeDescriptor
    {
        public string Name => "error";

        public DatatypeVariety Variety => DatatypeVariety.Union;

        public IDatatypeDescriptor? BaseType => null;

        public XsdVersion LexicalMode => XsdVersion.V11;

        public bool IsOrdered => false;

        public IReadOnlyList<IFacet> Facets { get; } = [];

        public ValidationResult Validate(ReadOnlySpan<char> lexical) =>
            ValidationResult.Invalid(DatatypeError.NoUnionMember, FacetKind.None, 0);
    }
}
