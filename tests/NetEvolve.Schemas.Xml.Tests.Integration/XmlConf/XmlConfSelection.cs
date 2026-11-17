namespace NetEvolve.Schemas.Xml.Tests.Integration.XmlConf;

/// <summary>The split of the catalog into cases that run and cases that are excluded.</summary>
/// <param name="Selected">The cases to run.</param>
/// <param name="Excluded">The cases left out, each with a reason.</param>
internal sealed record XmlConfSelection(IReadOnlyList<XmlConfCase> Selected, IReadOnlyList<ExcludedCase> Excluded);
