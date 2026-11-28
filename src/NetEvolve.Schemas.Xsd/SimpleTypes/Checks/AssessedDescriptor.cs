namespace NetEvolve.Schemas.Xsd.SimpleTypes.Checks;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Validation.Core;
using DatatypeResult = NetEvolve.Schemas.Xml.Datatypes.Contracts.ValidationResult;

/// <summary>
/// A member type of a union that no descriptor of the Xml package describes whole (a list, a union, or a restriction of <c>anyURI</c>):
/// it accepts what the value assessor accepts for the definition.
/// </summary>
internal sealed class AssessedDescriptor : IDatatypeDescriptor
{
    private readonly SimpleTypeDefinition _definition;
    private readonly ISimpleValueAssessor _assessor;

    internal AssessedDescriptor(SimpleTypeDefinition definition, ISimpleValueAssessor assessor, XsdVersion version)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(assessor);

        _definition = definition;
        _assessor = assessor;
        LexicalMode = version;
    }

    /// <inheritdoc />
    public string Name => _definition.IsAnonymous ? "anonymous" : _definition.Name.LocalName;

    /// <inheritdoc />
    public DatatypeVariety Variety => _definition.Variety;

    /// <inheritdoc />
    public IDatatypeDescriptor? BaseType => null;

    /// <inheritdoc />
    public XsdVersion LexicalMode { get; }

    /// <inheritdoc />
    public bool IsOrdered => false;

    /// <inheritdoc />
    public IReadOnlyList<IFacet> Facets => _definition.Facets;

    /// <inheritdoc />
    public DatatypeResult Validate(ReadOnlySpan<char> lexical) => _assessor.Assess(_definition, lexical, out _);
}
