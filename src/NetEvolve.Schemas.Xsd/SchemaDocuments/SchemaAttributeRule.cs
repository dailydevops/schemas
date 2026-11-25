namespace NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>An attribute that a schema element admits.</summary>
internal sealed class SchemaAttributeRule
{
    internal SchemaAttributeRule(string name, SchemaAttributeKind kind, bool required, string[] tokens)
    {
        Name = name;
        Kind = kind;
        Required = required;
        Tokens = tokens;
    }

    internal string Name { get; }

    internal SchemaAttributeKind Kind { get; }

    internal bool Required { get; }

    internal string[] Tokens { get; }

    /// <summary>Parses <c>name[:kind][!]</c>, where kind is <see langword="bool"/>, <c>ncname</c>, <c>qname</c>, <c>qnames</c>, <c>nonempty</c>, <see langword="uint"/>, <c>max</c>, <c>set=a|b</c>, or <c>list=a|b</c>.</summary>
    /// <param name="spec">The specification.</param>
    /// <returns>The rule.</returns>
    internal static SchemaAttributeRule Parse(string spec)
    {
        var required = spec.Length > 0 && spec[spec.Length - 1] == '!';
        if (required)
        {
            spec = spec.Substring(0, spec.Length - 1);
        }

        var colon = System.MemoryExtensions.IndexOf(spec.AsSpan(), ':');
        if (colon < 0)
        {
            return new SchemaAttributeRule(spec, SchemaAttributeKind.Any, required, []);
        }

        var name = spec.Substring(0, colon);
        var kind = spec.Substring(colon + 1);
        var equals = System.MemoryExtensions.IndexOf(kind.AsSpan(), '=');
        if (equals >= 0)
        {
            var tokens = kind.Substring(equals + 1).Split('|');
            var isList = kind.StartsWith("list", System.StringComparison.Ordinal);
            return new SchemaAttributeRule(
                name,
                isList ? SchemaAttributeKind.TokenList : SchemaAttributeKind.Token,
                required,
                tokens
            );
        }

        var simple = kind switch
        {
            "bool" => SchemaAttributeKind.Boolean,
            "ncname" => SchemaAttributeKind.NCName,
            "qname" => SchemaAttributeKind.QName,
            "qnames" => SchemaAttributeKind.QNameList,
            "nonempty" => SchemaAttributeKind.NonEmpty,
            "uint" => SchemaAttributeKind.UnsignedInteger,
            _ => SchemaAttributeKind.MaxOccurs,
        };
        return new SchemaAttributeRule(name, simple, required, []);
    }
}
