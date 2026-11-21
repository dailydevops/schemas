namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using System.Runtime.InteropServices;

/// <summary>The name of an element or attribute that an instruction computed: the prefix as written, the local name, and the namespace URI.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct ExpandedName
{
    internal ExpandedName(string prefix, string local, string uri)
    {
        Prefix = prefix;
        Local = local;
        Uri = uri;
    }

    /// <summary>Gets the prefix; empty for none.</summary>
    internal string Prefix { get; }

    /// <summary>Gets the local name.</summary>
    internal string Local { get; }

    /// <summary>Gets the namespace URI; empty for none.</summary>
    internal string Uri { get; }
}
