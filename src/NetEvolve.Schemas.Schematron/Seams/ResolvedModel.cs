namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Model;

/// <summary>
/// The output of stage Resolve: the schema with every <c>include</c> and every <c>extends</c> with <c>href</c> merged at its
/// position. Limitation: the XSLT-namespace children of included libraries travel in the foreign children of the schema; the
/// module set of design #150 (<c>XsltModuleSet</c>) does not exist yet.
/// </summary>
internal sealed class ResolvedModel
{
    internal ResolvedModel(SchematronSchema schema)
    {
        Argument.ThrowIfNull(schema);

        Schema = schema;
    }

    internal SchematronSchema Schema { get; }
}
