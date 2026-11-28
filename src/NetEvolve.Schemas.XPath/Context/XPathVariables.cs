namespace NetEvolve.Schemas.XPath.Context;

using System.Collections.Generic;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>The variables bound for an evaluation. Not thread-safe; one instance per evaluation context.</summary>
public sealed class XPathVariables
{
    private readonly Dictionary<XPathQName, XdmSequence> _values = [];
    private XdmSequence[] _slots = [];
    private bool[] _slotBound = [];

    internal XPathVariables() { }

    /// <summary>Binds a variable to one item.</summary>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The value.</param>
    public void Set(XPathQName name, in XdmItem value) => _values[name] = XdmSequence.Create(value);

    /// <summary>Binds a variable to a sequence.</summary>
    /// <param name="name">The variable name.</param>
    /// <param name="value">The value.</param>
    public void Set(XPathQName name, XdmSequence value) => _values[name] = value;

    internal bool TryGetSlot(int slot, out XdmSequence value)
    {
        if ((uint)slot < (uint)_slotBound.Length && _slotBound[slot])
        {
            value = _slots[slot];
            return true;
        }

        value = default;
        return false;
    }

    internal bool TryGet(XPathQName name, out XdmSequence value) => _values.TryGetValue(name, out value);

    /// <summary>Gets the value of a bound variable.</summary>
    /// <param name="name">The variable name.</param>
    /// <returns>The value.</returns>
    /// <exception cref="XPathException">The variable is not bound (XPDY0002).</exception>
    internal XdmSequence Get(XPathQName name) =>
        _values.TryGetValue(name, out var value) ? value : throw XPathErrors.Exception(XPathErrorCodes.XPDY0002);

    /// <summary>Unbinds a variable, as the end of the scope of a range variable of a for, some, or every expression does when nothing was bound before.</summary>
    /// <param name="name">The variable name.</param>
    internal void Remove(XPathQName name) => _ = _values.Remove(name);

    internal void Clear() => _values.Clear();

    /// <summary>Attaches the slot arrays of the host. The arrays are read live, so the host stores into them directly; it attaches them again when it replaces them. The name-keyed values are unaffected.</summary>
    /// <param name="values">The values by slot.</param>
    /// <param name="bound">Whether the slot has a value, by slot.</param>
    internal void AttachSlots(XdmSequence[] values, bool[] bound)
    {
        _slots = values;
        _slotBound = bound;
    }

    /// <summary>Gets the value of a host slot.</summary>
    /// <param name="slot">The slot.</param>
    /// <returns>The value.</returns>
    /// <exception cref="XPathException">The slot has no value (XPDY0002).</exception>
    internal XdmSequence GetSlot(int slot) =>
        (uint)slot < (uint)_slotBound.Length && _slotBound[slot]
            ? _slots[slot]
            : throw XPathErrors.Exception(XPathErrorCodes.XPDY0002);
}
