namespace NetEvolve.Schemas.Xsd.Tests.Unit.TypeAlternatives;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.SchemaDocuments;
using NetEvolve.Schemas.Xsd.Tests.Unit.Declarations11;
using NetEvolve.Schemas.Xsd.TypeAlternatives;

public class InheritedAttributeTests
{
    private const string Kind = "<xs:alternative test=\"@kind = 'wide'\" type=\"t:a\"/>";

    [Test]
    public async Task AnInheritableAttributeOfAnAncestor_IsVisibleToTheTestOfADescendant()
    {
        var f = new AlternativeFixture();
        var table = f.Table(Kind)!;
        using var evaluator = new TypeAlternativeEvaluator();
        var stack = new InheritedAttributes();

        var parent = f.Enter(evaluator, table, stack, Attr.Of("kind", "wide", inheritable: true));
        var child = f.Enter(evaluator, table, stack);
        var grandchild = f.Enter(evaluator, table, stack);

        await Assert.That(parent.Type).IsSameReferenceAs(f.Type("a"));
        await Assert.That(child.Type).IsSameReferenceAs(f.Type("a"));
        await Assert.That(grandchild.Type).IsSameReferenceAs(f.Type("a"));
    }

    [Test]
    public async Task ANonInheritableAttributeOfAnAncestor_IsNotVisibleToADescendant()
    {
        var f = new AlternativeFixture();
        var table = f.Table(Kind)!;
        using var evaluator = new TypeAlternativeEvaluator();
        var stack = new InheritedAttributes();

        _ = f.Enter(evaluator, table, stack, Attr.Of("kind", "wide"));
        var child = f.Enter(evaluator, table, stack);

        await Assert.That(child.Type).IsSameReferenceAs(f.Declared);
    }

    [Test]
    public async Task TheInheritedAttribute_IsNotVisibleAfterItsElementEnds()
    {
        var f = new AlternativeFixture();
        var table = f.Table(Kind)!;
        using var evaluator = new TypeAlternativeEvaluator();
        var stack = new InheritedAttributes();

        _ = f.Enter(evaluator, table, stack);
        _ = f.Enter(evaluator, table, stack, Attr.Of("kind", "wide", inheritable: true));
        stack.Leave();
        var sibling = f.Enter(evaluator, table, stack);

        await Assert.That(sibling.Type).IsSameReferenceAs(f.Declared);
        await Assert.That(stack.Depth).IsEqualTo(2);
    }

    [Test]
    public async Task AnOwnAttribute_WinsOverAnInheritedOneOfTheSameName()
    {
        var f = new AlternativeFixture();
        var table = f.Table(Kind)!;
        using var evaluator = new TypeAlternativeEvaluator();
        var stack = new InheritedAttributes();

        _ = f.Enter(evaluator, table, stack, Attr.Of("kind", "wide", inheritable: true));
        var child = f.Enter(evaluator, table, stack, Attr.Of("kind", "narrow"));
        var grandchild = f.Enter(evaluator, table, stack);

        await Assert.That(child.Type).IsSameReferenceAs(f.Declared);
        await Assert.That(grandchild.Type).IsSameReferenceAs(f.Declared);
        await Assert.That(stack.Inherited.Length).IsEqualTo(0);
    }

    [Test]
    public async Task TheNearestAncestor_WinsOverAFartherOne()
    {
        var f = new AlternativeFixture();
        var table = f.Table(Kind)!;
        using var evaluator = new TypeAlternativeEvaluator();
        var stack = new InheritedAttributes();

        _ = f.Enter(evaluator, table, stack, Attr.Of("kind", "wide", inheritable: true));
        _ = f.Enter(evaluator, table, stack, Attr.Of("kind", "narrow", inheritable: true));
        var grandchild = f.Enter(evaluator, table, stack);
        stack.Leave();
        stack.Leave();
        var again = f.Enter(evaluator, table, stack);

        await Assert.That(grandchild.Type).IsSameReferenceAs(f.Declared);
        await Assert.That(again.Type).IsSameReferenceAs(f.Type("a"));
    }

