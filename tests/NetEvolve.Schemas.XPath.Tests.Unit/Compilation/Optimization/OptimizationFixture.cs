namespace NetEvolve.Schemas.XPath.Tests.Unit.Compilation.Optimization;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Compilation.Optimization;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Compiles an expression with chosen rewrites and evaluates it over a tree that counts the nodes it reads.</summary>
internal static class OptimizationFixture
{
    /// <summary>Compiles an expression with the rewrites that are enabled.</summary>
    public static CompiledExpression Compile(string text, XPathOptimizations optimizations)
    {
        var result = XPathEngine.TryCompile(text, text, null, XPathEngine.AllSlots, optimizations);
        return result.Expression?.Compiled
            ?? throw new InvalidOperationException("The expression does not compile: " + result.Diagnostic.Code);
    }

    /// <summary>Compiles and evaluates an expression with the document of the tree as the context item.</summary>
    /// <returns>The rendered result, or the error code when evaluating raises one.</returns>
    public static string Run(string text, XPathOptimizations optimizations, ChildrenTree tree)
    {
        var compiled = Compile(text, optimizations);
        using var context = new XPathEvaluationContext();
        context.SetContextItem(XdmItem.FromNode(tree.At(0)));
        try
        {
            return Render(compiled.Evaluate(context), tree);
        }
        catch (XPathException failure)
        {
            return failure.Diagnostic.Code;
        }
    }

    /// <summary>Renders nodes as <c>#index</c>, other items by their string value, separated by a space.</summary>
    public static string Render(in XdmSequence sequence, ChildrenTree tree)
    {
        var text = new StringBuilder();
        for (var i = 0; i < sequence.Count; i++)
        {
            _ = text.Append(i == 0 ? string.Empty : " ");
            var item = sequence[i];
            if (item.TryGetNode(out var node))
            {
                _ = text.Append('#').Append(tree.IndexOf(node).ToString(CultureInfo.InvariantCulture));
            }
            else if (item.TryGetBoolean(out var boolean))
            {
                _ = text.Append(boolean ? "true" : "false");
            }
            else if (item.TryGetInteger(out var integer))
            {
                _ = text.Append(integer.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                _ = text.Append(item.ToString());
            }
        }

        return text.ToString();
    }

    /// <summary>
    /// A document with one element <c>r</c> that has a number of children <c>x</c>; the nodes are numbered in document order, from 0 for
    /// the document. Every read of the kind of a node is counted, which is what a name test does for each node it looks at.
    /// </summary>
    internal sealed class ChildrenTree : XmlTree
    {
        private readonly int _count;
        private readonly SpanInterningNameTable _names = new(100);
        private readonly XmlName _r;
        private readonly XmlName _x;

        public ChildrenTree(int children, bool internOtherFirst = false)
        {
            _count = children + 2;
            if (internOtherFirst)
            {
                _ = _names.Intern("padding");
            }

            _r = new XmlName(default, _names.Intern("r"), default);
            _x = new XmlName(default, _names.Intern("x"), default);
        }

        public int KindReads { get; private set; }

        public override XmlNameTable Names => _names;

        public override IReadOnlyList<XmlUnparsedEntity> UnparsedEntities => [];

        public override IReadOnlyList<XmlNotation> Notations => [];

        public XPathNode At(int index) => new(new XmlNode(this, index));

        public int IndexOf(in XPathNode node) => node.Node.Index;

        protected internal override XmlNodeKind GetKind(int index)
        {
            KindReads++;
            return index == 0 ? XmlNodeKind.Document : XmlNodeKind.Element;
        }

        protected internal override XmlName GetName(int index) => index == 1 ? _r : _x;

        protected internal override int GetParent(int index) =>
            index == 0 ? NoNode
            : index == 1 ? 0
            : 1;

        protected internal override int GetFirstChild(int index) =>
            index == 0 ? 1
            : index == 1 && _count > 2 ? 2
            : NoNode;

        protected internal override int GetNextSibling(int index) =>
            index > 1 && index + 1 < _count ? index + 1 : NoNode;

        protected internal override int GetFirstAttribute(int index) => NoNode;

        protected internal override int GetNextAttribute(int index) => NoNode;

        protected internal override int GetFirstNamespace(int index) => NoNode;

        protected internal override int GetNextNamespace(int index) => NoNode;

        protected internal override string GetStringValue(int index) => string.Empty;

        protected internal override Uri? GetBaseUri(int index) => null;

        protected internal override Uri? GetDocumentUri(int index) => null;

        protected internal override int CompareDocumentOrder(int first, int second) => first.CompareTo(second);
    }
}
