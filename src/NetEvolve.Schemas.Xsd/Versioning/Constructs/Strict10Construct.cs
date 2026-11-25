namespace NetEvolve.Schemas.Xsd.Versioning.Constructs;

/// <summary>One XSD 1.1 element or attribute that strict 1.0 mode rejects, with the rule it reports.</summary>
internal sealed class Strict10Construct
{
    internal Strict10Construct(bool isAttribute, string name, XsdRule rule, string owners = "", bool localOnly = false)
    {
        IsAttribute = isAttribute;
        Name = name;
        Rule = rule;
        Owners = owners.Length == 0 ? [] : owners.Split(' ');
        LocalOnly = localOnly;
    }

    /// <summary>Gets a value indicating whether the construct is an attribute (otherwise an element).</summary>
    internal bool IsAttribute { get; }

    /// <summary>Gets the local name; a facet such as <c>explicitTimezone</c> is an element.</summary>
    internal string Name { get; }

    /// <summary>Gets the strict 1.0 rule.</summary>
    internal XsdRule Rule { get; }

    /// <summary>Gets the local names of the elements that carry the attribute; empty for any.</summary>
    internal string[] Owners { get; }

    /// <summary>Gets a value indicating whether the not-yet-supported gate of XSD 1.1 rejects the construct only on a local declaration; strict 1.0 mode rejects it on any declaration.</summary>
    internal bool LocalOnly { get; }
}
