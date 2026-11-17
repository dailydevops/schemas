namespace NetEvolve.Schemas.Schematron.Model;

// Internal parser intermediate: merged into the extending rule by stages Resolve and Expand.
internal sealed class SchematronExtends : SchematronNode
{
    internal SchematronExtends(DocumentLocation location, string? rule, string? href)
        : base(location)
    {
        Rule = rule;
        Href = href;
    }

    internal string? Rule { get; }

    internal string? Href { get; }
}
