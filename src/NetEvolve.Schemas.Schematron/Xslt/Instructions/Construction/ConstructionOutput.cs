namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Construction;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model.Traversal;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// Where the construction instructions write: into the content of an element or document (a <see cref="ContentWriter"/>), into the
/// tree of another tree sink, or into a temporary tree of their own that an item sink returns as a node (design #150 EP-8). Nodes are
/// built only through the tree events of the sink and copied only through the Xml pump.
/// </summary>
/// <remarks>
/// Limitations: the Xml tree builder starts every temporary tree with a document node, so an element, comment, processing instruction,
/// or text node that an item sink returns is the child of a document node, not parentless; an attribute or namespace node cannot be
/// returned by an item sink at all and is the unsupported construct error (#814). A sink that has no tree (the text of a message) gets
/// the same error for any node that needs one.
/// </remarks>
internal static class ConstructionOutput
{
    /// <summary>Reports a dynamic error of an instruction: NES3301 with the code of the rule in the constraint.</summary>
    /// <param name="frame">The frame that records the problem.</param>
    /// <param name="location">The location of the instruction.</param>
    /// <param name="constraint">The code of the XSLT rule, such as <c>XTDE0820</c>.</param>
    /// <param name="message">The message.</param>
    /// <returns><see cref="QueryStatus.Problem"/>.</returns>
    internal static QueryStatus Error(XsltFrame frame, in DocumentLocation location, string constraint, string message)
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(constraint);
        Argument.ThrowIfNull(message);

