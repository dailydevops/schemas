namespace NetEvolve.Schemas.Xml.Tests.Unit.Contracts;

using System;
using System.Collections.Generic;
using System.Reflection;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;

public class XmlTreeContractTests
{
    // <a xmlns:p="urn:p" id="1" p:lang="en"><b>t1</b><!--c--><?pi d?><b>t2</b></a>
    private sealed record Sample(
        FakeXmlTree Tree,
        int A,
        int Id,
        int Lang,
        int Ns,
        int B1,
        int T1,
        int Comment,
        int Pi,
        int B2,
        int T2
    );

    private static Sample Build()
    {
        var tree = new FakeXmlTree();
        var a = tree.AddElement(0, "a");
        var ns = tree.AddNamespace(a, "p", "urn:p");
        var id = tree.AddAttribute(a, "id", "1");
        var lang = tree.AddAttribute(a, "lang", "en", "p", "urn:p");
        var b1 = tree.AddElement(a, "b");
        var t1 = tree.AddText(b1, "t1");
        var comment = tree.AddComment(a, "c");
        var pi = tree.AddProcessingInstruction(a, "pi", "d");
        var b2 = tree.AddElement(a, "b");
        var t2 = tree.AddText(b2, "t2");

        return new Sample(tree, a, id, lang, ns, b1, t1, comment, pi, b2, t2);
    }

    private static XmlNode Node(Sample sample, int index) => new(sample.Tree, index);

    [Test]
    public async Task Document_IsIndexZeroAndHasNoParent()
    {
        var sample = Build();

        var document = sample.Tree.Document;

        await Assert.That(document.Index).IsEqualTo(0);
        await Assert.That(document.Tree).IsSameReferenceAs(sample.Tree);
        await Assert.That(document.Kind).IsEqualTo(XmlNodeKind.Document);
        await Assert.That(document.Parent.IsEmpty).IsTrue();
        await Assert.That(document.IsEmpty).IsFalse();
    }

