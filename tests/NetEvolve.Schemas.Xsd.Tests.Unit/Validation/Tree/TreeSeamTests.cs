namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Tree;

using System;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.Xsd.Validation.Annotations;
using NetEvolve.Schemas.Xsd.Validation.Assembly;
using NetEvolve.Schemas.Xsd.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Tree;
using XsdVersion = NetEvolve.Schemas.Xml.Datatypes.Contracts.XsdVersion;

public class TreeSeamTests
{
    [Test]
    public async Task Validate_Document_CallsTheSharedCoreOnce_WithTheNodeAndTheToken(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var core = new RecordingCore();
        var input = TreeFixture.Load("<root/>");
        using var source = new CancellationTokenSource();

        _ = new TreeValidator(core).Validate(input.Document, source.Token);

        await Assert.That(core.Nodes.Count).IsEqualTo(1);
        await Assert.That(core.Nodes[0]).IsEqualTo(input.Document);
        await Assert.That(core.Tokens[0]).IsEqualTo(source.Token);
    }

    [Test]
    public async Task Validate_Subtree_CallsTheSharedCoreWithTheElement(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var core = new RecordingCore();
        var input = TreeFixture.Load("<root><item>1</item></root>");
        var element = TreeFixture.Element(input, "item");

        _ = new TreeValidator(core).Validate(element, cancellationToken);

        await Assert.That(core.Nodes.Count).IsEqualTo(1);
        await Assert.That(core.Nodes[0]).IsEqualTo(element);
    }

    [Test]
    public async Task Validate_ReturnsTheResultOfTheCore(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var core = new RecordingCore();

        var result = new TreeValidator(core).Validate(TreeFixture.Load("<root/>").Document, cancellationToken);

        await Assert.That(result.Result).IsSameReferenceAs(core.Result);
    }

    [Test]
    public async Task TreeNamespace_HasNoTreeWalker(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var names = new List<string>();
        foreach (var type in typeof(TreeValidator).Assembly.GetTypes())
        {
            if (string.Equals(type.Namespace, typeof(TreeValidator).Namespace, StringComparison.Ordinal))
            {
                names.Add(type.Name);
            }
        }

        await Assert.That(names.Count).IsGreaterThan(0);
        foreach (var name in names)
        {
            await Assert.That(name.Contains("Walk", StringComparison.Ordinal)).IsFalse();
            await Assert.That(name.Contains("Travers", StringComparison.Ordinal)).IsFalse();
            await Assert.That(name.Contains("Visit", StringComparison.Ordinal)).IsFalse();
            await Assert.That(name.Contains("Pump", StringComparison.Ordinal)).IsFalse();
        }
    }

    [Test]
    public async Task Constructor_NullCore_Throws() =>
        await Assert.That(() => new TreeValidator(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task Validate_DefaultNode_Throws(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validator = new TreeValidator(new RecordingCore());

        await Assert.That(() => validator.Validate(default, cancellationToken)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Validate_NodeThatIsNeitherDocumentNorElement_Throws(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var validator = new TreeValidator(new RecordingCore());
        var attribute = TreeFixture.Element(TreeFixture.Load("<root a=\"1\"/>"), "root").FirstAttribute;

        await Assert.That(() => validator.Validate(attribute, cancellationToken)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task IssueLog_IndexOutOfRange_Throws(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var log = new TreeIssueLog();
        log.Add(1);

        await Assert.That(log[0]).IsEqualTo(1);
        await Assert.That(() => log[1]).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => log.Add(-1)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task ValidateTree_Document_CallsTheSharedCoreOnce_WithTheDocumentNode(
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        var core = new RecordingCore();
        var input = TreeFixture.Load("<root/>");
        using var source = new CancellationTokenSource();

        _ = Validator(core).ValidateTree(input, source.Token);

        await Assert.That(core.Nodes.Count).IsEqualTo(1);
        await Assert.That(core.Nodes[0]).IsEqualTo(input.Document);
        await Assert.That(core.Tokens[0]).IsEqualTo(source.Token);
    }

    [Test]
    public async Task ValidateTree_Element_CallsTheSharedCoreOnce_WithTheElement(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var core = new RecordingCore();
        var input = TreeFixture.Load("<root><item>1</item></root>");
        var element = TreeFixture.Element(input, "item");

        _ = Validator(core).ValidateTree(input, element, cancellationToken);

        await Assert.That(core.Nodes.Count).IsEqualTo(1);
        await Assert.That(core.Nodes[0]).IsEqualTo(element);
    }

    [Test]
    public async Task ValidateTree_NullDocument_Throws(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert
            .That(() => Validator(new RecordingCore()).ValidateTree(null!, cancellationToken))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ValidateTree_DefaultElement_Throws(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var input = TreeFixture.Load("<root/>");

        await Assert
            .That(() => Validator(new RecordingCore()).ValidateTree(input, default, cancellationToken))
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task ValidateTree_ElementOfAnotherTree_Throws(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var input = TreeFixture.Load("<root/>");
        var other = TreeFixture.Element(TreeFixture.Load("<root/>"), "root");
        var core = new RecordingCore();

        await Assert
            .That(() => Validator(core).ValidateTree(input, other, cancellationToken))
            .Throws<ArgumentException>();
        await Assert.That(core.Nodes.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ValidateTree_NodeThatIsNotAnElement_Throws(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var input = TreeFixture.Load("<root a=\"1\"/>");
        var attribute = TreeFixture.Element(input, "root").FirstAttribute;

        await Assert
            .That(() => Validator(new RecordingCore()).ValidateTree(input, attribute, cancellationToken))
            .Throws<ArgumentException>();
    }

    private static XsdValidator Validator(IValidationCore core) =>
        new(core, XsdVersion.V11, ValidationLimits.Default, DenyAllResourceResolver.Instance);

    private sealed class RecordingCore : IValidationCore
    {
        public ValidationResult Result { get; } = ValidationResult.Valid;

        public List<XmlNode> Nodes { get; } = [];

        public List<CancellationToken> Tokens { get; } = [];

        public ISimpleValueAssessor Assessor { get; } = new DescriptorValueAssessor(XsdVersion.V11);

        public ValidationResult Validate(in DocumentSource source, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public ValidationResult ValidateTree(
            XmlNode node,
            IAnnotationSink annotations,
            TreeIssueLog issues,
            out XmlDocumentTree copy,
            CancellationToken cancellationToken
        )
        {
            cancellationToken.ThrowIfCancellationRequested();

            Nodes.Add(node);
            Tokens.Add(cancellationToken);
            copy = TreeFixture.Load("<root/>");
            return Result;
        }
    }
}
