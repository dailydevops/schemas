namespace NetEvolve.Schemas.XPath.Abstractions;

using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The reserved host slots of an evaluation context. Each slot is empty (<see langword="null"/>) by default; a function that
/// needs a slot and finds it empty raises XPDY0002. Not thread-safe.
/// </summary>
public sealed class XPathHostSlots
{
    internal XPathHostSlots() { }

    /// <summary>Gets or sets the current item (XSLT <c>current()</c>), distinct from the context item.</summary>
    public XdmItem? CurrentItem { get; set; }

    /// <summary>Gets or sets the current group.</summary>
    public XdmSequence? CurrentGroup { get; set; }

    /// <summary>Gets or sets the groups captured by the last regular expression match.</summary>
    public XPathRegexGroups? RegexGroups { get; set; }

    /// <summary>Gets or sets the provider that resolves a decimal format by name.</summary>
    public IXPathDecimalFormatProvider? DecimalFormats { get; set; }

    /// <summary>Gets or sets the provider that resolves a key by name and value.</summary>
    public IXPathKeyProvider? Keys { get; set; }

    /// <summary>Empties every slot.</summary>
    public void Clear()
    {
        CurrentItem = null;
        CurrentGroup = null;
        RegexGroups = null;
        DecimalFormats = null;
        Keys = null;
    }
}
