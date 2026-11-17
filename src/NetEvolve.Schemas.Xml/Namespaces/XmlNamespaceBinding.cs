namespace NetEvolve.Schemas.Xml.Namespaces;

using System.Runtime.InteropServices;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>One prefix bound to one namespace URI of an <see cref="XmlNameTable"/>.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct XmlNamespaceBinding
{
    internal XmlNamespaceBinding(XmlSymbol prefix, XmlSymbol uri)
    {
        Prefix = prefix;
        Uri = uri;
    }

    /// <summary>Gets the prefix, empty for the default namespace.</summary>
    internal XmlSymbol Prefix { get; }

    /// <summary>Gets the namespace URI, empty when the default namespace is undeclared.</summary>
    internal XmlSymbol Uri { get; }
}
