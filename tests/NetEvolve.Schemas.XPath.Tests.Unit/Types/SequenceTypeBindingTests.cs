namespace NetEvolve.Schemas.XPath.Tests.Unit.Types;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static TypeFixture;

public class SequenceTypeBindingTests
{
    // The sequence type starts after "() instance of ".
    private const int TypeStart = 15;

    private static XPathException Bind(string type, XPathVersion? version = null)
    {
        var statics = version is { } v ? Statics.ToBuilder().WithVersion(v).Build() : Statics;
        try
        {
            _ = Matcher(type, statics);
        }
        catch (XPathException exception)
        {
            return exception;
        }

        throw new InvalidOperationException($"'{type}' was expected to raise a static error.");
    }

    [Test]
    [Arguments("xs:nosuchtype", 13)]
    [Arguments("xs:nosuchtype*", 13)]
    [Arguments("xs:Integer", 10)]
    [Arguments("p:integer", 9)]
    [Arguments("integer", 7)]
    [Arguments("xs:anyType", 10)]
    [Arguments("xs:anySimpleType", 16)]
    [Arguments("xs:untyped", 10)]
    [Arguments("xs:NMTOKENS", 11)]
    [Arguments("xs:IDREFS", 9)]
    public async Task AtomicType_NotAnAtomicBuiltIn_RaisesXpst0051AtTheTypeName(string type, int nameLength)
    {
        var exception = Bind(type);

        await Assert.That(exception.Diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0051);
        await Assert.That(exception.Diagnostic.Offset).IsEqualTo(TypeStart);
        await Assert.That(exception.Diagnostic.Length).IsEqualTo(nameLength);
    }

    [Test]
    [Arguments("element(*, xs:nosuchtype)")]
    [Arguments("element(a, p:integer)")]
    [Arguments("attribute(*, xs:nosuchtype)")]
    [Arguments("attribute(id, integer)")]
    [Arguments("document-node(element(*, xs:nosuchtype))")]
    public async Task ElementOrAttributeTest_UnknownTypeName_RaisesXpst0051(string type)
    {
        var exception = Bind(type);

        await Assert.That(exception.Diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0051);
        await Assert.That(exception.Diagnostic.Kind).IsEqualTo(XPathErrorKind.Static);
    }

    [Test]
    [Arguments("q:integer")]
    [Arguments("q:integer?")]
    [Arguments("element(*, q:integer)")]
    [Arguments("element(q:a)")]
    [Arguments("attribute(q:a)")]
    public async Task UnboundPrefix_RaisesXpst0081(string type) =>
        await Assert.That(Bind(type).Diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0081);

    // Deferred: schema import is not supported (#52), so no schema declaration is in scope.
    [Test]
    [Arguments("schema-element(a)")]
    [Arguments("schema-attribute(a)")]
    [Arguments("document-node(schema-element(a))")]
    public async Task Deferred_SchemaKindTest_RaisesXpst0008(string type)
    {
        var exception = Bind(type);

        await Assert.That(exception.Diagnostic.QName).IsEqualTo(XPathErrorCodes.XPST0008);
        await Assert.That(exception.Diagnostic.Kind).IsEqualTo(XPathErrorKind.Static);
    }

    [Test]
    public async Task UnprefixedTypeName_UsesTheDefaultElementNamespace()
    {
        var statics = Statics.ToBuilder().WithDefaultElementNamespace(Xs).Build();

        await Assert.That(Matcher("integer", statics).Matches(Seq(XdmItem.FromInteger(1)))).IsTrue();
        await Assert.That(Matcher("element(*, untyped)", statics).Matches(Seq(Node("a")))).IsTrue();
    }

    [Test]
    public async Task RebindingThePrefix_ResolvesTheTypeThroughThePrefixUri()
    {
        var statics = Statics.ToBuilder().WithNamespace("t", Xs).Build();

        await Assert.That(Matcher("t:integer", statics).Matches(Seq(XdmItem.FromInteger(1)))).IsTrue();
    }

    [Test]
    public async Task DateTimeStamp_IsAnInScopeTypeFromXPath31Only()
    {
        _ = Matcher("xs:dateTimeStamp", Statics.ToBuilder().WithVersion(XPathVersion.V31).Build());

        await Assert
            .That(Bind("xs:dateTimeStamp", XPathVersion.V20).Diagnostic.QName)
            .IsEqualTo(XPathErrorCodes.XPST0051);
        await Assert
            .That(Bind("xs:dateTimeStamp", XPathVersion.V30).Diagnostic.QName)
            .IsEqualTo(XPathErrorCodes.XPST0051);
    }

    [Test]
    public async Task EveryBuiltInAtomicName_BindsThroughTheXsPrefix()
    {
        foreach (var name in BuiltInTypeTests.AtomicNames())
        {
            if (string.Equals(name, "dateTimeStamp", StringComparison.Ordinal))
            {
                continue;
            }

            await Assert.That(Matcher($"xs:{name}*").Matches(XdmSequence.Empty)).IsTrue();
        }
    }
}
