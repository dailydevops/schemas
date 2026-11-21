namespace NetEvolve.Schemas.Tests.Architecture.Schematron;

using System.Reflection;
using System.Text.RegularExpressions;
using NetArchTest.Rules;
using NetEvolve.Schemas.Tests.Architecture.Xml;

/// <summary>
/// Rules for the composition folder of the Schematron package (SCH-55, #311): the pipeline reads an instance only through the readers
/// of the Xml package, so it never decodes or parses text on its own and has no second parse path.
/// </summary>
internal static class PipelineReaderRules
{
    /// <summary>The namespace of the composition folder.</summary>
    internal const string Pipeline = "NetEvolve.Schemas.Schematron.Pipeline";

    /// <summary>The loader of the Xml package that builds the tree of an instance from every input kind.</summary>
    internal const string Loader = "NetEvolve.Schemas.Xml.Model.Loading.XmlDocumentLoader";

    /// <summary>The types that decode or parse text without the readers of the Xml package.</summary>
    internal static readonly string[] OwnReaders =
    [
        "System.Xml",
        "System.Text.Encoding",
        "System.Text.Json",
        "System.IO.TextReader",
        "System.IO.StreamReader",
        "System.IO.StringReader",
    ];

    /// <summary>Lists the types in <paramref name="ns"/> (and below) that decode or parse text on their own.</summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="ns">The namespace of the folder.</param>
    internal static IReadOnlyList<string> Violations(Assembly assembly, string ns) =>
        XmlLayering.FindDependencies(assembly, ns, OwnReaders);

    /// <summary>Lists the types in <paramref name="ns"/> (and below) that depend on the loader of the Xml package.</summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <param name="ns">The namespace of the folder.</param>
    internal static IReadOnlyList<string> Loaders(Assembly assembly, string ns) =>
        [
            .. Types
                .InAssembly(assembly)
                .That()
                .ResideInNamespaceMatching($@"^{Regex.Escape(ns)}(\.|$)")
                .And()
                .HaveDependencyOn(Loader)
                .GetTypes()
                .Select(type => type.FullName!),
        ];
}
