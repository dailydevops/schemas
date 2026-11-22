namespace NetEvolve.Schemas.Tests.LegacyCompiler.Schematron;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

/// <summary>Compiles consumer source in memory against the libraries under test with a chosen language version.</summary>
internal static class ConsumerCompiler
{
    private static readonly Lazy<MetadataReference[]> _references = new(LoadReferences);

    internal static ConsumerCompilation Compile(string source, LanguageVersion languageVersion)
    {
        var compilation = CSharpCompilation.Create(
            "Consumer" + Guid.NewGuid().ToString("N"),
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(languageVersion))],
            _references.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        var errors = new List<string>();
        foreach (var diagnostic in result.Diagnostics)
        {
            if (diagnostic.Severity == DiagnosticSeverity.Error)
            {
                errors.Add(diagnostic.Id);
            }
        }

        return new ConsumerCompilation(errors, result.Success ? Assembly.Load(stream.ToArray()) : null);
    }

    // The runtime lists every assembly of the test host, the framework and the libraries under test.
    private static MetadataReference[] LoadReferences()
    {
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        var references = new MetadataReference[paths.Length];
        for (var i = 0; i < paths.Length; i++)
        {
            references[i] = MetadataReference.CreateFromFile(paths[i]);
        }

        return references;
    }
}
