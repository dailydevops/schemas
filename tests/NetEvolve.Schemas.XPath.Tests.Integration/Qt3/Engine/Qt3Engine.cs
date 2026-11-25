namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Engine;

using System.Collections.Concurrent;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;
using NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Runner;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using QName = NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static.QName;

/// <summary>
/// The XPath engine behind the runner seam: it loads the environment of a case through the Xml package, compiles the query with the
/// public API, evaluates it, and turns the sequence into the items of an <see cref="EngineResult"/>.
/// </summary>
/// <remarks>
/// <para>
/// An <c>xs:boolean</c> item is a <see cref="bool"/>, an <c>xs:string</c> item a <see cref="string"/>, every other item an
/// <see cref="XdmItem"/>. A static or dynamic error of the specifications is an error result. A condition that is no error of the
/// specifications is never an error result, because <c>error code="*"</c> would pass it: an engine condition (a limit), a document
/// that does not load, and an unexpected exception all give a not-bound result with its own reason, which fails every active case.
/// </para>
/// <para>
/// Limitations: the prefixes <c>xs</c>, <c>xsi</c> and <c>fn</c> are declared in every static context, the implicit timezone and the
/// clock are the defaults of the library, and the static base URI is absent unless the environment gives one, so that a result never
/// depends on the checkout path. Source documents are loaded with their internal document type declaration processed and are not
/// validated.
/// </para>
/// </remarks>
internal sealed class Qt3Engine : IQt3Engine
{
    private static readonly XPathQName Result = new(string.Empty, "result");
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static Qt3Engine? s_shared;

    private readonly Qt3EnvironmentCatalog _environments;
    private readonly ConcurrentDictionary<string, Lazy<XmlDocumentTree>> _documents = new(StringComparer.Ordinal);

    /// <summary>Initializes a new instance of the <see cref="Qt3Engine"/> class.</summary>
    /// <param name="suiteRoot">The directory of the QT3 suite, <c>catalog.xml</c> inside.</param>
    public Qt3Engine(string suiteRoot) => _environments = new Qt3EnvironmentCatalog(suiteRoot);

