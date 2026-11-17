namespace NetEvolve.Schemas.Tests.Architecture.XPath;

using System.Reflection;

/// <summary>What a layering rule inspects: the types under a root namespace and the package-relative source paths.</summary>
/// <param name="Assembly">The assembly that holds the types.</param>
/// <param name="Root">The root namespace of the package, without a trailing dot.</param>
/// <param name="HigherPackages">The root namespaces of the packages above XPath (Schematron and Xsd).</param>
/// <param name="SourcePaths">Source file paths relative to the package folder, for example <c>Functions/Strings/Registration.cs</c>.</param>
internal sealed record XPathLayerInput(
    Assembly Assembly,
    string Root,
    IReadOnlyList<string> HigherPackages,
    IReadOnlyList<string> SourcePaths
);
