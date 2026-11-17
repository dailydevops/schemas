namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Model;

/// <summary>The output of stage Parse: the principal schema with its <c>include</c> and <c>extends</c> still in place. Its base URI is <c>Schema.Location.Source</c>.</summary>
internal sealed class ParsedModule
{
    internal ParsedModule(SchematronSchema schema)
    {
        Argument.ThrowIfNull(schema);

        Schema = schema;
    }

    internal SchematronSchema Schema { get; }
}