    /// <summary>
    /// Gets the engine for the pinned suite, shared by all tests so that a source document loads once; the archive is downloaded once and
    /// cached by checksum.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the fetch.</param>
    /// <returns>The engine.</returns>
    public static async Task<Qt3Engine> GetSharedAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return s_shared ??= new Qt3Engine(await Qt3Suite.GetRootAsync(cancellationToken).ConfigureAwait(false));
        }
        finally
        {
            _ = Gate.Release();
        }
    }

    public EngineResult Evaluate(Qt3Case testCase)
    {
        try
        {
            return EvaluateCore(testCase);
        }
        catch (InvalidDataException)
        {
            return EngineResult.NotBoundBecause("a source document did not load");
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return EngineResult.NotBoundBecause("an unexpected exception: " + exception.GetType().Name);
        }
    }

    private EngineResult EvaluateCore(Qt3Case testCase)
    {
        var environment = _environments.Resolve(testCase);
        var query = testCase.QueryFile is null ? testCase.Query : File.ReadAllText(testCase.QueryFile);

        var statics = Statics(environment, hasResult: testCase.ResultVariable is not null);
        var compiled = XPathExpression.TryCompile(query, statics);
        if (!compiled.Success)
        {
            return Failure(compiled.Diagnostic);
        }

        using var context = new XPathEvaluationContext
        {
            ResourceResolver = new Qt3SourceResolver(environment.Sources),
        };
        var variables = Bind(environment, context);
        if (testCase.ResultVariable is { } items)
        {
            var sequence = new XdmItem[items.Count];
            for (var i = 0; i < sequence.Length; i++)
            {
                sequence[i] = ToItem(items[i]);
            }
            variables.Add((Result, XdmSequence.Create(sequence)));
        }
        foreach (var (name, value) in variables)
        {
            context.Variables.Set(name, value);
        }

        var result = compiled.Expression!.TryEvaluate(context);
        return result.Status == XPathEvaluationStatus.Ok ? Value(result.Value) : Failure(result.Diagnostic);
    }

    private static XPathStaticContext Statics(Qt3Environment environment, bool hasResult)
    {
        var builder = XPathStaticContext
            .Default.ToBuilder()
            .WithNamespace("xs", "http://www.w3.org/2001/XMLSchema")
            .WithNamespace("xsi", "http://www.w3.org/2001/XMLSchema-instance")
            .WithNamespace("fn", "http://www.w3.org/2005/xpath-functions");
        foreach (var binding in environment.Namespaces)
        {
            _ =
                binding.Prefix.Length == 0
                    ? builder.WithDefaultElementNamespace(binding.Uri)
                    : builder.WithNamespace(binding.Prefix, binding.Uri);
        }
        if (environment.StaticBaseUri is { } baseUri && Uri.TryCreate(baseUri, UriKind.Absolute, out var uri))
        {
            _ = builder.WithBaseUri(uri);
        }
        foreach (var source in environment.Sources)
        {
            if (IsVariableRole(source.Role))
            {
                _ = builder.WithVariable(VariableName(source.Role[1..], environment));
            }
        }
        foreach (var parameter in environment.Params)
        {
            _ = builder.WithVariable(VariableName(parameter.Name, environment));
        }
        return (hasResult ? builder.WithVariable(Result) : builder).Build();
    }

    // The sources bind the context item and the variables; the parameters are then evaluated with that context item, before any variable is set.
    private List<(XPathQName Name, XdmSequence Value)> Bind(Qt3Environment environment, XPathEvaluationContext context)
    {
        var variables = new List<(XPathQName, XdmSequence)>();
        foreach (var source in environment.Sources)
        {
            var document = XdmItem.FromNode(new XPathNode(LoadOrThrow(source).Document));
            if (string.Equals(source.Role, ".", StringComparison.Ordinal))
            {
                context.SetContextItem(document);
            }
            else if (IsVariableRole(source.Role))
            {
                variables.Add((VariableName(source.Role[1..], environment), XdmSequence.Create(document)));
            }
        }

        var statics = Statics(environment, hasResult: false);
        foreach (var parameter in environment.Params)
        {
            if (parameter.Select is not null)
            {
                var select = XPathExpression.Compile(parameter.Select, statics);
                variables.Add((VariableName(parameter.Name, environment), Copy(select.Evaluate(context))));
            }
        }
        return variables;
    }

    private XmlDocumentTree LoadOrThrow(Qt3Source source)
    {
        try
        {
            return Load(source);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            throw new InvalidDataException("The source document " + source.File + " did not load.", exception);
        }
    }

    private XmlDocumentTree Load(Qt3Source source) =>
        _documents
            .GetOrAdd(source.File + "\n" + source.Uri, _ => new Lazy<XmlDocumentTree>(
                () =>
                    XmlDocumentLoader.LoadFromMemory(
                        File.ReadAllBytes(source.File),
                        XmlReaderOptions.Default.WithDtdProcessing(true),
                        source.Uri is not null && Uri.TryCreate(source.Uri, UriKind.Absolute, out var uri) ? uri : null
                    ),
                LazyThreadSafetyMode.ExecutionAndPublication
            ))
            .Value;

    private static bool IsVariableRole(string role) => role.Length > 0 && role[0] == '$';

    private static XPathQName VariableName(string name, Qt3Environment environment)
    {
        if (name.StartsWith("Q{", StringComparison.Ordinal))
        {
            var end = name.IndexOf('}', StringComparison.Ordinal);
            return new XPathQName(name[2..end], name[(end + 1)..]);
        }

        var colon = name.IndexOf(':', StringComparison.Ordinal);
        if (colon > 0)
        {
            var prefix = name[..colon];
            foreach (var binding in environment.Namespaces)
            {
                if (string.Equals(binding.Prefix, prefix, StringComparison.Ordinal))
                {
                    return new XPathQName(binding.Uri, name[(colon + 1)..]);
                }
            }
        }
        return new XPathQName(string.Empty, name);
    }

    private static XdmSequence Copy(XdmSequence value)
    {
        var items = new XdmItem[value.Count];
        for (var i = 0; i < items.Length; i++)
        {
            items[i] = value[i];
        }
        return XdmSequence.Create(items);
    }

    private static EngineResult Value(XdmSequence value)
    {
        var items = new object[value.Count];
        for (var i = 0; i < items.Length; i++)
        {
            var item = value[i];
            items[i] =
                item.TryGetBoolean(out var boolean) ? boolean
                : item.TypeId == XdmTypeId.XsString && item.TryGetString(out var text) ? text!
                : item;
        }
        return EngineResult.Value(items);
    }

    private static XdmItem ToItem(object item) =>
        item switch
        {
            XdmItem xdm => xdm,
            bool boolean => XdmItem.FromBoolean(boolean),
            string text => XdmItem.FromString(text),
            int integer => XdmItem.FromInteger(integer),
            long integer => XdmItem.FromInteger(integer),
            double number => XdmItem.FromDouble(number),
            _ => throw new ArgumentException("Unsupported item " + item.GetType(), nameof(item)),
        };

    // A diagnostic outside the specifications is no error result: error code="*" would pass it.
    private static EngineResult Failure(XPathDiagnostic diagnostic) =>
        diagnostic.Kind == XPathErrorKind.Engine
            ? EngineResult.NotBoundBecause("the evaluation hit an engine limit")
            : EngineResult.Error(new QName(diagnostic.QName.Namespace, diagnostic.QName.LocalName));
}
