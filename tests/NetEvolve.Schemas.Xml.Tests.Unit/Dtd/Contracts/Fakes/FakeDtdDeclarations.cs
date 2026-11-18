namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Contracts.Fakes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;

/// <summary>A hand-written declaration view that is not the model of the parser: proof that a step depends on the view only.</summary>
internal sealed class FakeDtdDeclarations : IDtdDeclarations
{
    public string Name => "fake";

    public string? PublicId => null;

    public string? SystemId => null;

    public bool HasExternalSubset => false;

    public bool HasParameterEntityReferences => false;

    public IReadOnlyDictionary<string, DtdElementDeclaration> Elements { get; } =
        new Dictionary<string, DtdElementDeclaration>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, IReadOnlyList<DtdAttributeDefinition>> AttributeLists { get; } =
        new Dictionary<string, IReadOnlyList<DtdAttributeDefinition>>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, DtdEntityDeclaration> GeneralEntities { get; } =
        new Dictionary<string, DtdEntityDeclaration>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, DtdEntityDeclaration> ParameterEntities { get; } =
        new Dictionary<string, DtdEntityDeclaration>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, XmlNotation> Notations { get; } =
        new Dictionary<string, XmlNotation>(StringComparer.Ordinal);
}
