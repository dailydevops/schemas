namespace NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments;

using System;
using static NetEvolve.Schemas.Xsd.Tests.Unit.SchemaDocuments.SchemaFixture;

/// <summary>The <c>appinfo</c> and <c>documentation</c> elements build no syntax node but are still checked.</summary>
public class SchemaAnnotationContentTests
{
    [Test]
    [Arguments("<xs:appinfo/>")]
    [Arguments("<xs:documentation>text<b><xs:element/></b></xs:documentation>")]
    [Arguments("<xs:appinfo source=\"urn:s\" xml:lang=\"en\" xmlns:p=\"urn:p\"/><xs:documentation source=\"urn:d\"/>")]
    public async Task Annotation_HasNoChildNodes(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var (document, issues) = Parse(
            Open + "<xs:annotation>" + content + "</xs:annotation>\n<xs:element name=\"e\"/>" + Close
        );

        await Assert.That(issues.Count).IsEqualTo(0);
        await Assert.That(Find(document, "annotation").ChildCount).IsEqualTo(0);
        await Assert.That(document.Root!.ChildCount).IsEqualTo(2);
        await Assert.That(Find(document, "element").FindAttribute("name")).IsEqualTo("e");
    }

    [Test]
    [Arguments("<xs:annotation><xs:documentation ^id=\"x\"/></xs:annotation>")]
    [Arguments("<xs:annotation><xs:appinfo ^xs:source=\"x\"/></xs:annotation>")]
    public async Task WrongAttribute_IsReportedAtTheNameOfTheAttribute(string marked)
    {
        ArgumentNullException.ThrowIfNull(marked);

        var (text, line, column) = Unmark(Open + marked);
        var (_, issues) = Parse(text + Close);

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Constraint).IsEqualTo("src-annotation");
        await Assert.That(issues[0].Location.Line).IsEqualTo(line);
        await Assert.That(issues[0].Location.Column).IsEqualTo(column);
    }

    [Test]
    public async Task SecondElement_AfterAnIssueOnTheFirst_IsStillChecked()
    {
        var (_, issues) = Parse(
            Open
                + "<xs:annotation><xs:documentation bad=\"1\"><p/></xs:documentation>\n<xs:appinfo bad=\"2\"/></xs:annotation>"
                + Close
        );

        await Assert.That(issues.Count).IsEqualTo(2);
        await Assert.That(issues[0].Location.Line).IsEqualTo(2);
        await Assert.That(issues[1].Location.Line).IsEqualTo(3);
    }

    [Test]
    public async Task NamespaceDeclaration_OnTheElement_DoesNotLeakToTheAnnotation()
    {
        var (document, _) = Parse(
            Open + "<xs:annotation><xs:documentation xmlns:p=\"urn:p\"/></xs:annotation>" + Close
        );

        await Assert.That(Find(document, "annotation").TryResolvePrefix("p", out _)).IsFalse();
    }

    [Test]
    public async Task Annotation_AfterAnotherChild_IsStillRejected()
    {
        var (_, issues) = Parse(
            Open
                + "<xs:element name=\"e\"><xs:simpleType><xs:restriction base=\"xs:string\"/></xs:simpleType><xs:annotation><xs:documentation/></xs:annotation></xs:element>"
                + Close
        );

        await Assert.That(issues.Count).IsEqualTo(1);
    }
}
