namespace NetEvolve.Schemas.Schematron.Parsing.Core;

/// <summary>The classification of an element for the core parser.</summary>
internal enum CoreElement
{
    /// <summary>An element outside the Schematron namespace; skipped.</summary>
    Foreign = 0,

    /// <summary>A Schematron element the grammar does not know.</summary>
    Unknown,

    /// <summary>The <c>include</c> element.</summary>
    Include,

    /// <summary>The <c>extends</c> element.</summary>
    Extends,
    Library,
    Rules,
    Group,
    Param,
    Properties,
    Property,
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
