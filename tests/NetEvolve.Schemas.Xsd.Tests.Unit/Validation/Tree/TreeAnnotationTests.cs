namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Tree;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using NetEvolve.Schemas.Xsd.Validation.Annotations;

public class TreeAnnotationTests
{
    private const string Valid = "<root><item>42</item><item>x</item></root>";

    [Test]
    public async Task TryGetAnnotation_ElementOfTheReturnedTree_HasTypeAndValidity(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = TreeFixture.Validator().Validate(TreeFixture.Load(Valid).Document, cancellationToken);

        var found = result.TryGetAnnotation(TreeFixture.Element(result.Document, "item"), out var annotation);
        var invalid = result.TryGetAnnotation(TreeFixture.Element(result.Document, "item", 1), out var rejected);

        await Assert.That(found).IsTrue();
        await Assert.That(annotation.TypeName.LocalName).IsEqualTo("integer");
        await Assert.That(annotation.Validity).IsEqualTo(XsdValidity.Valid);
        await Assert.That(invalid).IsTrue();
        await Assert.That(rejected.Validity).IsEqualTo(XsdValidity.Invalid);
    }

    [Test]
    public async Task TryGetAnnotation_AttributeOfTheReturnedTree_HasType(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = TreeFixture
            .Validator()
            .Validate(TreeFixture.Load("<root a=\"5\"><item>1</item></root>").Document, cancellationToken);

        var found = result.TryGetAnnotation(
            TreeFixture.Element(result.Document, "root").FirstAttribute,
            out var annotation
        );

        await Assert.That(found).IsTrue();
        await Assert.That(annotation.TypeName.LocalName).IsEqualTo("int");
    }

    [Test]
    public async Task Data_XPathOverTheAnnotatedTree_ReturnsTypedValues(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = TreeFixture.Validator().Validate(TreeFixture.Load(Valid).Document, cancellationToken);
        var expression = XPathExpression.Compile("/root/item[1]");
        using var context = new XPathEvaluationContext();
        context.SetContextItem(XdmItem.FromNode(new XPathNode(result.Document.Document)));

        var data = result.Data(expression.Evaluate(context));

        await Assert.That(data.Count).IsEqualTo(1);
        await Assert.That(data[0].TypeId).IsEqualTo(XdmTypeId.XsInteger);
        await Assert.That(data[0].TryGetInteger(out var value)).IsTrue();
        await Assert.That(value).IsEqualTo(42L);
    }

    [Test]
    public async Task Validate_InputTree_IsNotModified_AndTheAnnotationsAreOnTheReturnedTree(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var input = TreeFixture.Load(Valid);
        var before = (input.Count, input.Document.StringValue);

        var result = TreeFixture.Validator().Validate(input.Document, cancellationToken);

        await Assert.That((input.Count, input.Document.StringValue)).IsEqualTo(before);
        await Assert.That(result.Document).IsNotSameReferenceAs(input);
        await Assert.That(result.TryGetAnnotation(TreeFixture.Element(input, "item"), out _)).IsFalse();
        await Assert.That(result.TryGetAnnotation(TreeFixture.Element(result.Document, "item"), out _)).IsTrue();
        await Assert.That(result.Document.Document.StringValue).IsEqualTo(input.Document.StringValue);
    }

    [Test]
    public async Task Validate_Subtree_ReturnsADocumentThatContainsOnlyTheSubtree(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var input = TreeFixture.Load("<root><item>1</item><group><inner>2</inner></group></root>");

        var result = TreeFixture.Validator().Validate(TreeFixture.Element(input, "group"), cancellationToken);

        await Assert.That(result.Document.Document.FirstChild.Kind).IsEqualTo(XmlNodeKind.Element);
        await Assert.That(result.Document.Document.StringValue).IsEqualTo("2");
        await Assert
            .That(result.TryGetAnnotation(TreeFixture.Element(result.Document, "inner"), out var annotation))
            .IsTrue();
        await Assert.That(annotation.TypeName.LocalName).IsEqualTo("int");
    }