    [Test]
    public async Task Children_AreVisitedInDocumentOrderThroughFirstChildAndNextSibling()
    {
        var sample = Build();
        var kinds = new List<XmlNodeKind>();

        for (var child = Node(sample, sample.A).FirstChild; !child.IsEmpty; child = child.NextSibling)
        {
            kinds.Add(child.Kind);
        }

        await Assert
            .That(kinds)
            .IsEquivalentTo([
                XmlNodeKind.Element,
                XmlNodeKind.Comment,
                XmlNodeKind.ProcessingInstruction,
                XmlNodeKind.Element,
            ]);
        await Assert.That(sample.Tree.Document.FirstChild.Index).IsEqualTo(sample.A);
        await Assert.That(Node(sample, sample.T1).FirstChild.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Parent_OfEveryNodeKind_IsTheOwner()
    {
        var sample = Build();

        await Assert.That(Node(sample, sample.A).Parent.Index).IsEqualTo(0);
        await Assert.That(Node(sample, sample.T1).Parent.Index).IsEqualTo(sample.B1);
        await Assert.That(Node(sample, sample.Id).Parent.Index).IsEqualTo(sample.A);
        await Assert.That(Node(sample, sample.Ns).Parent.Index).IsEqualTo(sample.A);
    }

    [Test]
    public async Task Attributes_AreVisitedInSourceOrderAndAreNotChildren()
    {
        var sample = Build();
        var element = Node(sample, sample.A);

        var first = element.FirstAttribute;
        var second = first.NextAttribute;

        await Assert.That(first.Index).IsEqualTo(sample.Id);
        await Assert.That(first.Kind).IsEqualTo(XmlNodeKind.Attribute);
        await Assert.That(first.StringValue).IsEqualTo("1");
        await Assert.That(second.Index).IsEqualTo(sample.Lang);
        await Assert.That(second.NextAttribute.IsEmpty).IsTrue();
        await Assert.That(element.FirstChild.Kind).IsEqualTo(XmlNodeKind.Element);
        await Assert.That(Node(sample, sample.B1).FirstAttribute.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Names_ResolveThroughTheTreeNameTable()
    {
        var sample = Build();
        var names = sample.Tree.Names;
        var lang = Node(sample, sample.Lang).Name;
        var target = Node(sample, sample.Pi).Name;

        await Assert.That(names.GetString(lang.Local)).IsEqualTo("lang");
        await Assert.That(names.GetString(lang.Prefix)).IsEqualTo("p");
        await Assert.That(names.GetString(lang.NamespaceUri)).IsEqualTo("urn:p");
        await Assert.That(names.GetString(target.Local)).IsEqualTo("pi");
        await Assert.That(Node(sample, sample.T1).Name).IsEqualTo(default(XmlName));
    }

    [Test]
    public async Task Namespaces_AreNodesWithThePrefixAsNameAndTheUriAsValue()
    {
        var sample = Build();
        sample.Tree.AddNamespace(sample.A, "q", "urn:q");

        var first = Node(sample, sample.A).FirstNamespace;
        var second = first.NextNamespace;

        await Assert.That(first.Kind).IsEqualTo(XmlNodeKind.Namespace);
        await Assert.That(sample.Tree.Names.GetString(first.Name.Local)).IsEqualTo("p");
        await Assert.That(first.StringValue).IsEqualTo("urn:p");
        await Assert.That(sample.Tree.Names.GetString(second.Name.Local)).IsEqualTo("q");
        await Assert.That(second.NextNamespace.IsEmpty).IsTrue();
        await Assert.That(Node(sample, sample.B1).FirstNamespace.IsEmpty).IsTrue();
    }

    [Test]
    public async Task StringValue_OfElementsAndDocument_ConcatenatesDescendantText()
    {
        var sample = Build();

        await Assert.That(Node(sample, sample.A).StringValue).IsEqualTo("t1t2");
        await Assert.That(sample.Tree.Document.StringValue).IsEqualTo("t1t2");
        await Assert.That(Node(sample, sample.B2).StringValue).IsEqualTo("t2");
        await Assert.That(Node(sample, sample.Comment).StringValue).IsEqualTo("c");
        await Assert.That(Node(sample, sample.Pi).StringValue).IsEqualTo("d");
        await Assert.That(Node(sample, sample.T1).StringValue).IsEqualTo("t1");
    }

    [Test]
    public async Task BaseUriAndDocumentUri_ComeFromTheTree()
    {
        var sample = Build();
        sample.Tree.BaseUriValue = new Uri("file:///base/");
        sample.Tree.DocumentUriValue = new Uri("file:///doc.xml");

        await Assert.That(Node(sample, sample.A).BaseUri).IsEqualTo(new Uri("file:///base/"));
        await Assert.That(sample.Tree.Document.DocumentUri).IsEqualTo(new Uri("file:///doc.xml"));
        await Assert.That(Node(sample, sample.B1).DocumentUri).IsEqualTo(new Uri("file:///doc.xml"));
    }

    [Test]
    public async Task UnparsedEntitiesAndNotations_AreExposedOnTheTree()
    {
        var sample = Build();
        sample.Tree.UnparsedEntityList.Add(new XmlUnparsedEntity("pic", "pic.png", null, "png"));
        sample.Tree.NotationList.Add(new XmlNotation("png", "viewer", null));

        await Assert.That(sample.Tree.UnparsedEntities).Count().IsEqualTo(1);
        await Assert.That(sample.Tree.UnparsedEntities[0].NotationName).IsEqualTo("png");
        await Assert.That(sample.Tree.Notations).Count().IsEqualTo(1);
        await Assert.That(sample.Tree.Notations[0].Name).IsEqualTo("png");
    }

    [Test]
    public async Task CompareDocumentOrder_InOneTree_FollowsElementNamespacesAttributesThenChildren()
    {
        var sample = Build();
        var inOrder = new[]
        {
            sample.A,
            sample.Ns,
            sample.Id,
            sample.Lang,
            sample.B1,
            sample.T1,
            sample.Comment,
            sample.Pi,
            sample.B2,
            sample.T2,
        };
        var failures = new List<string>();

        for (var i = 0; i < inOrder.Length; i++)
        {
            for (var j = 0; j < inOrder.Length; j++)
            {
                var result = Math.Sign(Node(sample, inOrder[i]).CompareDocumentOrder(Node(sample, inOrder[j])));
                if (result != Math.Sign(i.CompareTo(j)))
                {
                    failures.Add($"{i} vs {j}: {result}");
                }
            }
        }

        await Assert.That(failures).IsEmpty();
        await Assert.That(sample.Tree.Document.CompareDocumentOrder(Node(sample, sample.A))).IsLessThan(0);
    }

    [Test]
    public async Task CompareDocumentOrder_AcrossTrees_OrdersByTreeIdAndIsStable()
    {
        var first = Build();
        var second = Build();

        var forward = Node(first, first.A).CompareDocumentOrder(Node(second, second.B2));
        var backward = Node(second, second.B2).CompareDocumentOrder(Node(first, first.A));
        var again = Node(first, first.A).CompareDocumentOrder(Node(second, second.B2));

        await Assert.That(forward).IsLessThan(0);
        await Assert.That(backward).IsGreaterThan(0);
        await Assert.That(again).IsEqualTo(forward);
        await Assert.That(second.Tree.TreeId).IsGreaterThan(first.Tree.TreeId);
    }

    [Test]
    public async Task TreeId_IsUniquePerTree()
    {
        var ids = new HashSet<long>();
        for (var i = 0; i < 50; i++)
        {
            ids.Add(new FakeXmlTree().TreeId);
        }

        await Assert.That(ids).Count().IsEqualTo(50);
    }

    [Test]
    public async Task XmlNode_Equality_IsTreeAndIndex()
    {
        var sample = Build();
        var other = Build();
        var node = Node(sample, sample.A);
        object boxed = Node(sample, sample.A);

        await Assert.That(node == Node(sample, sample.A)).IsTrue();
        await Assert.That(node != Node(sample, sample.B1)).IsTrue();
        await Assert.That(node == Node(other, other.A)).IsFalse();
        await Assert.That(node.Equals(boxed)).IsTrue();
        await Assert.That(node.Equals(new object())).IsFalse();
        await Assert.That(node.GetHashCode()).IsEqualTo(Node(sample, sample.A).GetHashCode());
        await Assert.That(default(XmlNode) == default(XmlNode)).IsTrue();
        await Assert.That(default(XmlNode).IsEmpty).IsTrue();
        await Assert.That(node.Parent.FirstChild).IsEqualTo(node);
    }

    [Test]
    public async Task XmlNode_Constructor_RejectsNullTreeAndNegativeIndex()
    {
        await Assert.That(() => new XmlNode(null!, 0)).Throws<ArgumentNullException>();
        await Assert.That(() => new XmlNode(new FakeXmlTree(), -1)).Throws<ArgumentOutOfRangeException>();
    }

    // Rule: property X delegates to XmlTree.GetX(int); a method delegates to the same-named XmlTree method.
    private static readonly HashSet<string> NotDelegating = new(StringComparer.Ordinal)
    {
        nameof(XmlNode.Tree),
        nameof(XmlNode.Index),
        nameof(XmlNode.IsEmpty),
        nameof(Equals),
        nameof(GetHashCode),
        nameof(ToString),
        nameof(GetType),
    };

    private static string? Invoke(MemberInfo member, out string expected)
    {
        var sample = Build();
        var boxed = (object)Node(sample, sample.A);
        sample.Tree.Calls.Clear();
        expected = string.Empty;
        switch (member)
        {
            case PropertyInfo property:
                _ = property.GetValue(boxed);
                expected = $"Get{property.Name}({sample.A})";
                break;
            case MethodInfo { IsSpecialName: false } method:
                _ = method.Invoke(boxed, [Node(sample, sample.B1)]);
                expected = $"{method.Name}({sample.A},{sample.B1})";
                break;
            default:
                return null;
        }

        return sample.Tree.Calls.Contains(expected)
            ? string.Empty
            : $"{member.Name}: expected {expected}, got [{string.Join(", ", sample.Tree.Calls)}]";
    }

    [Test]
    public async Task XmlNode_AllMembers_DelegateToXmlTree()
    {
        var delegating = new List<string>();
        var failures = new List<string>();

        foreach (var member in typeof(XmlNode).GetMembers(BindingFlags.Public | BindingFlags.Instance))
        {
            if (member.DeclaringType != typeof(XmlNode) || NotDelegating.Contains(member.Name))
            {
                continue;
            }

            var failure = Invoke(member, out _);
            if (failure is null)
            {
                continue;
            }

            delegating.Add(member.Name);
            if (failure.Length > 0)
            {
                failures.Add(failure);
            }
        }

        await Assert.That(failures).IsEmpty();
        await Assert
            .That(delegating)
            .IsEquivalentTo([
                nameof(XmlNode.Kind),
                nameof(XmlNode.Name),
                nameof(XmlNode.Parent),
                nameof(XmlNode.FirstChild),
                nameof(XmlNode.NextSibling),
                nameof(XmlNode.FirstAttribute),
                nameof(XmlNode.NextAttribute),
                nameof(XmlNode.FirstNamespace),
                nameof(XmlNode.NextNamespace),
                nameof(XmlNode.StringValue),
                nameof(XmlNode.BaseUri),
                nameof(XmlNode.DocumentUri),
                nameof(XmlNode.CompareDocumentOrder),
            ]);
    }

    [Test]
    public async Task XmlTree_CannotBeDerivedOutsideAssembly()
    {
        var constructors = typeof(XmlTree).GetConstructors(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );

        await Assert.That(typeof(XmlTree).IsAbstract).IsTrue();
        await Assert.That(constructors).Count().IsEqualTo(1);
        // private protected: only types derived inside the Xml assembly (and its friend assemblies) can call it.
        await Assert.That(constructors[0].IsFamilyAndAssembly).IsTrue();
    }

    [Test]
    public async Task XmlTree_AbstractAccessors_AreProtectedInternal()
    {
        var accessors = new List<string>();
        foreach (
            var method in typeof(XmlTree).GetMethods(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            )
        )
        {
            if (method.IsAbstract && !method.IsSpecialName)
            {
                accessors.Add(method.Name + ":" + method.IsFamilyOrAssembly);
            }
        }

        await Assert.That(accessors).Count().IsEqualTo(13);
        await Assert.That(accessors).DoesNotContain(name => name.EndsWith(":False", StringComparison.Ordinal));
    }
}
