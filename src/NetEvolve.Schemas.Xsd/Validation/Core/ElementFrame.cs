namespace NetEvolve.Schemas.Xsd.Validation.Core;

using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.ContentModels.AllGroups;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.OpenContent;

/// <summary>The state of one open element. Frames are reused by depth, so a run allocates one per level of nesting.</summary>
internal sealed class ElementFrame
{
    internal XmlName Name;
    internal XPathQName QName;
    internal DocumentLocation Location;
    internal int Ordinal;
    internal FrameContent Content;
    internal ElementDeclaration? Declaration;
    internal IXsdComponent? Type;
    internal ComplexTypeDefinition? Complex;
    internal IXsdComponent? SimpleType;
    internal ContentProgram? Program;
    internal CountingRun? Counting;
    internal AllGroupRun? AllRun;
    internal OpenContentRun Open;
    internal bool IsValid;
    internal bool ContentFailed;
    internal bool TextReported;
    internal bool HasText;
    internal bool IsNilled;
    internal bool UndeclaredRoot;
    internal int NamespaceMark;

    internal void Start(XmlName name, XPathQName qname, DocumentLocation location, int ordinal, int namespaceMark)
    {
        Name = name;
        QName = qname;
        Location = location;
        Ordinal = ordinal;
        Content = FrameContent.Unassessed;
        Declaration = null;
        Type = null;
        Complex = null;
        SimpleType = null;
        Program = null;
        Counting = null;
        AllRun = null;
        Open = default;
        IsValid = true;
        ContentFailed = false;
        TextReported = false;
        HasText = false;
        IsNilled = false;
        UndeclaredRoot = false;
        NamespaceMark = namespaceMark;
    }
}
