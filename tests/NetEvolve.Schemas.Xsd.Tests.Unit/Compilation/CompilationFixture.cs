namespace NetEvolve.Schemas.Xsd.Tests.Unit.Compilation;

using System.Collections.Generic;
using System.Text;
using System.Threading;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.Compilation;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Tests.Unit.Composition;

/// <summary>Compiles schema text through the pipeline under test.</summary>
internal static class CompilationFixture
{
    public const string RootUri = CompositionFixture.RootUri;

    public const string Xs = CompositionFixture.Xs;

    public const string Tns = "urn:t";

    /// <summary>Wraps a body in a schema element with the prefix <c>t</c> bound to <see cref="Tns"/>.</summary>
    public static string Schema(string body) => CompositionFixture.Schema(body, Tns);

    public static SchemaCompiler Compiler(
        XsdVersion version = XsdVersion.V10,
        IResourceResolver? resolver = null,
        ValidationLimits? limits = null
    ) => new(version, resolver ?? DenyAllResourceResolver.Instance, limits ?? ValidationLimits.Default);

    public static SchemaCompilation Compile(
        string root,
        XsdVersion version = XsdVersion.V10,
        IResourceResolver? resolver = null
    ) => Compiler(version, resolver).Compile(root.AsSpan(), RootUri, CancellationToken.None);

    public static IXsdComponent Get(SchemaCompilation compilation, XsdComponentKind kind, string name) =>
        compilation.Set!.TryGet(kind, new XPathQName(Tns, name), out var component)
            ? component!
            : throw new KeyNotFoundException(name);

    public static T Get<T>(SchemaCompilation compilation, XsdComponentKind kind, string name)
        where T : class, IXsdComponent => (T)Get(compilation, kind, name);

    public static List<DiagnosticCode> Codes(SchemaCompilation compilation)
    {
        var codes = new List<DiagnosticCode>();
        foreach (var issue in compilation.Result.Issues)
        {
            codes.Add(issue.Code);
        }

        return codes;
    }

    public static string Describe(SchemaCompilation compilation)
    {
        var text = new StringBuilder();
        foreach (var issue in compilation.Result.Issues)
        {
            _ = text.Append(issue).Append('\n');
        }

        return text.ToString();
    }
}
