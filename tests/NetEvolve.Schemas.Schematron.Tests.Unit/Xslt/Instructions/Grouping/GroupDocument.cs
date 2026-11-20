namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Grouping;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// One instance document of the grouping tests: a root element <c>l</c> whose children come from a specification of words separated
/// by a space. A word is the element name, optionally followed by <c>=k</c> or <c>=k/j</c>, which gives the element the attributes
/// <c>k</c> and <c>j</c>. <c>"i=a i=b/x h"</c> is three children: <c>i</c> with <c>k="a"</c>, <c>i</c> with <c>k="b" j="x"</c>, and <c>h</c>.
/// </summary>
internal sealed class GroupDocument
{
    internal GroupDocument(string specification)
    {
        using var b = new XmlTreeBuilder();
        b.StartDocument(new XmlDeclaration("1.0", "utf-8", null));
        b.StartElement(Name(b, "l"), default);
        b.EndAttributes();
        foreach (var word in specification.Split([' '], StringSplitOptions.RemoveEmptyEntries))
        {
            Write(b, word);
        }

        b.EndElement(Name(b, "l"));
        b.EndDocument();
        Tree = b.Complete();
        Root = new XPathNode(Tree.Document.FirstChild);
    }

    internal XmlDocumentTree Tree { get; }

    /// <summary>Gets the element <c>l</c>, the context of the instruction under test.</summary>
    internal XPathNode Root { get; }

    private static XmlName Name(XmlTreeBuilder b, string local) => b.Names.Intern(string.Empty, local, string.Empty);

    private static void Write(XmlTreeBuilder b, string word)
    {
        var equals = word.IndexOf('=', StringComparison.Ordinal);
        var name = equals < 0 ? word : word.Substring(0, equals);
        b.StartElement(Name(b, name), default);
        if (equals >= 0)
        {
            var keys = word.Substring(equals + 1).Split('/');
            b.Attribute(Name(b, "k"), keys[0]);
            if (keys.Length > 1)
            {
                b.Attribute(Name(b, "j"), keys[1]);
            }
        }

        b.EndAttributes();
        b.EndElement(Name(b, name));
    }
}
