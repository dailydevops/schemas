namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using System.Runtime.InteropServices;

/// <summary>An attribute that waits for the start tag of its element: the name as written and the value.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct PendingAttribute
{
    internal PendingAttribute(string prefix, string local, string uri, string value)
    {
        Prefix = prefix;
        Local = local;
        Uri = uri;
        Value = value;
    }

    /// <summary>Gets the prefix as requested; the namespace fixup may replace it.</summary>
    internal string Prefix { get; }

    /// <summary>Gets the local name.</summary>
    internal string Local { get; }

    /// <summary>Gets the namespace URI; empty for no namespace.</summary>
    internal string Uri { get; }

    /// <summary>Gets the value.</summary>
    internal string Value { get; }
}
