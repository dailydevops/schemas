namespace NetEvolve.Schemas.Schematron.Xslt.Registry;

using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// Turns an element of a stylesheet into its registry entry, or into the located problem of the situations of design #150
/// EP-9: an unregistered XSLT element is NES3204, a registered one in the wrong position is NES3003, and an element outside
/// the XSLT namespace uses the entry of the literal result element, which is NES3204 while none is registered.
/// </summary>
/// <remarks>
/// The position of an element is its start tag; the Xml package delivers no position for attributes or for trees built
/// without positions, where line and column are zero.
/// </remarks>
internal static class XsltElementResolver
{
    private const string MisplacedConstraint = "XTSE0010";

    /// <summary>Resolves an element; on failure <paramref name="problem"/> is set.</summary>
    /// <param name="registry">The registry.</param>
    /// <param name="element">The element node, or a text node of a sequence constructor.</param>
    /// <param name="where">The position the compiler is filling.</param>
    /// <param name="moduleSystemId">The system identifier of the module that holds the element, or <see langword="null"/> for the document URI of the tree.</param>
    /// <param name="instruction">The entry when the result is <see langword="true"/>.</param>
    /// <param name="problem">The problem when the result is <see langword="false"/>.</param>
    /// <returns><see langword="true"/> when an entry was found for the position.</returns>
    internal static bool TryResolve(
        IXsltInstructionRegistry registry,
        XPathNode element,
        XsltPosition where,
        string? moduleSystemId,
        out IXsltInstruction instruction,
        out SchematronProblem problem
    )
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(element.Tree);
        if (element.Kind == XdmNodeKind.Text)
        {
            return TryResolveText(registry, element, moduleSystemId, out instruction, out problem);
        }

        ArgumentOutOfRangeException.ThrowIfNotEqual((int)element.Kind, (int)XdmNodeKind.Element);

        var names = element.Tree.Names;
        var xmlName = element.Node.Name;
        var local = names.GetString(xmlName.Local);
        var namespaceUri = xmlName.NamespaceUri.IsEmpty ? string.Empty : names.GetString(xmlName.NamespaceUri);
        var name = new XPathQName(namespaceUri, local);

        switch (registry.Classify(in name, where, out instruction))
        {
            case XsltElementClass.Registered:
                problem = default;
                return true;
            case XsltElementClass.NotXslt:
                if (registry.TryGetSynthetic(XsltSyntheticElement.LiteralResultElement, out instruction))
                {
                    problem = default;
                    return true;
                }

                problem = new SchematronProblem(
                    SchematronDiagnosticCodes.XsltUnsupported,
                    "The XSLT construct 'literal result element' is not supported.",
                    LocationOf(element, moduleSystemId)
                );
                return false;
            case XsltElementClass.Misplaced:
                problem = new SchematronProblem(
                    SchematronDiagnosticCodes.ElementMisplaced,
                    $"The XSLT element 'xsl:{local}' is not allowed as {Describe(where)}; it is allowed as {Describe(instruction.Position)}.",
                    LocationOf(element, moduleSystemId),
                    MisplacedConstraint
                );
                return false;
            default:
                if (
                    (where & XsltPosition.Instruction) != XsltPosition.None
                    && !XsltKnownElements.Contains(local)
                    && HasFallback(element)
                    && registry.TryGetSynthetic(XsltSyntheticElement.UnknownInstruction, out instruction)
                )
                {
                    problem = default;
                    return true;
                }

                problem = new SchematronProblem(
                    SchematronDiagnosticCodes.XsltUnsupported,
                    $"The XSLT element 'xsl:{local}' is not supported.",
                    LocationOf(element, moduleSystemId)
                );
                return false;
        }
    }

    // Character data of a sequence constructor has no name: the registry holds one synthetic entry for it. The problem is located at the
    // element that holds the text, because a text node has no start tag.
    private static bool TryResolveText(
        IXsltInstructionRegistry registry,
        XPathNode text,
        string? moduleSystemId,
        out IXsltInstruction instruction,
        out SchematronProblem problem
    )
    {
        if (registry.TryGetSynthetic(XsltSyntheticElement.TextNode, out instruction))
        {
            problem = default;
            return true;
        }

        problem = new SchematronProblem(
            SchematronDiagnosticCodes.XsltUnsupported,
            "The XSLT construct 'text node' is not supported.",
            LocationOf(new XPathNode(text.Node.Parent), moduleSystemId)
        );
        return false;
    }

    // An element that XSLT does not define is replaced by its xsl:fallback children (XSLT 2.0 section 18.2).
    private static bool HasFallback(XPathNode element)
    {
        for (var child = element.Node.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            if (XsltElementReader.IsXslt(child, "fallback"))
            {
                return true;
            }
        }

        return false;
    }

    private static string Describe(XsltPosition position)
    {
        var text = string.Empty;
        text = Append(text, position, XsltPosition.Instruction, "an instruction");
        text = Append(text, position, XsltPosition.Declaration, "a declaration");
        text = Append(text, position, XsltPosition.Child, "a child of a parent instruction");
        return text.Length == 0 ? "nothing" : text;
    }

    private static string Append(string text, XsltPosition position, XsltPosition flag, string description) =>
        (position & flag) == XsltPosition.None ? text
        : text.Length == 0 ? description
        : text + " or " + description;

    internal static DocumentLocation LocationOf(XPathNode element, string? moduleSystemId)
    {
        var position = element.Tree is XmlDocumentTree tree ? tree.GetPosition(element.Node.Index) : default;
        return new DocumentLocation(
            moduleSystemId ?? element.Node.DocumentUri?.OriginalString,
            position.Line,
            position.Column
        );
    }
}
