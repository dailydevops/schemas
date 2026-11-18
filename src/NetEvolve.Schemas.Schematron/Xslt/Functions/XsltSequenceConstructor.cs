namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// Compiles a sequence constructor (XSLT 2.0 section 5.7): the children of an element, from a first child on, in order. A local
/// <c>xsl:variable</c> is visible to the later siblings and everything below them, so every child is compiled in a scope that names
/// the variables declared before it. A parent instruction with a sequence constructor of its own uses this class for its content.
/// </summary>
/// <remarks>
/// Limitation: character data other than white space in a sequence constructor creates a text node, which the instructions of
/// SCH-82 (#255) and SCH-67 (#253) provide; until then it is reported as not supported. Comments and processing instructions are ignored.
/// </remarks>
internal static class XsltSequenceConstructor
{
    private const string ParamLocalName = "param";
    private const string VariableLocalName = "variable";

    /// <summary>Compiles the children; every problem is reported through the context, and the compile continues after one.</summary>
    /// <param name="context">The compile context of the parent; it compiles the children in the scope given here.</param>
    /// <param name="first">The first child to compile; an empty node for no content.</param>
    /// <param name="names">The visible variable names, slot by slot; the names of the local variables are appended in order.</param>
    /// <param name="body">The compiled sequence constructor when the result is <see langword="true"/>.</param>
    /// <returns><see langword="true"/> when every child compiled.</returns>
    internal static bool TryCompile(
        XsltCompileContext context,
        XmlNode first,
        List<string> names,
        out XsltBodyExecutable body
    )
    {
        Argument.ThrowIfNull(context);
        Argument.ThrowIfNull(names);

        body = null!;
        var instructions = new List<XsltExecutable>();
        var ok = true;
        for (var node = first; !node.IsEmpty; node = node.NextSibling)
        {
            ok &= node.Kind switch
            {
                XmlNodeKind.Text => TryAcceptText(context, node),
                XmlNodeKind.Element => TryCompileElement(context, node, names, instructions),
                _ => true,
            };
        }

        if (ok)
        {
            body = new XsltBodyExecutable(instructions.ToArray());
        }

        return ok;
    }

    private static bool TryAcceptText(XsltCompileContext context, XmlNode text)
    {
        if (XsltElementReader.IsWhitespace(text.StringValue))
        {
            return true;
        }

        XsltProblems.Report(
            context.Problems,
            SchematronDiagnosticCodes.XsltUnsupported,
            "Text in a sequence constructor is not supported yet.",
            new XPathNode(text.Parent)
        );
        return false;
    }

    private static bool TryCompileElement(
        XsltCompileContext context,
        XmlNode node,
        List<string> names,
        List<XsltExecutable> instructions
    )
    {
        var element = new XPathNode(node);
        if (XsltElementReader.IsXslt(node, ParamLocalName))
        {
            XsltProblems.Report(
                context.Problems,
                SchematronDiagnosticCodes.ElementMisplaced,
                "The XSLT element 'xsl:param' is not allowed after the first instruction of a function; it is allowed as a child at the start.",
                element,
                "XTSE0010"
            );
            return false;
        }

        var outer = context.Scope;
        var scope = new QueryStaticScope(outer.Namespaces, names.ToArray(), outer.BaseUri, outer.Edition);
        var compiled = context.CompileChildIn(element, scope);
        if (compiled is not null)
        {
            instructions.Add(compiled);
        }

        if (XsltElementReader.IsXslt(node, VariableLocalName))
        {
            // A failed variable still takes its slot, so the slots of the later ones stay where the compile expects them.
            names.Add(XsltElementReader.NameOrEmpty(element));
        }

        return compiled is not null;
    }
}
