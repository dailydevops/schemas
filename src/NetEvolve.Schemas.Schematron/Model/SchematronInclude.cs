namespace NetEvolve.Schemas.Schematron.Model;

// Internal parser intermediate: replaced by stage Resolve, absent from the effective model.
internal sealed class SchematronInclude : SchematronNode
{
    internal SchematronInclude(DocumentLocation location, string href)
        : base(location) => Href = href;

    internal string Href { get; }
}
