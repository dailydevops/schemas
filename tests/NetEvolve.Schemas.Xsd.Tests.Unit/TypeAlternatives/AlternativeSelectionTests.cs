namespace NetEvolve.Schemas.Xsd.Tests.Unit.TypeAlternatives;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.TypeAlternatives;

public class AlternativeSelectionTests
{
    [Test]
    public async Task FirstAlternativeWhoseTestIsTrue_SelectsTheType_LaterMatchesAreIgnored()
    {
        var f = new AlternativeFixture();
        var table = f.Table(
            "<xs:alternative test=\"@k = 'no'\" type=\"t:c\"/>"
                + "<xs:alternative test=\"@k = 'yes'\" type=\"t:a\"/>"
                + "<xs:alternative test=\"@k\" type=\"t:b\"/>"
                + "<xs:alternative type=\"t:c\"/>"
        )!;

        var selection = f.Select(table, Attr.Of("k", "yes"));

        await Assert.That(f.Issues.Count).IsEqualTo(0);
        await Assert.That(AlternativeFixture.Name(selection)).IsEqualTo("a");
        await Assert.That(selection.Alternative).IsSameReferenceAs(table[1]);
    }

    [Test]
    public async Task NoMatchingTest_TheDefaultAlternativeApplies()
    {
        var f = new AlternativeFixture();
        var table = f.Table("<xs:alternative test=\"@k = 'yes'\" type=\"t:a\"/><xs:alternative type=\"t:b\"/>")!;

        var selection = f.Select(table, Attr.Of("k", "other"));

        await Assert.That(AlternativeFixture.Name(selection)).IsEqualTo("b");
        await Assert.That(selection.Alternative).IsSameReferenceAs(table.Default);
    }

    [Test]
    public async Task NoMatchingTest_WithoutADefault_TheDeclaredTypeApplies()
    {
        var f = new AlternativeFixture();
        var table = f.Table("<xs:alternative test=\"@k = 'yes'\" type=\"t:a\"/>")!;

        var selection = f.Select(table, Attr.Of("k", "other"));

        await Assert.That(selection.Type).IsSameReferenceAs(f.Declared);
        await Assert.That(selection.Alternative).IsNull();
    }

    [Test]
    public async Task ATestThatReadsAChild_SeesAnEmptySequence()
    {
        var f = new AlternativeFixture();
        var table = f.Table(
            "<xs:alternative test=\"t:child\" type=\"t:a\"/>"
                + "<xs:alternative test=\"not(t:child) and count(*) = 0 and empty(t:child/@z)\" type=\"t:b\"/>"
        )!;

        var selection = f.Select(table, Attr.Of("child", "present"));

        await Assert.That(AlternativeFixture.Name(selection)).IsEqualTo("b");
    }

    [Test]
    public async Task ATestThatReadsTheParentOrAnAncestor_FindsNone()
    {
        var f = new AlternativeFixture();
        var table = f.Table(
            "<xs:alternative test=\"exists(..)\" type=\"t:a\"/>"
                + "<xs:alternative test=\"empty(ancestor::*) and empty(parent::node())\" type=\"t:b\"/>"
        )!;

        var selection = f.Select(table);

        await Assert.That(AlternativeFixture.Name(selection)).IsEqualTo("b");
    }

    [Test]
    public async Task ATestThatRaisesADynamicError_IsFalse_AndDoesNotInvalidateTheElement()
    {
        var f = new AlternativeFixture();
        var table = f.Table(
            "<xs:alternative test=\"@k + 1 = 2\" type=\"t:a\"/>"
                + "<xs:alternative test=\"/ = 1\" type=\"t:a\"/>"
                + "<xs:alternative test=\"(1, 2)\" type=\"t:a\"/>"
                + "<xs:alternative type=\"t:b\"/>"
        )!;

        var selection = f.Select(table, Attr.Of("k", "not a number"));

        await Assert.That(AlternativeFixture.Name(selection)).IsEqualTo("b");
        await Assert.That(selection.Validate(new XPathQName("urn:test", "e"), default, f.Reporter)).IsTrue();
        await Assert.That(f.Issues.Count).IsEqualTo(0);
    }

    [Test]
    public async Task TheSchemaInstanceAttributes_AreVisibleToTheTest()
    {
        var f = new AlternativeFixture();
        var table = f.Table("<xs:alternative test=\"@xsi:nil = 'true'\" type=\"t:a\"/>")!;

        var selection = f.Select(table, new Attr("http://www.w3.org/2001/XMLSchema-instance", "nil", "true"));

        await Assert.That(AlternativeFixture.Name(selection)).IsEqualTo("a");
    }

