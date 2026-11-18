namespace NetEvolve.Schemas.XPath.Api;

using System;

/// <summary>
/// The host slots a function needs, and the host flags that make a function visible; a function that finds a required slot unset
/// raises XPDY0002, and a function whose flag the host does not set is not found (XPST0017).
/// </summary>
[Flags]
public enum XPathHostRequirements
{
    /// <summary>No host slot.</summary>
    None = 0,

    /// <summary>The current item.</summary>
    CurrentItem = 1,

    /// <summary>The current group.</summary>
    CurrentGroup = 2,

    /// <summary>The regex groups.</summary>
    RegexGroups = 4,

    /// <summary>The decimal formats.</summary>
    DecimalFormats = 8,

    /// <summary>The keys.</summary>
    Keys = 16,

    /// <summary>
    /// The XSLT 2.0 host flag: the host evaluates under the rules of XSLT 2.0, so the functions that XSLT 2.0 adds to XPath 2.0 are
    /// visible in a 2.0 static context (for example <c>generate-id</c>); it is a flag and not a slot, so no function raises XPDY0002 for it.
    /// </summary>
    Xslt20Host = 32,
}
