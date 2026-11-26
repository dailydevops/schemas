namespace NetEvolve.Schemas.Schematron.Pipeline;

using System.Threading;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Formats;
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
/// Every input kind reaches one read: a source in bytes or text is read as it is, a stream by <see cref="SchemaSourceHolder"/>. The
/// problems of the Xml package are reported with its codes and the position of the reader. The schema is a resource, not an instance:
/// its size is bounded by <c>MaxResourceSize</c>, and the reader keeps its own limits, as it does for a module that is included.
/// </remarks>
internal sealed class SchematronModelParser : IModelParser
{
    private readonly BuildSettings _settings;
    private readonly CancellationToken _cancellationToken;

    internal SchematronModelParser(BuildSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _settings = settings;
        _cancellationToken = cancellationToken;
    }

    public bool TryParse(SchemaSourceHolder source, IProblemSink problems, out ParsedModule module)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(problems);

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

    private bool TryRead(SchemaSourceHolder source, IProblemSink problems, out XmlDocumentTree tree)
    {
        _cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var options = _settings.DtdProcessing ? EntityOptions(_settings.Limits) : null;
            tree =
                source.Kind == DocumentSourceKind.Chars
                    ? XmlDocumentLoader.Load(source.GetChars(_settings.Limits).Span, options)
                    : XmlDocumentLoader.Load(source.GetBytes(_settings.Limits, _cancellationToken).Span, options);
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

    // The schema keeps the reader's own limits, except the entity expansion, which follows the validator limits.
    private static XmlReaderOptions EntityOptions(ValidationLimits limits) =>
        XmlReaderOptions
            .Default.WithDtdProcessing(true)
            .WithLimits(
                XmlReaderLimits
                    .Default.WithMaxEntityExpansionCharacters(limits.MaxEntityExpansion)
                    .WithMaxEntityDepth(limits.MaxEntityDepth)
            );
}
