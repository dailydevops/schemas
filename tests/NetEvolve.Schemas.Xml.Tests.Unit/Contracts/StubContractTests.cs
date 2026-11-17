namespace NetEvolve.Schemas.Xml.Tests.Unit.Contracts;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Interop;
using XmlNode = NetEvolve.Schemas.Xml.Contracts.XmlNode;

public class StubContractTests
{
    // AP17: the fifteen limits in table order, each with its error code.
    private static readonly (string Name, Type Type)[] Limits =
    [
        ("MaxDocumentCharacters", typeof(long)),
        ("MaxElementDepth", typeof(int)),
        ("MaxAttributesPerElement", typeof(int)),
        ("MaxNamespaceDeclarationsPerElement", typeof(int)),
        ("MaxNameLength", typeof(int)),
        ("MaxTokenLength", typeof(int)),
        ("MaxTextNodeLength", typeof(int)),
        ("MaxNodeCount", typeof(int)),
        ("MaxEntityExpansionCharacters", typeof(long)),
        ("MaxEntityDepth", typeof(int)),
        ("MaxDistinctNames", typeof(int)),
        ("MaxAttributeValueLength", typeof(int)),
        ("MaxStartTagCharacters", typeof(int)),
        ("MaxXmlDeclarationLength", typeof(int)),
        ("MaxDoctypeLength", typeof(int)),
    ];

    private static async Task AssertStub(Func<object?> call, string workPackage)
    {
        var failure = Assert.Throws<NotImplementedException>(() => call());
        await Assert.That(failure.Message).Contains(workPackage);
    }

    private static Exception? Unwrap(Action call)
    {
        try
        {
            call();
            return null;
        }
        catch (TargetInvocationException ex)
        {
            return ex.InnerException;
        }
    }

    [Test]
    public async Task XmlReaderLimits_IsSealedWithTheFifteenApprovedLimitsAndWithMethods()
    {
        var type = typeof(XmlReaderLimits);
        var failures = new List<string>();

        foreach (var (name, limitType) in Limits)
        {
            var property = type.GetProperty(name);
            var with = type.GetMethod("With" + name, [limitType]);
            if (property is null || property.PropertyType != limitType || property.CanWrite)
            {
                failures.Add(name);
            }

            if (with is null || with.ReturnType != type)
            {
                failures.Add("With" + name);
            }
        }

        await Assert.That(type.IsSealed).IsTrue();
        await Assert.That(failures).IsEmpty();
        await Assert.That(type.GetProperties(BindingFlags.Public | BindingFlags.Instance)).Count().IsEqualTo(15);
        await Assert.That(type.GetConstructors()).IsEmpty();
        await Assert.That(type.GetProperty(nameof(XmlReaderLimits.Default))!.PropertyType).IsEqualTo(type);
    }

    [Test]
    public async Task XmlReaderLimits_EveryMemberThrowsUntilXml08Lands()
    {
        var type = typeof(XmlReaderLimits);
        var failures = new List<string>();
        var instance = (XmlReaderLimits)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);

        foreach (var (name, limitType) in Limits)
        {
            var getter = type.GetProperty(name)!.GetMethod!;
            var with = type.GetMethod("With" + name, [limitType])!;
            var argument = Convert.ChangeType(1, limitType, System.Globalization.CultureInfo.InvariantCulture);
            if (Unwrap(() => getter.Invoke(instance, null)) is not NotImplementedException)
            {
                failures.Add(name);
            }

            if (Unwrap(() => with.Invoke(instance, [argument])) is not NotImplementedException)
            {
                failures.Add("With" + name);
            }
        }

        await Assert.That(failures).IsEmpty();
        await AssertStub(() => XmlReaderLimits.Default, "XML-08");
    }

    [Test]
    public async Task XmlReaderOptions_IsSealedAndEveryMemberThrowsUntilXml08Lands()
    {
        var options = (XmlReaderOptions)
            System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(XmlReaderOptions));

        await Assert.That(typeof(XmlReaderOptions).IsSealed).IsTrue();
        await Assert.That(typeof(XmlReaderOptions).GetConstructors()).IsEmpty();
        await AssertStub(() => XmlReaderOptions.Default, "XML-08");
        await AssertStub(() => options.Limits, "XML-08");
        await AssertStub(() => options.NameTable, "XML-08");
        await AssertStub(() => options.WithLimits(null!), "XML-08");
        await AssertStub(() => options.WithNameTable(null), "XML-08");
    }

    [Test]
    public async Task XmlInterop_EveryMemberThrowsUntilXml18Lands()
    {
        var node = new XmlNode(new Fakes.FakeXmlTree(), 0);
        using var reader = XmlReader.Create(new StringReader("<a/>"));
        var xdocument = new XDocument();
        var xmlDocument = new XmlDocument();
        var options = new XmlInteropOptions();

        await AssertStub(() => XmlInterop.FromXmlReader(reader), "XML-18");
        await AssertStub(() => XmlInterop.FromXmlReader(reader, options), "XML-18");
        await AssertStub(() => XmlInterop.ToXmlReader(node), "XML-18");
        await AssertStub(() => XmlInterop.FromXDocument(xdocument), "XML-18");
        await AssertStub(() => XmlInterop.ToXDocument(node), "XML-18");
        await AssertStub(() => XmlInterop.FromXmlDocument(xmlDocument), "XML-18");
        await AssertStub(() => XmlInterop.ToXmlDocument(node), "XML-18");
    }

    [Test]
    public async Task XmlInteropOptions_RejectsDocumentTypesByDefault()
    {
        await Assert.That(new XmlInteropOptions().AllowDocumentType).IsFalse();
        await Assert.That(new XmlInteropOptions(allowDocumentType: true).AllowDocumentType).IsTrue();
    }

    [Test]
    public async Task Stub_IsReachableFromXml() =>
        // The Schemas assembly exposes its internal Stub helper to the Xml assembly (InternalsVisibleTo); the message names the work package.
        await AssertStub(() => XmlInterop.ToXDocument(default), "XML-18");
}
