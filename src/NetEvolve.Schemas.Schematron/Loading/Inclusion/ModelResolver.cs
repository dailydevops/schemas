namespace NetEvolve.Schemas.Schematron.Loading.Inclusion;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Parsing.Core;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Embedding;
using NetEvolve.Schemas.Schematron.Xslt.Modules;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// Stage Resolve: merges every <c>include</c> and every rule-level <c>extends</c> with <c>href</c> into the schema at the position
/// of the reference, loading the documents through the shared <see cref="IModuleLoader"/> (design #137 AP-22, #202).
/// </summary>
/// <remarks>
/// <para>
/// An <c>include</c> is replaced by the root element of its document: a <c>library</c> contributes its patterns, parameters,
/// variables, and foreign children at the position and its <c>rules</c>, diagnostics, and properties at the end; a <c>pattern</c>
/// or <see langword="group"/>, a <c>rule</c>, or an <c>assert</c>, <c>report</c>, or <c>p</c> takes the place of the element. An <c>extends</c>
/// with <c>href</c> names a <c>library</c> that holds exactly one abstract rule; its variables and items are copied into the
/// extending rule at the position of the <c>extends</c>, in the order the expansion by id uses. Locations keep the system
/// identifier of the module an element came from, and a relative reference is resolved against the system identifier of the
/// module that holds it. A library is read under the edition and query binding of the including schema.
/// </para>
/// <para>
/// Limitations: the title and paragraphs of an included library are dropped; a reference with a fragment identifier is
/// <c>SCH-UNSUPPORTED</c>, as is an included document whose root the grammar allows at the place but no stage reads (see
/// <c>CoreParseRun.Inclusion.cs</c>); a root the grammar does not allow, a library with other than one abstract rule behind
/// <c>extends</c>, and a document that is not well formed are <c>SCH-MODULE-INVALID</c>; a cycle closed by an <c>include</c> is
/// <c>SCH-INCLUDE-CYCLE</c>, one closed by an <c>extends</c> is <c>SCH-ABSTRACT-INVALID</c>; a by-id <c>extends</c> inside a
/// library rule that is reached by <c>href</c> is looked up in the including document by the expansion stage; the
/// resolver takes no cancellation token, so loads are not cancellable; <c>xml:base</c> and <c>xml:lang</c> are not
/// applied (decision Q10 of design #106). The XSLT-namespace children of included libraries are kept in the foreign children of
/// the schema at the position of the <c>include</c>; the module set of design #150 does not exist yet.
/// </para>
/// </remarks>
internal sealed class ModelResolver : IModelResolver
{
    private const string SchematronNamespace = "http://purl.oclc.org/dsdl/schematron";

    public bool TryResolve(ParsedModule root, IModuleLoader loader, IProblemSink problems, out ResolvedModel model)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(problems);

        var schema = new ResolveRun(root.Schema, loader, problems).Run();
        var modules = XsltModuleSet.Empty;
        var ok = schema is not null && TryLink(schema, loader, problems, out modules);
        model = new ResolvedModel(schema ?? root.Schema, modules);
        return ok;
    }

    // The XSLT-namespace children of the schema, which include those of the included libraries, are the principal module. The tree of
    // the module is built by the content writer of the construction folder, so the linker reads the same node model as the compiler.
    private static bool TryLink(
        SchematronSchema schema,
        IModuleLoader loader,
        IProblemSink problems,
        out XsltModuleSet modules
    )
    {
        modules = XsltModuleSet.Empty;
        if (!CoreParseRun.IsXsltBinding(schema.QueryBinding))
        {
            return true;
        }

        var declarations = new List<SchematronNode>();
        foreach (var element in schema.ForeignElements)
        {
            if (string.Equals(element.NamespaceUri, XsltInstructionRegistry.Namespace, StringComparison.Ordinal))
            {
                declarations.Add(element);
            }
        }

        if (declarations.Count == 0)
        {
            return true;
        }

        // The declarations are children of a schema element, which is what makes a variable or a parameter of them a global one.
        var holder = new SchematronForeignElement(schema.Location, SchematronNamespace, "schema", [], declarations);
        var names = new SpanInterningNameTable(XmlReaderLimits.Default.MaxDistinctNames);
        if (!LetContentTree.TryBuild(names, [holder], out var document, out var found))
        {
            problems.Add(new SchematronProblem(found.Code, found.Message, schema.Location, found.Constraint));
            return false;
        }

        _ = document.TryGetNode(out var tree);
        var top = new List<XPathNode>();
        for (var child = tree.Node.FirstChild.FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            top.Add(new XPathNode(child));
        }

        return new XsltModuleLinker(loader, problems, default).TryLink(top, schema.Location.Source, out modules);
    }
}
