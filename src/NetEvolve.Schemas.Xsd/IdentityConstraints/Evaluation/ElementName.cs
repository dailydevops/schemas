namespace NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;

using System.Runtime.InteropServices;

/// <summary>The expanded name of an open element, kept on the stack the path matcher reads.</summary>
[StructLayout(LayoutKind.Auto)]
internal readonly struct ElementName
{
    internal ElementName(string namespaceName, string localName)
    {
        NamespaceName = namespaceName;
        LocalName = localName;
    }

    /// <summary>Gets the namespace name, empty for none.</summary>
    internal string NamespaceName { get; }

    /// <summary>Gets the local name.</summary>
    internal string LocalName { get; }
}
