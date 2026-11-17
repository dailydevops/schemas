namespace NetEvolve.Schemas.Schematron.Parsing.Core;

/// <summary>The classification of an element for the core parser.</summary>
internal enum CoreElement
{
    /// <summary>An element outside the Schematron namespace; skipped.</summary>
    Foreign = 0,

    /// <summary>A Schematron element the grammar does not know.</summary>
    Unknown,

    /// <summary>A known Schematron element that a later work package parses.</summary>
    Deferred,
    Schema,
    Ns,
    Pattern,
    Rule,
    Assert,
    Report,
    Let,
    Phase,
    Active,
    Diagnostics,
    Diagnostic,
    Title,
    Paragraph,
    Name,
    ValueOf,
    Emphasis,
    Direction,
    Span,
}
