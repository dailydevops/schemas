namespace NetEvolve.Schemas.Tests.Architecture.Schematron;

using System.Reflection;

/// <summary>What a layering rule inspects: the types under a root namespace.</summary>
/// <param name="Assembly">The assembly that holds the types.</param>
/// <param name="Root">The root namespace of the package, without a trailing dot.</param>
internal sealed record LayerInput(Assembly Assembly, string Root);
