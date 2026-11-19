namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Validation.Document;

using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Dtd.Validation.Document;

/// <summary>A declaration view that also records parse-time facts, the way the parser model will once #608 is done; every member delegates to a hand-built model.</summary>
internal sealed class AnomalousDeclarations : IDtdDeclarations, IDtdAnomalySource
{
    private readonly DtdDeclarations _inner;

    internal AnomalousDeclarations(DtdDeclarations inner, params DtdAnomaly[] anomalies)
    {
        _inner = inner;
        Anomalies = anomalies;
    }

    public IReadOnlyList<DtdAnomaly> Anomalies { get; }

    public string Name => _inner.Name;

    public string? PublicId => _inner.PublicId;

    public string? SystemId => _inner.SystemId;

    public bool HasExternalSubset => _inner.HasExternalSubset;

    public bool HasParameterEntityReferences => _inner.HasParameterEntityReferences;

    public IReadOnlyDictionary<string, DtdElementDeclaration> Elements => _inner.Elements;

    public IReadOnlyDictionary<string, IReadOnlyList<DtdAttributeDefinition>> AttributeLists => _inner.AttributeLists;

    public IReadOnlyDictionary<string, DtdEntityDeclaration> GeneralEntities => _inner.GeneralEntities;

    public IReadOnlyDictionary<string, DtdEntityDeclaration> ParameterEntities => _inner.ParameterEntities;

    public IReadOnlyDictionary<string, XmlNotation> Notations => _inner.Notations;
}
