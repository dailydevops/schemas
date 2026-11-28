namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Schemas.Schematron.Model;

/// <summary>
/// The immutable, thread-safe result of the compile stage. The evaluation-context pool and the precomputed assertion strings
/// are added by the packages that own them.
/// </summary>
internal class CompiledSchema
{
    internal CompiledSchema(SchematronSchema schema, string effectivePhase)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(effectivePhase);

        Schema = schema;
        EffectivePhase = effectivePhase;
    }

    internal SchematronSchema Schema { get; }

    internal string EffectivePhase { get; }
}
