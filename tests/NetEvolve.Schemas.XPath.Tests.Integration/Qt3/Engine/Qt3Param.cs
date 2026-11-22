namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Engine;

/// <summary>A <c>param</c> element: a variable bound to the value of an expression.</summary>
/// <param name="Name">The variable name.</param>
/// <param name="Select">The expression, <see langword="null"/> when the element has none.</param>
internal sealed record Qt3Param(string Name, string? Select);
