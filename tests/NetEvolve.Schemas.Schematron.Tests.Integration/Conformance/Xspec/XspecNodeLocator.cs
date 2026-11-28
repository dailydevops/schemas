namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Xspec;

using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Model.Loading;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Selects the node an XPath location names in the instance document, to compare two locations by node identity.</summary>
internal sealed class XspecNodeLocator
{
    private readonly XmlDocumentTree _tree;

    private XspecNodeLocator(XmlDocumentTree tree) => _tree = tree;

    /// <summary>Loads the instance document.</summary>
    /// <param name="instance">The encoded instance document.</param>
    /// <returns>The locator.</returns>
    public static XspecNodeLocator Load(ReadOnlyMemory<byte> instance) =>
        new(XmlDocumentLoader.LoadFromMemory(instance));

    /// <summary>Determines whether two location expressions select the same single node.</summary>
    /// <param name="left">The first expression.</param>
    /// <param name="leftNamespaces">The namespace bindings of the first expression.</param>
    /// <param name="right">The second expression.</param>
    /// <param name="rightNamespaces">The namespace bindings of the second expression.</param>
    /// <param name="problem">Why an expression does not select exactly one node; <see langword="null"/> otherwise.</param>
    /// <returns><see langword="true"/> when both select the same node.</returns>
    public bool Same(
        string left,
        IReadOnlyDictionary<string, string> leftNamespaces,
        string right,
        IReadOnlyDictionary<string, string> rightNamespaces,
        out string? problem
    )
    {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        var first = Select(left, leftNamespaces, out problem);
        if (problem is not null)
        {
            return false;
        }

        var second = Select(right, rightNamespaces, out problem);
        return problem is null && first.IsSameNode(second);
    }

    private XPathNode Select(string expression, IReadOnlyDictionary<string, string> namespaces, out string? problem)
    {
        var builder = XPathStaticContext.Default.ToBuilder();
        foreach (var (prefix, uri) in namespaces)
        {
            _ = builder.WithNamespace(prefix, uri);
        }

        var compiled = XPathExpression.TryCompile(expression, builder.Build());
        if (!compiled.Success)
        {
            problem = $"The location '{expression}' does not compile: {compiled.Diagnostic.Message}";
            return default;
        }

        using var context = new XPathEvaluationContext();
        context.SetContextItem(XdmItem.FromNode(new XPathNode(_tree.Document)));
        var result = compiled.Expression!.TryEvaluate(context);
        if (result.Status != XPathEvaluationStatus.Ok)
        {
            problem = $"The location '{expression}' does not evaluate: {result.Diagnostic.Message}";
            return default;
        }

        if (result.Value.Count != 1 || !result.Value[0].TryGetNode(out var node))
        {
            problem = $"The location '{expression}' does not select exactly one node.";
            return default;
        }

        problem = null;
        return node;
    }
}
