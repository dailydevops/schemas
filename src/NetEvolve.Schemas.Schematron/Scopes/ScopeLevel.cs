namespace NetEvolve.Schemas.Schematron.Scopes;

/// <summary>The level a variable is declared at, outermost first (design #150 EP-4).</summary>
internal enum ScopeLevel : byte
{
    /// <summary>A schema-level <c>param</c> or <see langword="let"/>.</summary>
    Schema = 0,

    /// <summary>A <see langword="let"/> of the selected phase.</summary>
    Phase = 1,

    /// <summary>A <see langword="let"/> of a pattern or group.</summary>
    Pattern = 2,

    /// <summary>A <see langword="let"/> of a rule.</summary>
    Rule = 3,
}
