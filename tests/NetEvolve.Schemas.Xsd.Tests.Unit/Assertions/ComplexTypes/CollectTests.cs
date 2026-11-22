namespace NetEvolve.Schemas.Xsd.Tests.Unit.Assertions.ComplexTypes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Assertions.ComplexTypes;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>The <c>assert</c> children of a complex type: directly, and under the extension or restriction of its content.</summary>
public class CollectTests
{
    private const string Document =
        "<xs:schema xmlns:xs=\"http://www.w3.org/2001/XMLSchema\" targetNamespace=\"urn:t\">"
        + "<xs:complexType name=\"A\"><xs:sequence/><xs:assert test=\"1 = 1\"/><xs:assert test=\"2 = 2\"/></xs:complexType>"
        + "<xs:complexType name=\"B\"><xs:complexContent><xs:extension base=\"A\"><xs:assert test=\"3 = 3\"/></xs:extension></xs:complexContent></xs:complexType>"
        + "<xs:complexType name=\"C\"><xs:simpleContent><xs:restriction base=\"xs:string\"><xs:assert test=\"$value = 'x'\"/></xs:restriction></xs:simpleContent></xs:complexType>"
        + "<xs:complexType name=\"D\"/>"
        + "<xs:complexType name=\"E\"><xs:sequence/><xs:assert test=\"a +\"/><xs:assert test=\"b\"/></xs:complexType>"
        + "</xs:schema>";

    private static (string[] Tests, IReadOnlyList<ValidationIssue> Issues) Collect(int index)
    {
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        var reporter = new XsdIssueReporter(collector);
        var root = SchemaDocumentParser.Parse(Document.AsSpan(), "file:///t.xsd", reporter).Root!;

        var found = ComplexTypeAssertion.Collect(
            root.GetChildNode(index),
            AssertionFixture.Scope,
            XsdVersion.V11,
            reporter
        );

        return ([.. found.Select(static assertion => assertion.Test)], collector.ToResult().Issues);
    }

    [Test]
    public async Task Collect_AssertChildrenOfTheType_InDocumentOrder()
    {
        var (tests, issues) = Collect(0);

        await Assert.That(tests).IsEquivalentTo(["1 = 1", "2 = 2"]);
        await Assert.That(issues).IsEmpty();
    }

    [Test]
    public async Task Collect_AssertChildrenOfTheExtension_AreTheirs()
    {
        var (tests, _) = Collect(1);

        await Assert.That(tests).IsEquivalentTo(["3 = 3"]);
    }

    [Test]
    public async Task Collect_AssertChildrenOfTheSimpleContentRestriction_AreTheirs()
    {
        var (tests, _) = Collect(2);

        await Assert.That(tests).IsEquivalentTo(["$value = 'x'"]);
    }

    [Test]
    public async Task Collect_TypeWithoutAssertions_IsEmpty()
    {
        var (tests, issues) = Collect(3);

        await Assert.That(tests).IsEmpty();
        await Assert.That(issues).IsEmpty();
    }

    [Test]
    public async Task Collect_AssertThatDoesNotCompile_IsReportedAndLeftOut()
    {
        var (tests, issues) = Collect(4);

        await Assert.That(tests).IsEquivalentTo(["b"]);
        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(XsdRuleCodes.XpathValid);
    }

    [Test]
    public async Task Collect_NullArguments_Throw()
    {
        var reporter = new XsdIssueReporter(new ValidationIssueCollector(ValidationLimits.Default));
        var root = SchemaDocumentParser.Parse(Document.AsSpan(), "file:///t.xsd", reporter).Root!;

        await Assert
            .That(() => ComplexTypeAssertion.Collect(null!, AssertionFixture.Scope, XsdVersion.V11, reporter))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => ComplexTypeAssertion.Collect(root, AssertionFixture.Scope, XsdVersion.V11, null!))
            .Throws<ArgumentNullException>();
    }
}