    [Test]
    public async Task ANamespacedAttribute_IsMatchedByItsExpandedName()
    {
        var f = new AlternativeFixture();
        var table = f.Table(
            "<xs:alternative test=\"@k = 'plain'\" type=\"t:a\"/><xs:alternative test=\"@t:k = 'qualified'\" type=\"t:b\"/>"
        )!;

        var selection = f.Select(table, new Attr("urn:test", "k", "qualified"));

        await Assert.That(AlternativeFixture.Name(selection)).IsEqualTo("b");
    }

    [Test]
    public async Task SelectingXsError_MakesTheElementInvalid()
    {
        var f = new AlternativeFixture();
        var table = f.Table("<xs:alternative test=\"@k = 'bad'\" type=\"xs:error\"/><xs:alternative type=\"t:a\"/>")!;
        var location = new DocumentLocation("file:///i.xml", 3, 5);

        var bad = f.Select(table, Attr.Of("k", "bad"));
        var good = f.Select(table, Attr.Of("k", "fine"));
        var accepted = good.Validate(new XPathQName("urn:test", "e"), location, f.Reporter);
        var rejected = bad.Validate(new XPathQName("urn:test", "e"), location, f.Reporter);

        await Assert.That(bad.IsError).IsTrue();
        await Assert.That(good.IsError).IsFalse();
        await Assert.That(accepted).IsTrue();
        await Assert.That(rejected).IsFalse();
        await Assert.That(f.Issues.Count).IsEqualTo(1);
        await Assert.That(f.Issues[0].Code).IsEqualTo(XsdRuleCodes.CvcType);
        await Assert.That(f.Issues[0].Location).IsEqualTo(location);
    }

    [Test]
    public async Task TheDefaultAlternativeWithXsError_AlsoMakesTheElementInvalid()
    {
        var f = new AlternativeFixture();
        var table = f.Table("<xs:alternative test=\"@k\" type=\"t:a\"/><xs:alternative type=\"xs:error\"/>")!;

        var selection = f.Select(table);

        await Assert.That(selection.IsError).IsTrue();
        await Assert.That(selection.Validate(new XPathQName("urn:test", "e"), default, f.Reporter)).IsFalse();
    }

    [Test]
    public async Task OneEvaluator_SelectsForManyElements_WithoutLeakingAttributesBetweenThem()
    {
        var f = new AlternativeFixture();
        var table = f.Table("<xs:alternative test=\"@k = 'one'\" type=\"t:a\"/>")!;
        using var evaluator = new TypeAlternativeEvaluator();
        var stack = new InheritedAttributes();

        var first = f.Enter(evaluator, table, stack, Attr.Of("k", "one"));
        stack.Leave();
        var second = f.Enter(evaluator, table, stack);
        stack.Leave();
        var third = f.Enter(evaluator, table, stack, Attr.Of("k", "one"));

        await Assert.That(AlternativeFixture.Name(first)).IsEqualTo("a");
        await Assert.That(second.Type).IsSameReferenceAs(f.Declared);
        await Assert.That(AlternativeFixture.Name(third)).IsEqualTo("a");
    }

    [Test]
    public async Task TheElementName_IsVisibleToTheTest()
    {
        var f = new AlternativeFixture();
        var table = f.Table("<xs:alternative test=\"self::t:other\" type=\"t:a\"/>")!;
        using var evaluator = new TypeAlternativeEvaluator();

        var matching = f.Enter(evaluator, table, new InheritedAttributes(), new XPathQName("urn:test", "other"));
        var other = f.Enter(evaluator, table, new InheritedAttributes(), new XPathQName("urn:test", "e"));

        await Assert.That(AlternativeFixture.Name(matching)).IsEqualTo("a");
        await Assert.That(other.Type).IsSameReferenceAs(f.Declared);
    }

    [Test]
    public async Task Select_WithANullArgument_Throws()
    {
        var f = new AlternativeFixture();
        var table = f.Table("<xs:alternative test=\"@k\" type=\"t:a\"/>")!;
        using var evaluator = new TypeAlternativeEvaluator();
        var name = new XPathQName("urn:test", "e");

        await Assert
            .That(() => evaluator.Select(null!, f.Declared, name, [], default, new InheritedAttributes()))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => evaluator.Select(table, null!, name, [], default, new InheritedAttributes()))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => evaluator.Select(table, f.Declared, name, [], default, null!))
            .Throws<ArgumentNullException>();
    }
}
