namespace NetEvolve.Schemas.Xsd.Validation.Core;

using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.ContentModels.AllGroups;
using NetEvolve.Schemas.Xsd.ContentModels.AllGroups11;
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
    internal DocumentLocation TextLocation;
    internal int Ordinal;
    internal FrameContent Content;
    internal ElementDeclaration? Declaration;
    internal IXsdComponent? Type;
    internal ComplexTypeDefinition? Complex;
    internal IXsdComponent? SimpleType;
    internal ContentProgram? Program;
    internal CountingRun? Counting;
    internal AllGroupRun? AllRun;
    internal AllGroup11Run? AllRun11;
    internal OpenContentRun Open;
    internal bool IsValid;
    internal bool ContentFailed;
    internal bool TextReported;
    internal bool HasText;
    internal bool HasChild;
    internal bool IsNilled;
    internal bool Inherits;
    internal bool UndeclaredRoot;
    internal bool UndeclaredStrict;
    internal int NamespaceMark;
    internal int ErrorMark;

    internal void Start(
        XmlName name,
        XPathQName qname,
        DocumentLocation location,
        int ordinal,
        int namespaceMark,
        int errorMark
    )
    {
        Name = name;
        QName = qname;
        Location = location;
        TextLocation = location;
        Ordinal = ordinal;
        Content = FrameContent.Unassessed;
        Declaration = null;
        Type = null;
        Complex = null;
        SimpleType = null;
        Program = null;
        Counting = null;
        AllRun = null;
        AllRun11 = null;
        Open = default;
        IsValid = true;
        ContentFailed = false;
        TextReported = false;
        HasText = false;
        HasChild = false;
        IsNilled = false;
        Inherits = false;
        UndeclaredRoot = false;
        UndeclaredStrict = false;
        NamespaceMark = namespaceMark;
        ErrorMark = errorMark;
    }
}
