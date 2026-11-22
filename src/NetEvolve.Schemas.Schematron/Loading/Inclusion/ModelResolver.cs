namespace NetEvolve.Schemas.Schematron.Loading.Inclusion;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;

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
    public bool TryResolve(ParsedModule root, IModuleLoader loader, IProblemSink problems, out ResolvedModel model)
    {
        ArgumentNullException.ThrowIfNull(root);
        ArgumentNullException.ThrowIfNull(loader);
        ArgumentNullException.ThrowIfNull(problems);

        var schema = new ResolveRun(root.Schema, loader, problems).Run();
        model = new ResolvedModel(schema ?? root.Schema);
        return schema is not null;
    }
}
