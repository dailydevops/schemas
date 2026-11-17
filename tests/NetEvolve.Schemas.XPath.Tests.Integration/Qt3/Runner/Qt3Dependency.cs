namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;

/// <summary>A <c>dependency</c> element of the catalog.</summary>
/// <param name="Type">The dependency type, for example <c>spec</c> or <c>feature</c>.</param>
/// <param name="Value">The dependency value.</param>
/// <param name="Satisfied"><see langword="false"/> when the case needs the dependency to be absent.</param>
internal sealed record Qt3Dependency(string Type, string Value, bool Satisfied);
