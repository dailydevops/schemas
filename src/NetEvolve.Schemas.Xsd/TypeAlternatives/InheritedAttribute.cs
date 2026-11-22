namespace NetEvolve.Schemas.Xsd.TypeAlternatives;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.XPath.Api;

/// <summary>An inheritable attribute of an ancestor that a descendant sees: its expanded name and its value.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct InheritedAttribute
{
    /// <summary>Initializes a new instance of the <see cref="InheritedAttribute"/> struct.</summary>
    /// <param name="name">The expanded name.</param>
    /// <param name="value">The value.</param>
    internal InheritedAttribute(XPathQName name, string value)
    {
        Name = name;
        Value = value;
    }

    /// <summary>Gets the expanded name.</summary>
    internal XPathQName Name { get; }

    /// <summary>Gets the value.</summary>
    internal string Value { get; }
}
