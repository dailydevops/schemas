namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Engine;

using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The SVRL output of one validation, loaded once so that the assertions of a case can be evaluated against it.</summary>
/// <remarks>
/// <para>
/// The suite states what the SVRL output must contain as XPath assertions on the SVRL document, not as expected SVRL files. The comparison is
/// therefore the evaluation of these assertions on the real XPath engine, and it ignores exactly what XPath does not see: the order of
/// attributes, white space between elements, and the prefix of the SVRL namespace. Text and structure are significant, so a missing fired
/// assertion fails the case.
/// </para>
/// <para>
/// Limitations: the prefix <c>svrl</c> is the only one declared for the assertions, because the case reader keeps the text of an assertion
/// and not the namespace declarations of its element, and every assertion of the pinned suite uses this prefix only; an assertion is
/// evaluated as <c>boolean(...)</c>, so a node sequence holds when it is not empty.
/// </para>
/// </remarks>
internal sealed class SvrlReport
{
    private const string SvrlNamespace = "http://purl.oclc.org/dsdl/svrl";

    private static readonly XPathStaticContext Statics = XPathStaticContext
        .Default.ToBuilder()
        .WithNamespace("svrl", SvrlNamespace)
        .WithNamespace("xs", "http://www.w3.org/2001/XMLSchema")
        .Build();

    private readonly XmlDocumentTree _tree;

    private SvrlReport(XmlDocumentTree tree) => _tree = tree;

    /// <summary>Gets a value indicating whether the output holds an <c>svrl:error</c>.</summary>
    public bool HasError => Holds("//svrl:error");

    /// <summary>Loads the SVRL output.</summary>
    /// <param name="svrl">The encoded SVRL document.</param>
    /// <returns>The report.</returns>
    /// <exception cref="InvalidDataException">The document is not well formed.</exception>
    public static SvrlReport Load(ReadOnlyMemory<byte> svrl)
    {
        try
        {
            return new SvrlReport(XmlDocumentLoader.LoadFromMemory(svrl));
        }
        catch (XmlParseException exception)
        {
            throw new InvalidDataException("The SVRL output is not well formed: " + exception.Message, exception);
        }
    }

    /// <summary>Evaluates an XPath expression on the document node and takes its effective boolean value.</summary>
    /// <param name="xpath">The expression; the prefix <c>svrl</c> is declared.</param>
    /// <returns>The effective boolean value.</returns>
    /// <exception cref="InvalidDataException">The expression does not compile or does not evaluate.</exception>
    public bool Holds(string xpath)
    {
        ArgumentNullException.ThrowIfNull(xpath);
        var compiled = XPathExpression.TryCompile("boolean(" + xpath + ")", Statics);
        if (!compiled.Success)
        {
            throw new InvalidDataException(
                $"The expression '{xpath}' does not compile: {compiled.Diagnostic.Code} {compiled.Diagnostic.Message}"
            );
        }

        using var context = new XPathEvaluationContext();
        context.SetContextItem(XdmItem.FromNode(new XPathNode(_tree.Document)));
        var result = compiled.Expression!.TryEvaluate(context);
        return
            result.Status == XPathEvaluationStatus.Ok
            && result.Value.Count == 1
            && result.Value[0].TryGetBoolean(out var holds)
            ? holds
            : throw new InvalidDataException(
                $"The expression '{xpath}' does not evaluate: {result.Diagnostic.Code} {result.Diagnostic.Message}"
            );
    }

    /// <summary>Evaluates the assertions of a case.</summary>
    /// <param name="assertions">The XPath assertions.</param>
    /// <returns>One text per assertion that does not hold, does not compile, or does not evaluate; empty when every assertion holds.</returns>
    public IReadOnlyList<string> Check(IReadOnlyList<string> assertions)
    {
        ArgumentNullException.ThrowIfNull(assertions);
        var failures = new List<string>();
        foreach (var assertion in assertions)
        {
            try
            {
                if (!Holds(assertion))
                {
                    failures.Add($"The SVRL does not satisfy '{assertion}'.");
                }
            }
            catch (InvalidDataException exception)
            {
                failures.Add(exception.Message);
            }
        }

        return failures;
    }
}
