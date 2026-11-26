namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

/// <summary>
/// The stylesheet function that one table resolved for a name and arity. Immutable, so a signature-only host function shared by
/// concurrent runs can publish it with one reference write and the runs of the same table skip the lookup.
/// </summary>
internal sealed class XsltResolvedFunction
{
    internal XsltResolvedFunction(XsltFunctionTable table, XsltFunctionComponent component)
    {
        Table = table;
        Component = component;
    }

    /// <summary>Gets the table that resolved the function.</summary>
    internal XsltFunctionTable Table { get; }

    /// <summary>Gets the function that won the import precedence.</summary>
    internal XsltFunctionComponent Component { get; }
}
