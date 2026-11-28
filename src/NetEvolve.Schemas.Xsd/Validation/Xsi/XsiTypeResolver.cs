namespace NetEvolve.Schemas.Xsd.Validation.Xsi;

using System;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Derivation.Types;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;
using NetEvolve.Schemas.Xsd.Substitution;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>
/// Resolves the <c>xsi:type</c> attribute of an element to a type and checks that it may stand in for the declared type
/// (Structures 3.3.4, Element Locally Valid (Element), clause 4): the type must exist, and it must be validly derived from the
/// declared type given the union of the disallowed substitutions of the element and the prohibited substitutions of the type.
/// </summary>
/// <remarks>
/// The type is looked up in the schema set, which holds the built-in types when the compile pipeline registers them. The
/// derivation check is the <see cref="IDerivationCheck"/> seam, asked with <see cref="XsdDerivationMethod.Substitution"/> and a
/// block set of extension and restriction only: the substitution bit of the element block applies to substitution groups, not to
/// <c>xsi:type</c>. An element without a declaration stipulates no type, so any named type is accepted.
/// </remarks>
internal sealed class XsiTypeResolver
{
    private const XsdDerivationMethod TypeDerivations = XsdDerivationMethod.Extension | XsdDerivationMethod.Restriction;
    private static readonly char[] Whitespace = [' ', '\t', '\r', '\n'];

    private readonly XsdSchemaSet _schema;
    private readonly INamespaceContext _namespaces;
    private IDerivationCheck? _derivation;

    internal XsiTypeResolver(XsdSchemaSet schema, INamespaceContext namespaces, IDerivationCheck? derivation)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(namespaces);

        _schema = schema;
        _namespaces = namespaces;
        _derivation = derivation;
    }

    /// <summary>Resolves a value.</summary>
    /// <param name="lexical">The attribute value as it appears in the instance.</param>
    /// <param name="declaredType">The type the declaration stipulates, <see langword="null"/> when the element has no declaration.</param>
    /// <param name="elementBlock">The disallowed substitutions of the element declaration.</param>
    /// <returns>The result.</returns>
    internal XsiTypeResult Resolve(
        ReadOnlySpan<char> lexical,
        IXsdComponent? declaredType,
        XsdDerivationMethod elementBlock
    )
    {
        var parsed = XsdQName.Parse(lexical.Trim(Whitespace), _namespaces);
        if (!parsed.Success)
        {
            var status =
                parsed.Error == DatatypeError.UnboundPrefix ? XsiTypeStatus.UnboundPrefix : XsiTypeStatus.InvalidQName;
            return new XsiTypeResult(status, null, default, null);
        }

        var name = new XPathQName(parsed.Value.NamespaceName ?? string.Empty, parsed.Value.LocalName);
        if (!_schema.TryGet(XsdComponentKind.SimpleType, name, out var type))
        {
            return new XsiTypeResult(XsiTypeStatus.UnknownType, null, name, null);
        }

        var named = type!;
        if (declaredType is null || ReferenceEquals(named, declaredType))
        {
            return new XsiTypeResult(XsiTypeStatus.Resolved, named, name, null);
        }

        var block = elementBlock;
        if (declaredType is IBlockedType blocked)
        {
            block |= blocked.Block;
        }

        _derivation ??= new TypeDerivationCheck(
            _schema.Version,
            new ValueSpace(new DescriptorValueAssessor(_schema.Version), _schema.Version)
        );
        var result = _derivation.Check(named, declaredType, XsdDerivationMethod.Substitution, block & TypeDerivations);
        return result.IsValid
            ? new XsiTypeResult(XsiTypeStatus.Resolved, named, name, null)
            : new XsiTypeResult(XsiTypeStatus.NotDerived, null, name, result.ViolatedRule);
    }
}
