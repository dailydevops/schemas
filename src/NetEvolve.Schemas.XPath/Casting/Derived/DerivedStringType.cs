namespace NetEvolve.Schemas.XPath.Casting.Derived;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xml.Datatypes.Strings;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Casting.Other;
using NetEvolve.Schemas.XPath.Casting.Temporal;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The conversion into one built-in type derived from <c>xs:string</c> (Functions and Operators, 17.2, 17.4, and 17.5): the value is cast to
/// <c>xs:string</c>, the whitespace of the type is applied, and the datatype layer checks the lexical space of the type and its facets. The result
/// is an <c>xs:string</c> item with the annotation of the type.
/// </summary>
/// <remarks>
/// A source of any primitive type is allowed, because every primitive type has a string value; the string value is the canonical lexical form that
/// the cast to <c>xs:string</c> gives, so for instance a date is a valid <c>xs:token</c> and an invalid <c>xs:NCName</c>. Immutable and thread-safe;
/// one instance serves every row of the type.
/// </remarks>
internal sealed class DerivedStringType
{
    private readonly XdmTypeId _type;
    private readonly AtomicDescriptor<XsdString> _descriptor;
    private readonly WhitespaceMode _whitespace;

    /// <summary>Initializes a new instance of the <see cref="DerivedStringType"/> class.</summary>
    /// <param name="type">The type.</param>
    /// <param name="descriptor">The datatype of the type, which applies its whitespace and checks its lexical space.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="descriptor"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="type"/> is <see langword="default"/>.</exception>
    public DerivedStringType(XdmTypeId type, AtomicDescriptor<XsdString> descriptor)
    {
        Argument.ThrowIfLessThan(type.Value, 1);
        Argument.ThrowIfNull(descriptor);

        _type = type;
        _descriptor = descriptor;
        _whitespace = descriptor.Whitespace;
    }

    /// <summary>Converts a value to the type.</summary>
    /// <param name="source">The source value, an item of one of the primitive types.</param>
    /// <param name="result">The result, <see langword="default"/> unless the status is <see cref="CastStatus.Ok"/>.</param>
    /// <returns>The status; a value outside the lexical space of the type is <see cref="CastStatus.InvalidLexical"/> (FORG0001).</returns>
    public CastStatus Convert(in XdmItem source, out XdmItem result)
    {
        result = default;
        if (
            !(
                NumberText.TryFormat(in source, out var text)
                || TemporalConversions.TryFormat(in source, out text)
                || OtherConversions.TryFormat(in source, out text)
            )
        )
        {
            return CastStatus.NotAllowed;
        }

        var normalized = WhitespaceText.Apply(text!, _whitespace);
        if (!_descriptor.Validate(normalized.AsSpan()).IsValid)
        {
            return CastStatus.InvalidLexical;
        }

        result = XdmItem.FromString(normalized).WithTypeId(_type);
        return CastStatus.Ok;
    }
}