        return frame.Fail(
            new SchematronProblem(SchematronDiagnosticCodes.EvaluationFailed, message, location, constraint)
        );
    }

    /// <summary>Reports a construct that the sink cannot take as the unsupported XSLT construct error.</summary>
    /// <param name="frame">The frame that records the problem.</param>
    /// <param name="location">The location of the instruction.</param>
    /// <param name="message">The message.</param>
    /// <returns><see cref="QueryStatus.Problem"/>.</returns>
    internal static QueryStatus Unsupported(XsltFrame frame, in DocumentLocation location, string message)
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(message);

        return frame.Fail(new SchematronProblem(SchematronDiagnosticCodes.XsltUnsupported, message, location));
    }

    /// <summary>Interns a name part in the table of the tree under construction.</summary>
    /// <param name="names">The table.</param>
    /// <param name="text">The text.</param>
    /// <param name="symbol">The handle when the result is <see langword="true"/>.</param>
    /// <param name="problem">The problem, without a location, when the result is <see langword="false"/>.</param>
    /// <returns><see langword="false"/> when the table is frozen or full.</returns>
    internal static bool TryIntern(XmlNameTable names, string text, out XmlSymbol symbol, out SchematronProblem problem)
    {
        Argument.ThrowIfNull(names);
        Argument.ThrowIfNull(text);

        var status = names.TryIntern(text.AsSpan(), out symbol);
        if (status == XmlNameTableStatus.Ok)
        {
            problem = default;
            return true;
        }

        problem =
            status == XmlNameTableStatus.Frozen
                ? new SchematronProblem(
                    SchematronDiagnosticCodes.EvaluationFailed,
                    $"The name table of the temporary tree is frozen and does not contain '{text}'.",
                    default
                )
                : new SchematronProblem(
                    SchematronDiagnosticCodes.EvaluationLimitStop,
                    "The temporary tree exceeds the maximum number of distinct names.",
                    default
                );
        return false;
    }

    /// <summary>Copies a node and its descendants into a tree; a document node contributes its children.</summary>
    /// <param name="node">The node.</param>
    /// <param name="target">The events of the tree.</param>
    internal static void Pump(XmlNode node, IXmlSink target)
    {
        Argument.ThrowIfNull(target);

        if (node.Kind != XmlNodeKind.Document)
        {
            XmlTreePump.Pump(node, target);
            return;
        }

        for (var child = node.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            XmlTreePump.Pump(child, target);
        }
    }

    /// <summary>Gets the tree that a construction writes into: the tree of a tree sink, or a new temporary tree of an item sink.</summary>
    /// <param name="frame">The frame that records the problem.</param>
    /// <param name="sink">The sink of the instruction.</param>
    /// <param name="location">The location of the instruction.</param>
    /// <param name="target">The events of the tree when the result is <see langword="true"/>.</param>
    /// <param name="scope">The namespace bindings in scope at the place in the tree.</param>
    /// <param name="owned">The temporary tree that the caller must end with <see cref="Complete"/>, or <see langword="null"/> when the sink owns the tree.</param>
    /// <returns><see langword="false"/> when the sink has no tree and the problem was recorded.</returns>
    internal static bool TryOpen(
        XsltFrame frame,
        ISequenceSink sink,
        in DocumentLocation location,
        out IXmlSink target,
        out NamespaceScope scope,
        out IXmlSink? owned
    )
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(sink);

        scope = NamespaceScope.Empty;
        owned = null;
        if (sink.Kind == SequenceSinkKind.Tree)
        {
            target = sink.Content;
            if (sink is ContentWriter writer)
            {
                scope = writer.ChildScope;
            }

            return true;
        }

        target = null!;
        if (!TryBeginTree(frame, sink, location, out var tree))
        {
            return false;
        }

        owned = tree;
        target = tree;
        return true;
    }

    /// <summary>Begins a temporary tree; a sink that has no tree answers with the unsupported construct error instead of an exception.</summary>
    /// <param name="frame">The frame that records the problem.</param>
    /// <param name="sink">The sink.</param>
    /// <param name="location">The location of the instruction.</param>
    /// <param name="tree">The events of the tree when the result is <see langword="true"/>.</param>
    /// <returns><see langword="false"/> when the problem was recorded.</returns>
    internal static bool TryBeginTree(
        XsltFrame frame,
        ISequenceSink sink,
        in DocumentLocation location,
        out IXmlSink tree
    )
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(sink);

        tree = null!;
        if (sink.Kind != SequenceSinkKind.Text)
        {
            try
            {
                tree = sink.BeginTree();
                return true;
            }
            catch (InvalidOperationException)
            {
                // A sink of another kind may forward to a text sink, as the join sink of xsl:value-of does (#821).
            }
        }

        _ = Unsupported(
            frame,
            location,
            "A node cannot be constructed where only text is built (limitation); the sink has no tree."
        );
        return false;
    }

    /// <summary>Ends a temporary tree and pushes the node that it holds.</summary>
    /// <param name="sink">The item sink that began the tree.</param>
    /// <param name="owned">The tree, or <see langword="null"/> when the sink owns the tree and there is nothing to do.</param>
    /// <param name="push">Whether to push the node, <see langword="false"/> after a failure that only releases the tree.</param>
    internal static void Complete(ISequenceSink sink, IXmlSink? owned, bool push)
    {
        Argument.ThrowIfNull(sink);

        if (owned is null)
        {
            return;
        }

        var document = sink.EndTree(owned);
        if (!push)
        {
            return;
        }

        // The node that was built is the first child of the document node of the temporary tree.
        _ = document.TryGetNode(out var node);
        sink.Item(XdmItem.FromNode(new XPathNode(node.Node.FirstChild)));
    }

    /// <summary>Reports the problem that a sink recorded, located at the instruction that wrote last.</summary>
    /// <param name="frame">The frame that records the problem.</param>
    /// <param name="sink">The sink that received the push.</param>
    /// <param name="location">The location of the instruction.</param>
    /// <returns><see cref="QueryStatus.Problem"/> when the sink recorded a problem, otherwise <see cref="QueryStatus.Ok"/>.</returns>
    internal static QueryStatus Surface(XsltFrame frame, ISequenceSink sink, in DocumentLocation location)
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(sink);

        var problem = sink switch
        {
            ContentWriter writer => writer.Problem,
            TreeContentSink tree => tree.Problem,
            _ => null,
        };
        return problem is { } found
            ? frame.Fail(new SchematronProblem(found.Code, found.Message, location, found.Constraint))
            : QueryStatus.Ok;
    }

    /// <summary>Pushes an attribute node as content.</summary>
    /// <param name="frame">The frame that records the problem.</param>
    /// <param name="sink">The sink of the instruction.</param>
    /// <param name="attribute">The attribute: name as requested and value.</param>
    /// <param name="location">The location of the instruction.</param>
    /// <returns>The status.</returns>
    internal static QueryStatus PushAttribute(
        XsltFrame frame,
        ISequenceSink sink,
        in PendingAttribute attribute,
        in DocumentLocation location
    )
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(sink);

        if (sink is ContentWriter writer)
        {
            _ = writer.TryAddAttribute(attribute.Prefix, attribute.Local, attribute.Uri, attribute.Value);
            return Surface(frame, sink, location);
        }

        return NotElementContent(frame, sink, location, "An attribute node");
    }

    /// <summary>Pushes a namespace node as content.</summary>
    /// <param name="frame">The frame that records the problem.</param>
    /// <param name="sink">The sink of the instruction.</param>
    /// <param name="binding">The namespace node: prefix and URI.</param>
    /// <param name="location">The location of the instruction.</param>
    /// <returns>The status.</returns>
    internal static QueryStatus PushNamespace(
        XsltFrame frame,
        ISequenceSink sink,
        in NamespaceBinding binding,
        in DocumentLocation location
    )
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(sink);

        if (sink is ContentWriter writer)
        {
            _ = writer.TryAddNamespace(binding.Prefix, binding.Uri);
            return Surface(frame, sink, location);
        }

        return NotElementContent(frame, sink, location, "A namespace node");
    }

    /// <summary>Pushes a comment node.</summary>
    /// <param name="frame">The frame that records the problem.</param>
    /// <param name="sink">The sink of the instruction.</param>
    /// <param name="value">The text of the comment.</param>
    /// <param name="location">The location of the instruction.</param>
    /// <returns>The status.</returns>
    internal static QueryStatus PushComment(
        XsltFrame frame,
        ISequenceSink sink,
        string value,
        in DocumentLocation location
    )
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(sink);
        Argument.ThrowIfNull(value);

        if (!TryOpen(frame, sink, location, out var target, out _, out var owned))
        {
            return QueryStatus.Problem;
        }

        target.Comment(value.AsSpan());
        Complete(sink, owned, true);
        return QueryStatus.Ok;
    }

    /// <summary>Pushes a processing instruction node.</summary>
    /// <param name="frame">The frame that records the problem.</param>
    /// <param name="sink">The sink of the instruction.</param>
    /// <param name="name">The target of the instruction.</param>
    /// <param name="value">The data of the instruction.</param>
    /// <param name="location">The location of the instruction.</param>
    /// <returns>The status.</returns>
    internal static QueryStatus PushProcessingInstruction(
        XsltFrame frame,
        ISequenceSink sink,
        string name,
        string value,
        in DocumentLocation location
    )
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(sink);
        Argument.ThrowIfNull(name);
        Argument.ThrowIfNull(value);

        if (!TryOpen(frame, sink, location, out var target, out _, out var owned))
        {
            return QueryStatus.Problem;
        }

        if (!TryIntern(target.Names, name, out var symbol, out var problem))
        {
            Complete(sink, owned, false);
            return frame.Fail(new SchematronProblem(problem.Code, problem.Message, location, problem.Constraint));
        }

        target.ProcessingInstruction(symbol, value.AsSpan());
        Complete(sink, owned, true);
        return QueryStatus.Ok;
    }

    /// <summary>Pushes a copy of a node: a tree sink copies it through the pump, an item sink gets a copy in a temporary tree.</summary>
    /// <param name="frame">The frame that records the problem.</param>
    /// <param name="sink">The sink of the instruction.</param>
    /// <param name="item">The node.</param>
    /// <param name="location">The location of the instruction.</param>
    /// <returns>The status.</returns>
    internal static QueryStatus PushCopy(
        XsltFrame frame,
        ISequenceSink sink,
        in XdmItem item,
        in DocumentLocation location
    )
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(sink);

        if (sink.Kind != SequenceSinkKind.Items)
        {
            sink.Item(in item);
            return Surface(frame, sink, location);
        }

        _ = item.TryGetNode(out var node);
        var xml = node.Node;
        if (xml.Kind is XmlNodeKind.Attribute or XmlNodeKind.Namespace)
        {
            return NotElementContent(
                frame,
                sink,
                location,
                xml.Kind == XmlNodeKind.Attribute ? "An attribute node" : "A namespace node"
            );
        }

        if (!TryBeginTree(frame, sink, location, out var tree))
        {
            return QueryStatus.Problem;
        }

        Pump(xml, tree);
        if (xml.Kind == XmlNodeKind.Document)
        {
            sink.Item(sink.EndTree(tree));
            return QueryStatus.Ok;
        }

        Complete(sink, tree, true);
        return QueryStatus.Ok;
    }

    // A tree sink that is not the content of an element is the content of a document; an item sink would have to return a parentless node.
    private static QueryStatus NotElementContent(
        XsltFrame frame,
        ISequenceSink sink,
        in DocumentLocation location,
        string what
    ) =>
        sink.Kind == SequenceSinkKind.Tree
            ? Error(frame, location, "XTDE0420", what + " cannot be content of a document node.")
            : Unsupported(
                frame,
                location,
                what + " cannot be constructed outside the content of an element yet (limitation, #814)."
            );
}
