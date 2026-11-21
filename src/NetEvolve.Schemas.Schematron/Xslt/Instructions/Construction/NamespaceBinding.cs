namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using System.Runtime.InteropServices;

/// <summary>A namespace prefix bound to a namespace URI, as text; an empty prefix is the default namespace, an empty URI undeclares it.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct NamespaceBinding
{
    internal NamespaceBinding(string prefix, string uri)
    {
        Prefix = prefix;
        Uri = uri;
    }

    /// <summary>Gets the prefix; empty for the default namespace.</summary>
    internal string Prefix { get; }

    /// <summary>Gets the namespace URI; empty for no namespace.</summary>
    internal string Uri { get; }
}