    [Test]
    public async Task TryGetIssueNode_BadValue_IsTheElementWithItsSourceLocation(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = TreeFixture
            .Validator()
            .Validate(TreeFixture.Load("<root>\n<item>1</item>\n  <item>x</item></root>").Document, cancellationToken);
        var issue = result.Result.Issues[0];

        var found = result.TryGetIssueNode(issue, out var node);

        await Assert.That(found).IsTrue();
        await Assert.That(node.StringValue).IsEqualTo("x");
        var position = result.Document.GetPosition(node.Index);
        await Assert.That(position.Line).IsEqualTo(issue.Location.Line);
        await Assert.That(position.Column).IsEqualTo(issue.Location.Column);
    }

    [Test]
    public async Task TryGetIssueNode_MissingContent_IsTheElementThatEnds(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = TreeFixture.Validator().Validate(TreeFixture.Load("<root/>").Document, cancellationToken);

        var found = result.TryGetIssueNode(result.Result.Issues[0], out var node);

        await Assert.That(found).IsTrue();
        await Assert.That(node).IsEqualTo(TreeFixture.Element(result.Document, "root"));
    }

    [Test]
    public async Task TryGetIssueNode_IssueOfTheEndOfTheRun_IsTheDocumentNode(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = TreeFixture
            .Validator()
            .Validate(TreeFixture.Load(TreeFixture.Instances[5]).Document, cancellationToken);

        var found = result.TryGetIssueNode(result.Result.Issues[0], out var node);

        await Assert.That(found).IsTrue();
        await Assert.That(node).IsEqualTo(result.Document.Document);
    }

    [Test]
    public async Task TryGetIssueNode_IssueOfAnotherResult_IsNotFound(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validator = TreeFixture.Validator();
        var first = validator.Validate(TreeFixture.Load("<root/>").Document, cancellationToken);
        var second = validator.Validate(TreeFixture.Load("<root/>").Document, cancellationToken);

        await Assert.That(second.TryGetIssueNode(first.Result.Issues[0], out _)).IsFalse();
    }

    [Test]
    public async Task TryGetIssueNode_IssueOfAnAttribute_IsTheElementThatOwnsIt(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = TreeFixture
            .Validator()
            .Validate(TreeFixture.Load("<root b=\"1\"><item>1</item></root>").Document, cancellationToken);

        var found = result.TryGetIssueNode(result.Result.Issues[0], out var node);

        await Assert.That(found).IsTrue();
        await Assert.That(node).IsEqualTo(TreeFixture.Element(result.Document, "root"));
    }

    [Test]
    public async Task Validate_DefaultedAttribute_IsNotANodeOfTheAnnotatedTree(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = TreeFixture
            .Validator()
            .Validate(TreeFixture.Load("<root><item>1</item></root>").Document, cancellationToken);

        await Assert.That(TreeFixture.Element(result.Document, "root").FirstAttribute.IsEmpty).IsTrue();
    }

    [Test]
    public async Task Validate_AnnotatedTree_SharesTheNameTableOfTheRunWithoutFreezingIt(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = TreeFixture
            .Validator()
            .Validate(TreeFixture.Load("<root><item>1</item></root>").Document, cancellationToken);

        await Assert.That(result.Document.Names.IsFrozen).IsFalse();
    }

    [Test]
    [MethodDataSource(nameof(Documents))]
    public async Task TryGetIssueNode_EveryIssueThatBelongsToAnElement_IsLocatedAtTheStartOfThatElement(
        string instance,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = TreeFixture.Validator().Validate(TreeFixture.Load(instance).Document, cancellationToken);

        foreach (var issue in result.Result.Issues)
        {
            await Assert.That(result.TryGetIssueNode(issue, out var node)).IsTrue();
            if (node.Kind == XmlNodeKind.Element)
            {
                var position = result.Document.GetPosition(node.Index);
                await Assert
                    .That((position.Line, position.Column))
                    .IsEqualTo((issue.Location.Line, issue.Location.Column));
            }
        }
    }

    public static IEnumerable<Func<string>> Documents()
    {
        foreach (var instance in TreeFixture.Instances)
        {
            var captured = instance;
            yield return () => captured;
        }

        yield return () => "<root><item>1</item>text<group><inner>1</inner>more<inner>x</inner></group></root>";
        yield return () =>
            "<root><item>1</item><group id=\"a\"><inner>1</inner></group><group id=\"a\"><inner>1</inner></group></root>";
    }
}
