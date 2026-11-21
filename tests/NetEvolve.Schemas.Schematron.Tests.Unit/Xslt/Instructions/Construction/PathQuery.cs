namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Construction;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Runs a real XPath path expression over a constructed tree, with the path lowerings of the XPath package.</summary>
internal static class PathQuery
{
    /// <summary>Selects the nodes of a path from a context node.</summary>
    /// <param name="context">The context item, a node.</param>
    /// <param name="path">The path, such as <c>/out/item</c> or <c>@a</c>.</param>
    /// <returns>The nodes in document order.</returns>
    internal static XdmSequence Select(in XdmItem context, string path)
    {
        if (!XPathParser.TryParse(path.AsSpan(), out var syntax, out var parseFailure))
        {
            throw new InvalidOperationException("The path does not parse: " + parseFailure.Code);
        }

        var registry = new LoweringRegistry();
        PathLowerings.Register(registry);
        if (
            !ExpressionCompiler.TryCompile(
                syntax!,
                path,
                XPathStaticContext.Default,
                registry,
                out var compiled,
                out var failure
            )
        )
        {
            throw new InvalidOperationException("The path does not compile: " + failure.Code);
        }

        using var evaluation = new XPathEvaluationContext();
        evaluation.SetContextItem(in context);
        return compiled!.Evaluate(evaluation);
    }

    /// <summary>Describes a node as <c>kind:name=string value</c>, such as <c>Element:out=text</c>.</summary>
    /// <param name="item">The item, a node.</param>
    /// <returns>The description.</returns>
    internal static string Describe(in XdmItem item)
    {
        _ = item.TryGetNode(out var node);
        var named =
            node.Node.Kind
            is XmlNodeKind.Element
                or XmlNodeKind.Attribute
                or XmlNodeKind.ProcessingInstruction
                or XmlNodeKind.Namespace;
        var name = named ? node.Tree!.Names.GetString(node.Node.Name.Local) : string.Empty;
        return $"{node.Kind}:{name}={node.Node.StringValue}";
    }
}
