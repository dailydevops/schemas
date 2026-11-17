namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;

/// <summary>What an engine produced for one case.</summary>
internal enum EngineResultKind
{
    /// <summary>The query evaluated to a sequence.</summary>
    Value,

    /// <summary>The query raised an error.</summary>
    Error,

    /// <summary>No engine is bound; every active assertion fails.</summary>
    NotBound,
}
