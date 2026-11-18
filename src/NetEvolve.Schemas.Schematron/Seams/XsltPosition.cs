namespace NetEvolve.Schemas.Schematron.Seams;

using System;

/// <summary>Where an XSLT element may appear; an entry carries every position it allows (design #150 EP-9).</summary>
[Flags]
internal enum XsltPosition : byte
{
    None = 0,

    /// <summary>In a sequence constructor.</summary>
    Instruction = 1,

    /// <summary>As a top-level declaration.</summary>
    Declaration = 2,

    /// <summary>As a structural child that only a parent instruction accepts, such as <c>xsl:sort</c>.</summary>
    Child = 4,
}
