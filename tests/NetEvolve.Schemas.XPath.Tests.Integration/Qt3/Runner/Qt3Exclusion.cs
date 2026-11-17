namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

/// <summary>A case the dependency filter removed.</summary>
/// <param name="CaseId">The case id.</param>
/// <param name="Reason">The unmet dependencies, for example <c>feature schemaImport</c>.</param>
internal sealed record Qt3Exclusion(string CaseId, string Reason);
