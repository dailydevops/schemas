namespace NetEvolve.Schemas.Xsd.Validation.Xsi;

using System;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>Resolves the <c>xsi:type</c> attribute of an element to a type and checks that it may stand in for the declared type.</summary>
internal sealed class XsiTypeResolver
{
    internal XsiTypeResolver(XsdSchemaSet schema, INamespaceContext namespaces, IDerivationCheck? derivation) =>
        throw new NotSupportedException();

    /// <summary>Resolves a value.</summary>
    /// <param name="lexical">The attribute value as it appears in the instance.</param>
    /// <param name="declaredType">The type the declaration stipulates, <see langword="null"/> when the element has no declaration.</param>
    /// <param name="elementBlock">The disallowed substitutions of the element declaration.</param>
    /// <returns>The result.</returns>
    internal XsiTypeResult Resolve(
        ReadOnlySpan<char> lexical,
        IXsdComponent? declaredType,
        XsdDerivationMethod elementBlock
    ) => throw new NotSupportedException();
}
