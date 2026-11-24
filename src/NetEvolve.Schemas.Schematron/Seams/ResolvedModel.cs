namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Model;

/// <summary>
/// The output of stage Resolve: the schema with every <c>include</c> and every <c>extends</c> with <c>href</c> merged at its
/// position, and the XSLT modules that the schema links (design #150 EP-14).
/// </summary>
/// <remarks>
/// The XSLT-namespace children of the schema and of included libraries also stay in the foreign children of the schema; the module
/// set holds the same declarations, with the modules of <c>xsl:include</c> and <c>xsl:import</c> followed and the import precedence
/// applied.
/// </remarks>
internal sealed class ResolvedModel
{
    internal ResolvedModel(SchematronSchema schema, XsltModuleSet? modules = null)
    {
        ArgumentNullException.ThrowIfNull(schema);

        Schema = schema;
        Modules = modules ?? XsltModuleSet.Empty;
    }

    internal SchematronSchema Schema { get; }

    /// <summary>Gets the linked XSLT modules; the empty set when the schema has none or its binding is not an XSLT binding.</summary>
    internal XsltModuleSet Modules { get; }
}
