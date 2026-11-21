namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// Builds one element or document from its parts: the shared run of the literal result element, <c>xsl:element</c>, <c>xsl:copy</c>,
/// and <c>xsl:document</c> (XSLT 2.0 section 5.7.1). The namespace nodes and attributes that the instruction itself provides go into
/// the <see cref="ContentWriter"/> first, then the content runs into it.
/// </summary>
internal static class ElementConstruction
{
    /// <summary>Builds an element and pushes it into the sink.</summary>
    /// <param name="frame">The frame.</param>
    /// <param name="sink">The sink of the instruction.</param>
    /// <param name="location">The location of the instruction, for a problem.</param>
    /// <param name="name">The name of the element.</param>
    /// <param name="namespaces">The namespace nodes of the instruction.</param>
    /// <param name="attributes">The attributes of the instruction; the content may add more.</param>
    /// <param name="body">The content.</param>
    /// <returns>The status; the problem is read with <see cref="XsltFrame.TakeProblem"/>.</returns>
    internal static QueryStatus Construct(
        XsltFrame frame,
        ISequenceSink sink,
        in DocumentLocation location,
        in ExpandedName name,
        NamespaceBinding[] namespaces,
        PendingAttribute[] attributes,
        XsltExecutable body
    )
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(sink);
        Argument.ThrowIfNull(body);
        Argument.ThrowIfNull(namespaces);
        Argument.ThrowIfNull(attributes);

        if (!ConstructionOutput.TryOpen(frame, sink, location, out var target, out var scope, out var owned))
        {
            return QueryStatus.Problem;
        }

        var writer = ContentWriter.ForElement(sink, target, scope, name.Prefix, name.Local, name.Uri);
        foreach (var binding in namespaces)
        {
            _ = writer.TryAddNamespace(binding.Prefix, binding.Uri);
        }

        foreach (var attribute in attributes)
        {
            _ = writer.TryAddAttribute(attribute.Prefix, attribute.Local, attribute.Uri, attribute.Value);
        }

        var status = body.TryExecute(frame, writer);
        writer.Finish();
        ConstructionOutput.Complete(sink, owned, status == QueryStatus.Ok && writer.Problem is null);
        if (status == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        return writer.Problem is { } problem
            ? frame.Fail(new SchematronProblem(problem.Code, problem.Message, location, problem.Constraint))
            : QueryStatus.Ok;
    }

    /// <summary>Builds a document node from the content and pushes it into the sink; a tree sink adds its children.</summary>
    /// <param name="frame">The frame.</param>
    /// <param name="sink">The sink of the instruction.</param>
    /// <param name="location">The location of the instruction, for a problem.</param>
    /// <param name="body">The content.</param>
    /// <returns>The status; the problem is read with <see cref="XsltFrame.TakeProblem"/>.</returns>
    internal static QueryStatus ConstructDocument(
        XsltFrame frame,
        ISequenceSink sink,
        in DocumentLocation location,
        XsltExecutable body
    )
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(sink);
        Argument.ThrowIfNull(body);

        if (!ConstructionOutput.TryBeginTree(frame, sink, location, out var tree))
        {
            return QueryStatus.Problem;
        }

        var writer = ContentWriter.ForDocument(sink, tree);
        var status = body.TryExecute(frame, writer);
        var document = sink.EndTree(tree);
        if (status == QueryStatus.Problem)
        {
            return QueryStatus.Problem;
        }

        if (writer.Problem is { } problem)
        {
            return frame.Fail(new SchematronProblem(problem.Code, problem.Message, location, problem.Constraint));
        }

        sink.Item(in document);
        return ConstructionOutput.Surface(frame, sink, location);
    }
}
