namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction;

using System.Runtime.InteropServices;

/// <summary>An attribute of a literal result element: its name as written in the stylesheet and its value template.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct LiteralAttribute
{
    internal LiteralAttribute(string prefix, string local, string uri, AttributeValueTemplate template)
    {
        Prefix = prefix;
        Local = local;
        Uri = uri;
        Template = template;
    }

    /// <summary>Gets the prefix as written.</summary>
    internal string Prefix { get; }

    /// <summary>Gets the local name.</summary>
    internal string Local { get; }

    /// <summary>Gets the namespace URI; empty for none.</summary>
    internal string Uri { get; }

    /// <summary>Gets the value template.</summary>
    internal AttributeValueTemplate Template { get; }
}
