namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.DefaultNamespace;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.IdentityConstraints.DefaultNamespace;
using NetEvolve.Schemas.Xsd.Versioning.Constructs;
using static NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.DefaultNamespace.DefaultNamespaceFixture;

/// <summary>
/// Strict 1.0 mode: the construct check of the schema document reports the attribute once, with the rule code of the attribute and
/// the location of the element that carries it; the qualification step reads no namespace from it, so nothing is reported twice.
/// </summary>
public class XPathDefaultNamespaceStrictTests
{
    private static readonly DiagnosticCode Strict10XPathDefaultNamespace = new(4030);

    private static IReadOnlyList<ValidationIssue> Check(string schema, XsdVersion version)
    {
        var collector = new ValidationIssueCollector(ValidationLimits.Default);
        Strict10Constructs.Check(Parse(schema), version, new XsdIssueReporter(collector));
        return collector.ToResult().Issues;
    }

    [Test]
    public async Task Strict10_AttributeOnSelectorAndField_IsASchemaErrorAtEachElement()
    {
        var schema = Schema(string.Empty, Unique("e", Tns, "@a", Tns));

        var issues = Check(schema, XsdVersion.V10);

        await Assert.That(issues).Count().IsEqualTo(2);
        await Assert.That(issues[0].Code).IsEqualTo(Strict10XPathDefaultNamespace);
        await Assert.That(issues[0].Location.Line).IsEqualTo(3);
        await Assert.That(issues[0].Location.Column).IsEqualTo(Column(schema, "<xs:selector"));
        await Assert.That(issues[1].Code).IsEqualTo(Strict10XPathDefaultNamespace);
        await Assert.That(issues[1].Location.Line).IsEqualTo(3);
        await Assert.That(issues[1].Location.Column).IsEqualTo(Column(schema, "<xs:field"));
    }

    [Test]
    public async Task Strict10_AttributeOnTheSchemaElement_IsReportedOnceAtTheSchema()
    {
        var schema = Schema(Attr(Tns), Unique("e", null, "@a", null));

        var issues = Check(schema, XsdVersion.V10);

        await Assert.That(issues).Count().IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(Strict10XPathDefaultNamespace);
        await Assert.That(issues[0].Location.Line).IsEqualTo(1);
    }

    [Test]
    public async Task Version11_AttributeOnSelectorAndField_IsNoError()
    {
        var issues = Check(Schema(Attr(Tns), Unique("e", Tns, "@a", Tns)), XsdVersion.V11);

        await Assert.That(issues).IsEmpty();
    }

    [Test]
    public async Task Strict10_QualificationStep_ReportsNothingAndReadsNoNamespace()
    {
        var schema = Schema(Attr(Tns), Unique("e", Tns, "@a", Tns));
        var constraint = Constraint(schema);

        XPathDefaultNamespace.Apply(constraint, Scope, XsdVersion.V10);

        await Assert.That(constraint.GetChildNode(0).FindAttribute("xpath")).IsEqualTo("e");
        await Assert.That(Check(schema, XsdVersion.V10)).Count().IsEqualTo(3);
    }

    [Test]
    public async Task Strict10_ConstraintWithoutTheAttribute_IsNoError()
    {
        var issues = Check(Schema(string.Empty, Unique("e", null, "@a", null)), XsdVersion.V10);

        await Assert.That(issues).IsEmpty();
    }

    // The one-based column of a start tag on the line of the constraint, which the issue of the attribute points at.
    private static int Column(string schema, string tag) =>
        schema.IndexOf(tag, StringComparison.Ordinal) - schema.IndexOf("<xs:unique", StringComparison.Ordinal) + 1;
}
