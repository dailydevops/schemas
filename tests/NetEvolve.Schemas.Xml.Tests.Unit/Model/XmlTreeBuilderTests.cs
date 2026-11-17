namespace NetEvolve.Schemas.Xml.Tests.Unit.Model;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xml.Text.NameTable;

public class XmlTreeBuilderTests
{
    private const string XmlNs = "http://www.w3.org/XML/1998/namespace";

    private static readonly Uri DocumentUri = new("file:///data/doc.xml");

    private static XmlName Name(XmlTreeBuilder b, string local, string prefix = "", string uri = "") =>
        b.Names.Intern(prefix, local, uri);

    private static void Element(
        XmlTreeBuilder b,
        string local,
        Action? content = null,
        XmlPosition position = default,
        string prefix = "",
        string uri = ""
    )
    {
        var name = Name(b, local, prefix, uri);
        b.StartElement(name, position);
        b.EndAttributes();
        content?.Invoke();
        b.EndElement(name);
    }

    // <a xmlns:p="urn:p" id="1" p:lang="en"><b>t1</b><!--c--><?pi d?><b>t2</b></a>
    private static XmlDocumentTree BuildSample(Uri? documentUri = null)
    {
        using var b = new XmlTreeBuilder(documentUri: documentUri);
        b.StartDocument(new XmlDeclaration("1.0", "utf-8", null));
        var a = Name(b, "a");
        b.StartElement(a, new XmlPosition(1, 1, 0));
        b.NamespaceDeclaration(b.Names.Intern("p"), b.Names.Intern("urn:p"));
        b.Attribute(Name(b, "id"), "1");
        b.Attribute(Name(b, "lang", "p", "urn:p"), "en");
        b.EndAttributes();
        Element(b, "b", () => b.Text("t1", XmlTextKind.Text), new XmlPosition(2, 3, 40));
        b.Comment("c");
        b.ProcessingInstruction(b.Names.Intern("pi"), "d");
        Element(b, "b", () => b.Text("t2", XmlTextKind.Text), new XmlPosition(3, 5, 60));
        b.EndElement(a);
        b.EndDocument();
        return b.Complete();
    }

    private static List<XmlNode> Children(XmlNode node)
    {
        var list = new List<XmlNode>();
        for (var c = node.FirstChild; !c.IsEmpty; c = c.NextSibling)
        {
            list.Add(c);
        }

        return list;
    }

    private static string Describe(XmlNode node, XmlTree tree)
    {
        var sb = new StringBuilder();
        Walk(node);
        return sb.ToString();

        void Walk(XmlNode n)
        {
            sb.Append(n.Kind).Append(':').Append(tree.Names.GetString(n.Name.Local)).Append('=').Append(n.StringValue);
            for (var ns = n.FirstNamespace; !ns.IsEmpty; ns = ns.NextNamespace)
            {
                sb.Append('[').Append(ns.Kind).Append(':').Append(ns.StringValue).Append(']');
            }

            for (var at = n.FirstAttribute; !at.IsEmpty; at = at.NextAttribute)
            {
                sb.Append('@').Append(tree.Names.GetString(at.Name.Local)).Append('=').Append(at.StringValue);
            }

            sb.Append('(');
            for (var c = n.FirstChild; !c.IsEmpty; c = c.NextSibling)
            {
                Walk(c);
            }

            sb.Append(')');
        }
    }

