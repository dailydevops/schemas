namespace NetEvolve.Schemas.XPath.Abstractions;

using System;
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

    /// <summary>
    /// Gets or sets the current grouping key (XSLT <c>current-grouping-key()</c>): the key that the items of the current group share. It
    /// is unset (<see langword="null"/>) outside a grouping instruction and for a grouping by pattern; an empty sequence is a set slot
    /// that holds no key.
    /// </summary>
    public XdmSequence? CurrentGroupingKey { get; set; }

    /// <summary>Gets or sets the groups captured by the last regular expression match.</summary>
    public XPathRegexGroups? RegexGroups { get; set; }

    /// <summary>Gets or sets the provider that resolves a decimal format by name.</summary>
    public IXPathDecimalFormatProvider? DecimalFormats { get; set; }

    /// <summary>Gets or sets the provider that resolves a key by name and value.</summary>
    public IXPathKeyProvider? Keys { get; set; }

    /// <summary>
    /// Gets or sets the state of the current run, which a host function reaches through <c>invocation.Context.Host.HostState</c>. A
    /// compiled expression is shared by concurrent runs, so a registered host function holds only immutable data and reads the state of
    /// its run here. It is unset (<see langword="null"/>) by default.
    /// </summary>
    public object? HostState { get; set; }

    /// <summary>Gets or sets the sink of <c>fn:trace</c>: the label and the traced value. Internal until the host seam is decided (#884).</summary>
    internal Action<string, XdmSequence>? Trace { get; set; }

    /// <summary>Empties every slot.</summary>
    public void Clear()
    {
        CurrentItem = null;
        CurrentGroup = null;
        CurrentGroupingKey = null;
        RegexGroups = null;
        DecimalFormats = null;
        Keys = null;
        HostState = null;
        Trace = null;
    }
}
