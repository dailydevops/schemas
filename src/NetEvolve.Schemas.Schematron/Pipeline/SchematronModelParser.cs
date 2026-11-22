namespace NetEvolve.Schemas.Schematron.Pipeline;

using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Loading.XInclude;
using NetEvolve.Schemas.Schematron.Parsing.Core;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Loading;

/// <summary>
/// The stage Parse (design #137 AP-26): the source is read through the readers of the Xml package into a tree, the
/// <c>xi:include</c> elements are resolved (SCH-17, #251), and the tree is parsed into the object model; the edition comes out of
/// the parse, which reports an unknown one.
/// </summary>
/// <remarks>
/// Every input kind reaches one read: a source in bytes or text is read as it is, a stream by <see cref="SchemaSourceReader"/>. The
/// problems of the Xml package are reported with its codes and the position of the reader. The schema is a resource, not an instance:
/// its size is bounded by <c>MaxResourceSize</c>, and the reader keeps its own limits, as it does for a module that is included.
/// </remarks>
internal sealed class SchematronModelParser : IModelParser
{
    private readonly BuildSettings _settings;
    private readonly CancellationToken _cancellationToken;

    internal SchematronModelParser(BuildSettings settings, CancellationToken cancellationToken)
    {
        Argument.ThrowIfNull(settings);

        _settings = settings;
        _cancellationToken = cancellationToken;
    }

    public bool TryParse(SchemaSource source, IProblemSink problems, out ParsedModule module)
    {
        Argument.ThrowIfNull(source);
        Argument.ThrowIfNull(problems);

        module = null!;
        if (!TryRead(source, problems, out var tree))
        {
            return false;
        }

        var inclusion = new XIncludeResolver(_settings.Resolver, _settings.Limits);
        if (!inclusion.TryResolve(tree.Document, source.SystemId, problems, _cancellationToken, out var document))
        {
            return false;
        }

        if (!SchematronCoreParser.TryParse(document, source.SystemId, problems, out var schema))
        {
            return false;
        }

        module = new ParsedModule(schema!);
        return true;
    }

    private bool TryRead(SchemaSource source, IProblemSink problems, out XmlDocumentTree tree)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        try
        {
            tree =
                source.Kind == SchemaSourceKind.Chars
                    ? XmlDocumentLoader.Load(SchemaSourceReader.Chars(source, _settings.Limits).Span)
                    : XmlDocumentLoader.Load(
                        SchemaSourceReader.Bytes(source, _settings.Limits, _cancellationToken).Span
                    );
            return true;
        }
        catch (XmlParseException failure)
        {
            var issue = XmlIssues.Issue(failure, source.SystemId);
            problems.Add(new SchematronProblem(issue.Code, issue.Message, issue.Location, issue.Constraint));
            tree = null!;
            return false;
        }
    }
}
