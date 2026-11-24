namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Delta3;

using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:parse-xml</c> and <c>fn:parse-xml-fragment</c> (Functions and Operators 3.1, 14.9): a string parsed into a document node
/// with the secure reader defaults of the XML package.
/// </summary>
public class ParseXmlFunctionTests
{
    [Test]
    [Arguments("parse-xml")]
    [Arguments("parse-xml-fragment")]
    public async Task Registry_ResolvesInXPath30AndLater_ButNotInXPath20(
        string local,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Delta3Harness.TryResolve(local, 1, out _, XPathVersion.V20)).IsFalse();
        await Assert.That(Delta3Harness.TryResolve(local, 1, out _, XPathVersion.V30)).IsTrue();
        await Assert.That(Delta3Harness.TryResolve(local, 1, out _, XPathVersion.V31)).IsTrue();
    }

    [Test]
    [Arguments("fn:parse-xml('<a/>')")]
    [Arguments("fn:parse-xml-fragment('<a/>')")]
    public async Task TryCompile_InXPath20_ReturnsXpst0017(string query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var statics = XPathStaticContext
            .Default.ToBuilder()
            .WithVersion(XPathVersion.V20)
            .WithNamespace("fn", "http://www.w3.org/2005/xpath-functions")
            .Build();

        await Assert.That(XPathExpression.TryCompile(query, statics).Diagnostic.Code).IsEqualTo("XPST0017");
    }

    [Test]
    public async Task ParseXml_ReturnsADocumentNodeWithTheDocumentElement(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = Delta3Harness.Call(
            "parse-xml",
            Delta3Harness.Str("<?xml version=\"1.0\"?><a x=\"1\"><b>t</b><!--c--></a>")
        );

        await Assert.That(result.Count).IsEqualTo(1);
        _ = result[0].TryGetNode(out var document);
        await Assert.That(document.Kind).IsEqualTo(XdmNodeKind.Document);
        var a = Delta3Harness.Child(document, 0);
        await Assert.That(a.Kind).IsEqualTo(XdmNodeKind.Element);
        await Assert.That(a.StringValue()).IsEqualTo("t");
        await Assert.That(Delta3Harness.Child(a, 1).Kind).IsEqualTo(XdmNodeKind.Comment);
        await Assert.That(document.BaseUri()).IsNull();
    }

    [Test]
    public async Task ParseXml_ForTheEmptySequence_ReturnsTheEmptySequence(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Delta3Harness.Call("parse-xml", XdmSequence.Empty).Count).IsEqualTo(0);
        await Assert.That(Delta3Harness.Call("parse-xml-fragment", XdmSequence.Empty).Count).IsEqualTo(0);
    }

    [Test]
    [Arguments("<a>")]
    [Arguments("<a></b>")]
    [Arguments("<a/><b/>")]
    [Arguments("text")]
    [Arguments("")]
    [Arguments("<a>&undefined;</a>")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"x\">]><a>&e;</a>")]
    public async Task ParseXml_ForInputThatIsNotAWellFormedDocument_RaisesFODC0006(
        string input,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Delta3Harness.ErrorOf("parse-xml", Delta3Harness.Str(input))).IsEqualTo("FODC0006");
    }

    [Test]
    [Arguments("<a/><b/>text", "Element|Element|Text")]
    [Arguments("text", "Text")]
    [Arguments("a<b/>c", "Text|Element|Text")]
    [Arguments("<!--c--><?pi d?>", "Comment|ProcessingInstruction")]
    [Arguments("<?xml version=\"1.0\"?><a/>", "Element")]
    [Arguments("<?xml version=\"1.0\" encoding=\"utf-8\"?>t", "Text")]
    [Arguments("", "")]
    public async Task ParseXmlFragment_ReturnsADocumentNodeWithTheTopLevelNodes(
        string input,
        string expected,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = Delta3Harness.Call("parse-xml-fragment", Delta3Harness.Str(input));

        await Assert.That(result.Count).IsEqualTo(1);
        _ = result[0].TryGetNode(out var document);
        await Assert.That(document.Kind).IsEqualTo(XdmNodeKind.Document);
        var kinds = new System.Collections.Generic.List<string>();
        for (var child = document.FirstChild(); !child.IsEmpty(); child = child.NextSibling())
        {
            kinds.Add(child.Kind.ToString());
        }

        await Assert.That(string.Join("|", kinds)).IsEqualTo(expected);
    }

    [Test]
    [Arguments("<a>")]
    [Arguments("<a></b>")]
    [Arguments("</w><x/><w>")]
    [Arguments("</w>")]
    [Arguments("<!DOCTYPE a [<!ENTITY e \"x\">]><a>&e;</a>")]
    [Arguments("<a>&undefined;</a>")]
    public async Task ParseXmlFragment_ForInputThatIsNotWellFormed_RaisesFODC0006(
        string input,
        CancellationToken cancellationToken
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Assert.That(Delta3Harness.ErrorOf("parse-xml-fragment", Delta3Harness.Str(input))).IsEqualTo("FODC0006");
    }

    [Test]
    public async Task ParseXmlFragment_KeepsTheStringValueOfTheContent(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = Delta3Harness.Call("parse-xml-fragment", Delta3Harness.Str("x<a xmlns=\"urn:x\">y</a>z"));

        _ = result[0].TryGetNode(out var document);
        await Assert.That(document.StringValue()).IsEqualTo("xyz");
        await Assert.That(Delta3Harness.Show(Delta3Harness.Call("path", result))).IsEqualTo("/");
        await Assert
            .That(Delta3Harness.Show(Delta3Harness.Call("path", Delta3Harness.Nodes(Delta3Harness.Child(document, 1)))))
            .IsEqualTo("/Q{urn:x}a[1]");
    }
}
