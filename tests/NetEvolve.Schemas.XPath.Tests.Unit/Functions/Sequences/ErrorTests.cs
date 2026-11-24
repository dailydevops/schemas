namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;

using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary><c>fn:error</c> (Functions and Operators 1.0, 3.1.1): raises the error of its code, with FOER0000 as the default.</summary>
public class ErrorTests
{
    private static XdmSequence Code(string? namespaceName, string local) =>
        XdmSequence.Create(XdmItem.FromQName(new XdmQName(new XsdQName(namespaceName, local), null)));

    private static XPathException Raise(params XdmSequence[] arguments) =>
        Assert.Throws<XPathException>(() => SequencesHarness.CallWith("error", arguments));

    [Test]
    public async Task Error_IsRegisteredForZeroToThreeArguments()
    {
        for (var arity = 0; arity <= 4; arity++)
        {
            await Assert.That(SequencesHarness.TryResolve("error", arity, out _)).IsEqualTo(arity <= 3);
        }
    }

    [Test]
    public async Task Error_WithoutArguments_RaisesFoer0000()
    {
        var failure = Raise();

        await Assert.That(failure.Diagnostic.Code).IsEqualTo("FOER0000");
        await Assert.That(failure.Diagnostic.Kind).IsEqualTo(XPathErrorKind.Function);
    }

    [Test]
    public async Task Error_WithEmptyCode_RaisesFoer0000() =>
        await Assert.That(Raise(XdmSequence.Empty).Diagnostic.Code).IsEqualTo("FOER0000");

    [Test]
    public async Task Error_WithCode_RaisesThatCodeInItsNamespace()
    {
        var standard = Raise(Code(XPathErrorCode.StandardNamespace, "FOAR0001")).Diagnostic;
        var custom = Raise(Code("urn:x", "boom")).Diagnostic;
        var unqualified = Raise(Code(null, "boom")).Diagnostic;

        await Assert.That(standard.QName).IsEqualTo(new XPathErrorCode("FOAR0001"));
        await Assert.That(custom.QName).IsEqualTo(new XPathErrorCode("urn:x", "boom"));
        await Assert.That(custom.Kind).IsEqualTo(XPathErrorKind.Engine);
        await Assert.That(unqualified.QName).IsEqualTo(new XPathErrorCode(string.Empty, "boom"));
    }

    [Test]
    public async Task Error_WithDescription_UsesItAsTheMessage()
    {
        var failure = Raise(Code("urn:x", "boom"), XdmSequence.Create(XdmItem.FromString("it broke")));

        await Assert.That(failure.Diagnostic.Message).IsEqualTo("it broke");
        await Assert.That(failure.Message).IsEqualTo("it broke");
    }

    [Test]
    public async Task Error_WithErrorObject_StillRaisesTheCode()
    {
        var failure = Raise(
            Code("urn:x", "boom"),
            XdmSequence.Create(XdmItem.FromString("d")),
            XdmSequence.Create(XdmItem.FromString("object"))
        );

        await Assert.That(failure.Diagnostic.Code).IsEqualTo("{urn:x}boom");
    }

    [Test]
    public async Task Error_WithACodeThatIsNoQName_RaisesXpty0004() =>
        await Assert
            .That(Raise(XdmSequence.Create(XdmItem.FromString("FOER0000"))).Diagnostic.Code)
            .IsEqualTo("XPTY0004");

    [Test]
    public async Task Error_CodeOutsideTheTable_UsesTheCodeAsTheMessage() =>
        await Assert.That(Raise(Code("urn:x", "boom")).Diagnostic.Message).IsEqualTo("{urn:x}boom");
}
