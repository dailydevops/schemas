namespace NetEvolve.Schemas.Schematron.Model;

// Internal parser intermediate: replaced by stage Resolve, absent from the effective model.
// The indices place the include among the lists of its parent (the number of entries before it): Index is the position among
// the patterns of a container, among the rules of a pattern; the others are the positions among the variables, parameters, and
// foreign children of a container. A rule keeps the include inline in its items, so its indices stay zero.
internal sealed class SchematronInclude : SchematronNode
{
    internal SchematronInclude(
        DocumentLocation location,
        string href,
        int index = 0,
        int letIndex = 0,
        int parameterIndex = 0,
        int foreignIndex = 0
    )
        : base(location)
    {
        Href = href;
        Index = index;
        LetIndex = letIndex;
        ParameterIndex = parameterIndex;
        ForeignIndex = foreignIndex;
    }

    internal string Href { get; }

    internal int Index { get; }

    internal int LetIndex { get; }

    internal int ParameterIndex { get; }

    internal int ForeignIndex { get; }
}
