namespace NetEvolve.Schemas.Schematron.Parsing.Core;

using System;

/// <summary>The inline elements a rich-text host admits besides character data.</summary>
[Flags]
internal enum CoreInline
{
    None = 0,
    Name = 1,
    ValueOf = 2,
    Emphasis = 4,
    Direction = 8,
    Span = 16,

    /// <summary>The <c>xsl:copy-of</c> of the XSLT namespace, which only a <c>property</c> admits (SCH-84, #289).</summary>
    CopyOf = 32,

    /// <summary>The set of <c>p</c> and <c>active</c>.</summary>
    Plain = Emphasis | Direction | Span,

    /// <summary>The set of <c>diagnostic</c>.</summary>
    Diagnostic = Plain | ValueOf,

    /// <summary>The set of <c>assert</c> and <c>report</c>.</summary>
    Message = Diagnostic | Name,

    /// <summary>The set of <c>property</c>.</summary>
    Property = Diagnostic | CopyOf,
}
