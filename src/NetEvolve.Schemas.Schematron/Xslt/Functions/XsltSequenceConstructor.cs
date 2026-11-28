namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// Compiles a sequence constructor (XSLT 2.0 section 5.7): the children of an element, from a first child on, in order. A local
/// <c>xsl:variable</c> is visible to the later siblings and everything below them, so every child is compiled in a scope that names
/// the variables declared before it. A parent instruction with a sequence constructor of its own uses this class for its content.
/// </summary>
/// <remarks>
/// Character data other than white space is a text node of the sequence constructor; it compiles through the callback of the context
/// like an element, and the registry answers with the synthetic text node entry (design #150 EP-9), which the construction folder
/// registers; without that entry it is the unsupported construct error. Comments and processing instructions are ignored.
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
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(names);

        body = null!;
        var instructions = new List<XsltExecutable>();
        var ok = true;
        for (var node = first; !node.IsEmpty; node = node.NextSibling)
        {
            ok &= node.Kind switch
            {
                XmlNodeKind.Text => TryCompileText(context, node, names, instructions),
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

    private static bool TryCompileText(
        XsltCompileContext context,
        XmlNode text,
        List<string> names,
        List<XsltExecutable> instructions
    )
    {
        if (XsltElementReader.IsWhitespace(text.StringValue))
        {
            return true;
        }

        var outer = context.Scope;
        var scope = new QueryStaticScope(outer.Namespaces, names.ToArray(), outer.BaseUri, outer.Edition);
        var compiled = context.CompileChildIn(new XPathNode(text), scope);
        if (compiled is null)
        {
            return false;
        }

        instructions.Add(compiled);
        return true;
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
        var slot = names.Count;
        var compiled = context.CompileChildIn(element, scope);
        if (compiled is not null)
        {
            instructions.Add(compiled);

            // The locals of a nested body took slots above the visible names; no name reaches them after the instruction, but
            // the function must save them, and the later variables of this constructor must not share them.
            if (compiled is IXsltLocalSlots nested)
            {
                while (names.Count < nested.SlotEnd)
                {
                    names.Add(string.Empty);
                }
            }
        }

        if (XsltElementReader.IsXslt(node, VariableLocalName))
        {
            // A failed variable still takes its slot, so the slots of the later ones stay where the compile expects them. A variable
            // with content took its own slot before the locals of its content, which are already counted in the names.
            var name = VariableKeyOf(element);
            if (slot < names.Count)
            {
                names[slot] = name;
            }
            else
            {
                names.Add(name);
            }
        }

        return compiled is not null;
    }

    // A prefixed name is kept as {namespace}local, so a reference resolves by expanded QName (XPath 2.0 section 3.1.2).
    private static string VariableKeyOf(XPathNode element)
    {
        var name = XsltElementReader.NameOrEmpty(element);
        return
            XsltElementReader.TryParseQName(name, out var prefix, out var local)
            && prefix is not null
            && XsltElementReader.TryResolvePrefix(element, prefix, out var namespaceName)
            ? VariableKey.Of(namespaceName, local)
            : name;
    }
}
