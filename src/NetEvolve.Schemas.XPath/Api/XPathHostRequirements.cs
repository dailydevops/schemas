namespace NetEvolve.Schemas.XPath.Api;

using System;

/// <summary>The host slots a function needs; a function that finds a required slot unset raises XPDY0002.</summary>
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
}