    [Test]
    public async Task ANonInheritableAttributeOfTheSameName_HidesTheFartherOneFromTheSubtree()
    {
        var f = new AlternativeFixture();
        var table = f.Table(Kind)!;
        using var evaluator = new TypeAlternativeEvaluator();
        var stack = new InheritedAttributes();

        _ = f.Enter(evaluator, table, stack, Attr.Of("kind", "wide", inheritable: true));
        _ = f.Enter(evaluator, table, stack, Attr.Of("kind", "wide"));
        var grandchild = f.Enter(evaluator, table, stack);

        await Assert.That(grandchild.Type).IsSameReferenceAs(f.Declared);
    }

    [Test]
    public async Task InheritedAttributes_AreMatchedByExpandedName()
    {
        var f = new AlternativeFixture();
        var table = f.Table("<xs:alternative test=\"@t:kind = 'wide'\" type=\"t:a\"/>")!;
        using var evaluator = new TypeAlternativeEvaluator();
        var stack = new InheritedAttributes();

        _ = f.Enter(evaluator, table, stack, new Attr("urn:other", "kind", "wide", true));
        var other = f.Enter(evaluator, table, stack);
        stack.Leave();
        stack.Leave();
        _ = f.Enter(evaluator, table, stack, new Attr("urn:test", "kind", "wide", true));
        var same = f.Enter(evaluator, table, stack);

        await Assert.That(other.Type).IsSameReferenceAs(f.Declared);
        await Assert.That(same.Type).IsSameReferenceAs(f.Type("a"));
    }

    [Test]
    public async Task TheStack_ExposesTheInheritedAttributesOfTheInnermostElement()
    {
        var stack = new InheritedAttributes();
        var one = new[] { new TypeAlternativeAttribute(new XPathQName(string.Empty, "p"), 0, 2, true) };
        var two = new[]
        {
            new TypeAlternativeAttribute(new XPathQName(string.Empty, "q"), 0, 1, true),
            new TypeAlternativeAttribute(new XPathQName(string.Empty, "r"), 1, 1, false),
        };

        await Assert.That(stack.Inherited.Length).IsEqualTo(0);
        stack.Enter(one, "xy");
        await Assert.That(stack.Inherited.Length).IsEqualTo(0);
        stack.Enter(two, "ab");
        var count = stack.Inherited.Length;
        var name = stack.Inherited[0].Name.LocalName;
        var value = stack.Inherited[0].Value;
        await Assert.That(count).IsEqualTo(1);
        await Assert.That(name).IsEqualTo("p");
        await Assert.That(value).IsEqualTo("xy");
        stack.Enter([], string.Empty);
        await Assert.That(stack.Inherited.Length).IsEqualTo(2);
        stack.Leave();
        stack.Leave();
        stack.Leave();
        await Assert.That(stack.Depth).IsEqualTo(0);
    }

    [Test]
    public async Task Leave_WithoutAnOpenFrame_Throws()
    {
        var stack = new InheritedAttributes();

        await Assert.That(() => stack.Leave()).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task ManyFramesAndAttributes_GrowTheStack()
    {
        var stack = new InheritedAttributes();
        for (var i = 0; i < 40; i++)
        {
            stack.Enter([new TypeAlternativeAttribute(new XPathQName(string.Empty, "a" + i), 0, 1, true)], "v");
        }

        await Assert.That(stack.Depth).IsEqualTo(40);
        await Assert.That(stack.Inherited.Length).IsEqualTo(39);
    }

    [Test]
    [Arguments("inheritable=\"true\"", true)]
    [Arguments("inheritable=\"1\"", true)]
    [Arguments("inheritable=\"false\"", false)]
    [Arguments("inheritable=\"0\"", false)]
    [Arguments("", false)]
    public async Task TheInheritableAttribute_IsReadFromTheAttributeSyntax(string written, bool expected)
    {
        var root = SyntaxFixture.ParseDocument(
            SyntaxFixture.WithNamespace
                + "<xs:attribute name=\"k\" type=\"xs:string\" "
                + written
                + "/>"
                + SyntaxFixture.EndOfSchema
        );

        await Assert.That(AttributeInheritance.IsInheritable(root.GetChildNode(0))).IsEqualTo(expected);
    }
}
