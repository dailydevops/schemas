namespace NetEvolve.Schemas.Schematron.Tests.Unit.Loading.XInclude;

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Loading.XInclude;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Tests.Unit.Loading.Inclusion;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>The documents, the resolve call, and the tree dump of the XInclude tests.</summary>
internal static class XIncludeFixture
{
    internal const string Xi = "http://www.w3.org/2001/XInclude";
    internal const string RootUri = "file:///a/root.xml";

    /// <summary>Wraps a body in an element that declares the XInclude prefix.</summary>
    internal static string Doc(string body, string name = "root") =>
        $"<{name} xmlns:xi=\"{Xi}\" xmlns=\"urn:t\">{body}</{name}>";

    internal static string Inc(string href, string attributes = "", string body = "") =>
        $"<xi:include href=\"{href}\"{attributes}>{body}</xi:include>";

    internal static string Fallback(string body) => $"<xi:fallback>{body}</xi:fallback>";

    internal static Outcome Run(
        string xml,
        IResourceResolver resources,
        ValidationLimits? limits = null,
        string? systemId = RootUri
    )
    {
        var problems = new FakeProblemSink();
        var resolver = new XIncludeResolver(resources, limits ?? ValidationLimits.Default);
        var ok = resolver.TryResolve(Load(xml), systemId, problems, CancellationToken.None, out var result);
        return new Outcome(ok, result, problems.Problems);
    }

    internal static XmlNode Load(string xml) => InclusionFixture.Document(xml);

    internal static string Dump(XmlNode node)
    {
        var builder = new StringBuilder();
        Write(builder, node);
        return builder.ToString();
    }

    private static void Write(StringBuilder builder, XmlNode node)
    {
        switch (node.Kind)
        {
            case XmlNodeKind.Element:
                var name = node.Tree!.Names.GetString(node.Name.Local);
                builder.Append('<').Append(name);
                for (var a = node.FirstAttribute; !a.IsEmpty; a = a.NextAttribute)
                {
                    builder
                        .Append(' ')
                        .Append(node.Tree.Names.GetString(a.Name.Local))
                        .Append("=\"")
                        .Append(a.StringValue)
                        .Append('"');
                }

                builder.Append('>');
                WriteChildren(builder, node);
                builder.Append("</").Append(name).Append('>');
                break;
            case XmlNodeKind.Text:
                builder.Append(node.StringValue);
                break;
            case XmlNodeKind.Comment:
                builder.Append("<!--").Append(node.StringValue).Append("-->");
                break;
            case XmlNodeKind.ProcessingInstruction:
                builder
                    .Append("<?")
                    .Append(node.Tree!.Names.GetString(node.Name.Local))
                    .Append(' ')
                    .Append(node.StringValue)
                    .Append("?>");
                break;
            default:
                WriteChildren(builder, node);
                break;
        }
    }

    private static void WriteChildren(StringBuilder builder, XmlNode node)
    {
        for (var c = node.FirstChild; !c.IsEmpty; c = c.NextSibling)
        {
            Write(builder, c);
        }
    }

    /// <summary>Returns the first element child of the document element, for position and base URI assertions.</summary>
    internal static XmlNode FirstChildElement(XmlNode document)
    {
        var root = document.FirstChild;
        for (var c = root.FirstChild; !c.IsEmpty; c = c.NextSibling)
        {
            if (c.Kind == XmlNodeKind.Element)
            {
                return c;
            }
        }

        return default;
    }

    internal sealed class Outcome(bool ok, XmlNode document, List<SchematronProblem> problems)
    {
        internal bool Ok { get; } = ok;

        internal XmlNode Document { get; } = document;

        internal List<SchematronProblem> Problems { get; } = problems;

        internal string Text => Dump(Document);
    }

    /// <summary>A resolver over raw bytes, for text resources in other encodings.</summary>
    internal sealed class ByteResources : IResourceResolver
    {
        private readonly Dictionary<string, byte[]> _files = new(StringComparer.Ordinal);

        internal ByteResources Add(string uri, byte[] bytes)
        {
            _files[uri] = bytes;
            return this;
        }

        public ResourceResult Resolve(in ResourceRequest request, CancellationToken cancellationToken) =>
            _files.TryGetValue(request.ResolvedUri ?? request.Reference, out var bytes)
                ? ResourceResult.Found(new ReadOnlyMemory<byte>(bytes), request.ResolvedUri ?? request.Reference)
                : ResourceResult.NotFound("none");
    }
}
