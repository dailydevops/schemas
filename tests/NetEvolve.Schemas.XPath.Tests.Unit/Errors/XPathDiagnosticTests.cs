namespace NetEvolve.Schemas.XPath.Tests.Unit.Errors;

using System;
using NetEvolve.Schemas.XPath.Errors;

public class XPathDiagnosticTests
{
    [Test]
    public async Task Equals_SameValues_AreEqualWithSameHashCode()
    {
        var left = XPathDiagnostic.Create(XPathErrorCodes.XPST0003, 1, 2);
        var right = XPathDiagnostic.Create(XPathErrorCodes.XPST0003, 1, 2);

        await Assert.That(left.Equals(right)).IsTrue();
        await Assert.That(left.Equals((object)right)).IsTrue();
        await Assert.That(left == right).IsTrue();
        await Assert.That(left.GetHashCode()).IsEqualTo(right.GetHashCode());
    }

    [Test]
    public async Task Equals_DifferentLocationOrCode_AreNotEqual()
    {
        var baseline = XPathDiagnostic.Create(XPathErrorCodes.XPST0003, 1, 2);

        await Assert.That(baseline != XPathDiagnostic.Create(XPathErrorCodes.XPST0003, 2, 2)).IsTrue();
        await Assert.That(baseline != XPathDiagnostic.Create(XPathErrorCodes.XPST0008, 1, 2)).IsTrue();
        await Assert.That(baseline.Equals((object?)null)).IsFalse();
    }

    [Test]
    public async Task Default_HasEmptyMessageAndNoCode()
    {
        var diagnostic = default(XPathDiagnostic);

        await Assert.That(diagnostic.Message).IsEmpty();
        await Assert.That(diagnostic.Code).IsEmpty();
    }

    [Test]
    public async Task Constructor_NullMessage_Throws() =>
        await Assert
            .That(() => new XPathDiagnostic(XPathErrorCodes.XPST0003, XPathErrorKind.Static, null!, 0, 0))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Exception_MessageConstructors_CarryEngineDiagnostic()
    {
        var inner = new InvalidOperationException();

        var parameterless = new XPathException();
        var withMessage = new XPathException("boom");
        var withInner = new XPathException("boom", inner);

        await Assert.That(parameterless.Message).IsNotNull();
        await Assert.That(withMessage.Diagnostic.Kind).IsEqualTo(XPathErrorKind.Engine);
        await Assert.That(withMessage.Diagnostic.Message).IsEqualTo("boom");
        await Assert.That(withMessage.Diagnostic.Offset).IsEqualTo(-1);
        await Assert.That(withInner.InnerException).IsSameReferenceAs(inner);
        await Assert.That(withInner.Diagnostic.Message).IsEqualTo("boom");
    }
}