    [Test]
    public async Task Build_ScriptedSinkCalls_ProducesKindsNamesValuesAndLinks()
    {
        var tree = BuildSample();
        var doc = tree.Document;

        await Assert.That(doc.Kind).IsEqualTo(XmlNodeKind.Document);
        await Assert.That(doc.Parent.IsEmpty).IsTrue();
        await Assert.That(doc.NextSibling.IsEmpty).IsTrue();

        var a = doc.FirstChild;
        await Assert.That(a.Kind).IsEqualTo(XmlNodeKind.Element);
        await Assert.That(tree.Names.GetString(a.Name.Local)).IsEqualTo("a");
        await Assert.That(a.Parent).IsEqualTo(doc);

        var attributes = new List<XmlNode>();
        for (var at = a.FirstAttribute; !at.IsEmpty; at = at.NextAttribute)
        {
            attributes.Add(at);
        }

        await Assert.That(attributes.Count).IsEqualTo(2);
        await Assert.That(attributes[0].Kind).IsEqualTo(XmlNodeKind.Attribute);
        await Assert.That(attributes[0].StringValue).IsEqualTo("1");
        await Assert.That(attributes[0].Parent).IsEqualTo(a);
        await Assert.That(tree.Names.GetString(attributes[1].Name.Prefix)).IsEqualTo("p");
        await Assert.That(tree.Names.GetString(attributes[1].Name.NamespaceUri)).IsEqualTo("urn:p");
        await Assert.That(attributes[1].StringValue).IsEqualTo("en");

        var kids = Children(a);
        await Assert
            .That(kids.Select(k => k.Kind).ToArray())
            .IsEquivalentTo([
                XmlNodeKind.Element,
                XmlNodeKind.Comment,
                XmlNodeKind.ProcessingInstruction,
                XmlNodeKind.Element,
            ]);
        await Assert.That(kids[0].FirstChild.Kind).IsEqualTo(XmlNodeKind.Text);
        await Assert.That(kids[0].FirstChild.StringValue).IsEqualTo("t1");
        await Assert.That(kids[0].StringValue).IsEqualTo("t1");
        await Assert.That(kids[1].StringValue).IsEqualTo("c");
        await Assert.That(tree.Names.GetString(kids[2].Name.Local)).IsEqualTo("pi");
        await Assert.That(kids[2].StringValue).IsEqualTo("d");
        await Assert.That(a.StringValue).IsEqualTo("t1t2");
        await Assert.That(doc.StringValue).IsEqualTo("t1t2");
        await Assert.That(kids[3].Parent).IsEqualTo(a);
        await Assert.That(kids[3].NextSibling.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Build_ConsecutiveTextEvents_CoalesceIntoOneTextNodeAndEmptyTextIsDropped()
    {
        using var b = new XmlTreeBuilder();
        b.StartDocument(new XmlDeclaration("1.0", "", null));
        Element(b, "r", () =>
        {
            b.Text("a", XmlTextKind.Text);
            b.Text("&", XmlTextKind.Reference);
            b.Text("b", XmlTextKind.CData);
            b.Text("", XmlTextKind.Text);
            b.Comment("x");
            b.Text("", XmlTextKind.Text);
            b.Text("c", XmlTextKind.Text);
        });
        b.EndDocument();
        var tree = b.Complete();

        var kids = Children(tree.Document.FirstChild);
        await Assert.That(kids.Count).IsEqualTo(3);
        await Assert.That(kids[0].StringValue).IsEqualTo("a&b");
        await Assert.That(kids[1].Kind).IsEqualTo(XmlNodeKind.Comment);
        await Assert.That(kids[2].StringValue).IsEqualTo("c");
        await Assert.That(tree.Document.StringValue).IsEqualTo("a&bc");
    }

    [Test]
    public async Task Namespaces_InScopeSetPerElement_InheritsShadowsAndUndeclares()
    {
        // <a xmlns="urn:d" xmlns:p="urn:p"><b xmlns:p="urn:p2" xmlns:q="urn:q"><c xmlns=""/></b></a>
        using var b = new XmlTreeBuilder();
        b.StartDocument(new XmlDeclaration("1.0", "", null));
        var n = b.Names;
        var a = Name(b, "a");
        b.StartElement(a, default);
        b.NamespaceDeclaration(n.Intern(""), n.Intern("urn:d"));
        b.NamespaceDeclaration(n.Intern("p"), n.Intern("urn:p"));
        b.EndAttributes();
        var bn = Name(b, "b");
        b.StartElement(bn, default);
        b.NamespaceDeclaration(n.Intern("p"), n.Intern("urn:p2"));
        b.NamespaceDeclaration(n.Intern("q"), n.Intern("urn:q"));
        b.EndAttributes();
        var c = Name(b, "c");
        b.StartElement(c, default);
        b.NamespaceDeclaration(n.Intern(""), n.Intern(""));
        b.EndAttributes();
        b.EndElement(c);
        b.EndElement(bn);
        b.EndElement(a);
        b.EndDocument();
        var tree = b.Complete();

        string[] Scope(XmlNode element)
        {
            var list = new List<string>();
            for (var ns = element.FirstNamespace; !ns.IsEmpty; ns = ns.NextNamespace)
            {
                if (ns.Kind != XmlNodeKind.Namespace || ns.Parent != element)
                {
                    throw new InvalidOperationException("Namespace node must belong to its element.");
                }

                list.Add(tree.Names.GetString(ns.Name.Local) + "=" + ns.StringValue);
            }

            list.Sort(StringComparer.Ordinal);
            return [.. list];
        }

        var ea = tree.Document.FirstChild;
        var eb = ea.FirstChild;
        var ec = eb.FirstChild;

        await Assert.That(Scope(ea)).IsEquivalentTo(["=urn:d", "p=urn:p"]);
        await Assert.That(Scope(eb)).IsEquivalentTo(["=urn:d", "p=urn:p2", "q=urn:q"]);
        await Assert.That(Scope(ec)).IsEquivalentTo(["p=urn:p2", "q=urn:q"]);
    }

    [Test]
    public async Task DocumentOrder_WithinOneTree_FollowsElementNamespacesAttributesChildren()
    {
        var tree = BuildSample();
        var a = tree.Document.FirstChild;
        var ns = a.FirstNamespace;
        var id = a.FirstAttribute;
        var lang = id.NextAttribute;
        var b1 = a.FirstChild;
        var t1 = b1.FirstChild;
        var comment = b1.NextSibling;
        var b2 = comment.NextSibling.NextSibling;

        XmlNode[] expected = [tree.Document, a, ns, id, lang, b1, t1, comment, comment.NextSibling, b2, b2.FirstChild];

        for (var i = 0; i < expected.Length; i++)
        {
            for (var j = 0; j < expected.Length; j++)
            {
                await Assert.That(Math.Sign(expected[i].CompareDocumentOrder(expected[j]))).IsEqualTo(i.CompareTo(j));
            }
        }
    }

    [Test]
    public async Task Identity_SameTreeAndTwoTrees_IsStableAndOrderAcrossTreesIsConsistent()
    {
        var first = BuildSample();
        var second = BuildSample();

        var a1 = first.Document.FirstChild;
        var again = first.Document.FirstChild;
        var a2 = second.Document.FirstChild;

        await Assert.That(a1 == again).IsTrue();
        await Assert.That(a1.GetHashCode()).IsEqualTo(again.GetHashCode());
        await Assert.That(a1 == a2).IsFalse();
        await Assert.That(a1.Index).IsEqualTo(a2.Index);
        await Assert.That(first.TreeId == second.TreeId).IsFalse();

        var ab = a1.CompareDocumentOrder(a2);
        await Assert.That(ab).IsNotEqualTo(0);
        await Assert.That(Math.Sign(a2.CompareDocumentOrder(a1))).IsEqualTo(-Math.Sign(ab));
        await Assert
            .That(Math.Sign(first.Document.CompareDocumentOrder(second.Document.FirstChild)))
            .IsEqualTo(Math.Sign(ab));
    }

    [Test]
    public async Task BaseUri_DocumentUriXmlBaseAndInheritance_AreResolved()
    {
        // <r><s xml:base="sub/"><t xml:base="http://example.org/x/"><u/></t><v/></s><w/></r>
        using var b = new XmlTreeBuilder(documentUri: DocumentUri);
        b.StartDocument(new XmlDeclaration("1.0", "", null));
        var xmlBase = Name(b, "base", "xml", XmlNs);
        Element(b, "r", () =>
        {
            var s = Name(b, "s");
            b.StartElement(s, default);
            b.Attribute(xmlBase, "sub/");
            b.EndAttributes();
            var t = Name(b, "t");
            b.StartElement(t, default);
            b.Attribute(xmlBase, "http://example.org/x/");
            b.EndAttributes();
            Element(b, "u");
            b.EndElement(t);
            Element(b, "v");
            b.EndElement(s);
            Element(b, "w");
        });
        b.EndDocument();
        var tree = b.Complete();

        var r = tree.Document.FirstChild;
        var sNode = r.FirstChild;
        var tNode = sNode.FirstChild;

        await Assert.That(tree.Document.BaseUri).IsEqualTo(DocumentUri);
        await Assert.That(tree.Document.DocumentUri).IsEqualTo(DocumentUri);
        await Assert.That(r.BaseUri).IsEqualTo(DocumentUri);
        await Assert.That(sNode.BaseUri).IsEqualTo(new Uri("file:///data/sub/"));
        await Assert.That(sNode.FirstAttribute.BaseUri).IsEqualTo(new Uri("file:///data/sub/"));
        await Assert.That(tNode.BaseUri).IsEqualTo(new Uri("http://example.org/x/"));
        await Assert.That(tNode.FirstChild.BaseUri).IsEqualTo(new Uri("http://example.org/x/"));
        await Assert.That(tNode.NextSibling.BaseUri).IsEqualTo(new Uri("file:///data/sub/"));
        await Assert.That(sNode.NextSibling.BaseUri).IsEqualTo(DocumentUri);
        await Assert.That(tNode.DocumentUri).IsEqualTo(DocumentUri);
    }

    [Test]
    public async Task BaseUri_RootedPathXmlBase_ResolvesAgainstDocumentUriOnEveryPlatform()
    {
        using var b = new XmlTreeBuilder(documentUri: new Uri("http://example.org/a/b.xml"));
        b.StartDocument(new XmlDeclaration("1.0", "", null));
        var xmlBase = Name(b, "base", "xml", XmlNs);
        var r = Name(b, "r");
        b.StartElement(r, default);
        b.Attribute(xmlBase, "/dir/");
        b.EndAttributes();
        b.EndElement(r);
        b.EndDocument();
        var tree = b.Complete();

        await Assert.That(tree.Document.FirstChild.BaseUri).IsEqualTo(new Uri("http://example.org/dir/"));
    }

    [Test]
    public async Task Complete_AfterFailure_BuilderStaysUsableAfterNewDocument()
    {
        using var b = new XmlTreeBuilder();
        await Assert.That(() => b.Complete()).Throws<InvalidOperationException>();

        b.StartDocument(new XmlDeclaration("1.0", "", null));
        Element(b, "a");
        b.EndDocument();

        await Assert.That(b.Complete().Document.FirstChild.Kind).IsEqualTo(XmlNodeKind.Element);
    }

    [Test]
    public async Task BaseUri_WithoutDocumentUri_IsNull()
    {
        var tree = BuildSample();

        await Assert.That(tree.Document.FirstChild.BaseUri).IsNull();
        await Assert.That(tree.Document.DocumentUri).IsNull();
    }

    [Test]
    public async Task Positions_ElementsCarryLineColumnAndOffset()
    {
        var tree = BuildSample();
        var a = tree.Document.FirstChild;
        var b1 = a.FirstChild;
        var b2 = b1.NextSibling.NextSibling.NextSibling;

        await Assert.That(tree.GetPosition(a.Index)).IsEqualTo(new XmlPosition(1, 1, 0));
        await Assert.That(tree.GetPosition(b1.Index)).IsEqualTo(new XmlPosition(2, 3, 40));
        await Assert.That(tree.GetPosition(b2.Index)).IsEqualTo(new XmlPosition(3, 5, 60));
        await Assert.That(tree.GetPosition(b1.FirstChild.Index)).IsEqualTo(default(XmlPosition));
    }

    [Test]
    public async Task Positions_WhenNotTracked_AreDefault()
    {
        using var b = new XmlTreeBuilder(trackPositions: false);
        Element(b, "a", position: new XmlPosition(4, 2, 9));
        var tree = b.Complete();

        await Assert.That(tree.GetPosition(0)).IsEqualTo(default(XmlPosition));
    }

    [Test]
    public async Task Fragment_WithoutDocumentNode_HasElementRootAndOrder()
    {
        using var b = new XmlTreeBuilder();
        Element(b, "root", () =>
        {
            Element(b, "x", () => b.Text("1", XmlTextKind.Text));
            Element(b, "y");
        });
        var tree = b.Complete();

        var root = tree.Document;
        await Assert.That(root.Kind).IsEqualTo(XmlNodeKind.Element);
        await Assert.That(root.Index).IsEqualTo(0);
        await Assert.That(root.Parent.IsEmpty).IsTrue();
        await Assert.That(root.NextSibling.IsEmpty).IsTrue();

        var x = root.FirstChild;
        var y = x.NextSibling;
        await Assert.That(root.CompareDocumentOrder(x)).IsLessThan(0);
        await Assert.That(x.CompareDocumentOrder(y)).IsLessThan(0);
        await Assert.That(y.CompareDocumentOrder(x.FirstChild)).IsGreaterThan(0);
        await Assert.That(root.StringValue).IsEqualTo("1");
    }

    [Test]
    public async Task Fragment_TextRoot_IsASingleTextNode()
    {
        using var b = new XmlTreeBuilder();
        b.Text("he", XmlTextKind.Text);
        b.Text("llo", XmlTextKind.Text);
        var tree = b.Complete();

        await Assert.That(tree.Document.Kind).IsEqualTo(XmlNodeKind.Text);
        await Assert.That(tree.Document.StringValue).IsEqualTo("hello");
        await Assert.That(tree.Document.NextSibling.IsEmpty).IsTrue();
    }

    [Test]
    public async Task IdTable_RegisteredIds_ResolveToElementsFirstWins()
    {
        using var b = new XmlTreeBuilder();
        b.StartDocument(new XmlDeclaration("1.0", "", null));
        Element(b, "r", () =>
        {
            var e1 = Name(b, "e");
            b.StartElement(e1, default);
            b.RegisterId("one");
            b.EndAttributes();
            b.EndElement(e1);
            var e2 = Name(b, "e");
            b.StartElement(e2, default);
            b.RegisterId("one");
            b.RegisterId("two");
            b.EndAttributes();
            b.EndElement(e2);
        });
        b.EndDocument();
        var tree = b.Complete();

        var first = tree.Document.FirstChild.FirstChild;
        var second = first.NextSibling;

        await Assert.That(tree.TryGetElementById("one", out var one)).IsTrue();
        await Assert.That(one).IsEqualTo(first);
        await Assert.That(tree.TryGetElementById("two", out var two)).IsTrue();
        await Assert.That(two).IsEqualTo(second);
        await Assert.That(tree.TryGetElementById("three", out var none)).IsFalse();
        await Assert.That(none.IsEmpty).IsTrue();
    }

    [Test]
    public async Task IdTable_WithoutRegistrations_IsEmpty()
    {
        var tree = BuildSample();

        await Assert.That(tree.TryGetElementById("1", out var node)).IsFalse();
        await Assert.That(node.IsEmpty).IsTrue();
    }

    [Test]
    public async Task UnparsedEntitiesAndNotations_AddedBeforeComplete_AreExposedOnTheTree()
    {
        using var b = new XmlTreeBuilder();
        b.AddUnparsedEntity(new XmlUnparsedEntity("pic", "pic.png", null, "png"));
        b.AddNotation(new XmlNotation("png", "viewer.exe", null));
        Element(b, "a");
        var tree = b.Complete();

        await Assert.That(tree.UnparsedEntities.Count).IsEqualTo(1);
        await Assert.That(tree.UnparsedEntities[0].Name).IsEqualTo("pic");
        await Assert.That(tree.Notations.Count).IsEqualTo(1);
        await Assert.That(tree.Notations[0].Name).IsEqualTo("png");
        await Assert.That(BuildSample().UnparsedEntities.Count).IsEqualTo(0);
        await Assert.That(BuildSample().Notations.Count).IsEqualTo(0);
    }

    [Test]
    public async Task NameTable_OwnedIsFrozenAtComplete_SuppliedIsNot()
    {
        using var owned = new XmlTreeBuilder();
        Element(owned, "a");
        var ownedTree = owned.Complete();

        var supplied = new SpanInterningNameTable(100);
        using var shared = new XmlTreeBuilder(supplied);
        Element(shared, "a");
        var sharedTree = shared.Complete();

        await Assert.That(ownedTree.Names.IsFrozen).IsTrue();
        await Assert.That(sharedTree.Names).IsSameReferenceAs(supplied);
        await Assert.That(supplied.IsFrozen).IsFalse();
    }

    [Test]
    public async Task Builder_CompleteTwice_ReusesBuffersAndYieldsIndependentTrees()
    {
        using var b = new XmlTreeBuilder();
        Element(b, "a", () => b.Text("one", XmlTextKind.Text));
        var first = b.Complete();
        Element(b, "z", () => b.Text("two", XmlTextKind.Text));
        var second = b.Complete();

        await Assert.That(first.Document.StringValue).IsEqualTo("one");
        await Assert.That(first.Names.GetString(first.Document.Name.Local)).IsEqualTo("a");
        await Assert.That(second.Document.StringValue).IsEqualTo("two");
        await Assert.That(second.Names.GetString(second.Document.Name.Local)).IsEqualTo("z");
        await Assert.That(first.TreeId == second.TreeId).IsFalse();
    }

    [Test]
    public async Task Builder_Misuse_ThrowsInvalidOperation()
    {
        using var empty = new XmlTreeBuilder();
        await Assert.That(() => empty.Complete()).Throws<InvalidOperationException>();

        using var open = new XmlTreeBuilder();
        open.StartElement(Name(open, "a"), default);
        open.EndAttributes();
        await Assert.That(() => open.Complete()).Throws<InvalidOperationException>();

        using var stray = new XmlTreeBuilder();
        await Assert.That(() => stray.EndElement(Name(stray, "a"))).Throws<InvalidOperationException>();

        using var late = new XmlTreeBuilder();
        Element(late, "a");
        await Assert
            .That(() => late.StartDocument(new XmlDeclaration("1.0", "", null)))
            .Throws<InvalidOperationException>();

        using var noElement = new XmlTreeBuilder();
        await Assert.That(() => noElement.RegisterId("x")).Throws<InvalidOperationException>();

        var disposed = new XmlTreeBuilder();
        disposed.Dispose();
        await Assert.That(() => disposed.Comment("x")).Throws<ObjectDisposedException>();
    }

    [Test]
    public async Task Builder_NullArguments_Throw()
    {
        using var b = new XmlTreeBuilder();

        await Assert.That(() => b.AddNotation(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => b.AddUnparsedEntity(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => b.RegisterId(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => BuildSample().GetPosition(-1)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => BuildSample().GetPosition(1_000)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Navigation_WalkingTheWholeTree_AllocatesNothing()
    {
        var tree = BuildSample();

        AllocationAssert.NoAllocation(() =>
        {
            var count = 0;
            var node = tree.Document;
            while (!node.IsEmpty)
            {
                count += (int)node.Kind + node.Name.Local.Value;
                for (var at = node.FirstAttribute; !at.IsEmpty; at = at.NextAttribute)
                {
                    count += at.Index;
                }

                for (var ns = node.FirstNamespace; !ns.IsEmpty; ns = ns.NextNamespace)
                {
                    count += ns.Index;
                }

                if (!node.FirstChild.IsEmpty)
                {
                    node = node.FirstChild;
                    continue;
                }

                while (!node.IsEmpty && node.NextSibling.IsEmpty)
                {
                    node = node.Parent;
                }

                node = node.IsEmpty ? node : node.NextSibling;
            }

            if (count == int.MinValue)
            {
                throw new InvalidOperationException();
            }

            _ = tree.Document.CompareDocumentOrder(tree.Document.FirstChild);
        });

        await Task.CompletedTask;
    }

    [Test]
    public async Task Build_SecondTreeWithReusedBuilder_AllocatesOnlyTheFinalArrays()
    {
        var table = new SpanInterningNameTable(100);
        using var shared = new XmlTreeBuilder(table);
        void BuildShared()
        {
            var name = table.Intern("", "item", "");
            shared.StartDocument(new XmlDeclaration("1.0", "", null));
            shared.StartElement(name, default);
            shared.EndAttributes();
            for (var i = 0; i < 50; i++)
            {
                shared.StartElement(name, default);
                shared.EndAttributes();
                shared.EndElement(name);
            }

            shared.EndElement(name);
            shared.EndDocument();
        }

        BuildShared();
        _ = shared.Complete();
        var baseline = AllocationAssert.Measure(() =>
        {
            BuildShared();
            _ = shared.Complete();
        });

        // Final arrays are the only allocations: the columns of 52 rows plus the tree object itself.
        await Assert.That(baseline).IsLessThan(4_000);
    }

    [Test]
    public async Task Concurrency_ManyThreadsReadingOneTree_SeeIdenticalResults()
    {
        var tree = BuildSample(DocumentUri);
        var expected = Describe(tree.Document, tree);

        var results = await Task.WhenAll(
            Enumerable
                .Range(0, 16)
                .Select(worker =>
                    Task.Run(() =>
                    {
                        var last = string.Empty;
                        for (var i = 0; i < 200; i++)
                        {
                            last = Describe(tree.Document, tree);
                            _ = tree.Document.FirstChild.FirstChild.BaseUri;
                            _ = tree.Document.FirstChild.CompareDocumentOrder(tree.Document);
                        }

                        return last;
                    })
                )
        );

        foreach (var result in results)
        {
            await Assert.That(result).IsEqualTo(expected);
        }
    }

    [Test]
    public async Task Immutability_PublicSurfaceOfBuiltTree_OffersNoMutator()
    {
        var type = typeof(XmlDocumentTree);
        var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        foreach (var member in members)
        {
            switch (member)
            {
                case PropertyInfo property:
                    await Assert.That(property.SetMethod is null || !property.SetMethod.IsPublic).IsTrue();
                    break;
                case FieldInfo field:
                    await Assert.That(field.IsInitOnly).IsTrue();
                    break;
                case MethodInfo { IsSpecialName: false } method:
                    await Assert
                        .That(
                            method.Name.StartsWith("Get", StringComparison.Ordinal)
                                || method.Name.StartsWith("TryGet", StringComparison.Ordinal)
                        )
                        .IsTrue();
                    break;
                default:
                    break;
            }
        }

        await Assert.That(type.IsSealed).IsTrue();
        await Assert.That(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Length).IsEqualTo(0);
    }

    [Test]
    public async Task Doctype_Event_IsIgnoredWithoutNode()
    {
        using var b = new XmlTreeBuilder();
        b.StartDocument(new XmlDeclaration("1.0", "", null));
        b.Doctype(new XmlDoctypeInfo("a", "", "", ""));
        Element(b, "a");
        b.EndDocument();
        var tree = b.Complete();

        await Assert.That(Children(tree.Document).Count).IsEqualTo(1);
    }
}
