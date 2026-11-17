namespace NetEvolve.Schemas.Xml.Tests.Unit.Contracts;

using System;
using System.Collections.Generic;
using System.Reflection;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Interop;

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
    public async Task XmlInteropOptions_RejectsDocumentTypesByDefault()
    {
        await Assert.That(new XmlInteropOptions().AllowDocumentType).IsFalse();
        await Assert.That(new XmlInteropOptions(allowDocumentType: true).AllowDocumentType).IsTrue();
    }
}
