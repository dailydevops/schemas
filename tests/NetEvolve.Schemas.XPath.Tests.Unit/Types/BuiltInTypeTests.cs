namespace NetEvolve.Schemas.XPath.Tests.Unit.Types;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

public class BuiltInTypeTests
{
    // The type hierarchy of XSD 1.1 Part 2 section 3 and XDM section 2.5 (xs:untyped, xs:untypedAtomic), written out by hand
    // so the registry-derived table of the implementation is checked against an independent source.
    private static readonly Dictionary<string, string> Parents = new(StringComparer.Ordinal)
    {
        ["anyType"] = "",
        ["anySimpleType"] = "anyType",
        ["untyped"] = "anyType",
        ["anyAtomicType"] = "anySimpleType",
        ["untypedAtomic"] = "anyAtomicType",
        ["string"] = "anyAtomicType",
        ["boolean"] = "anyAtomicType",
        ["decimal"] = "anyAtomicType",
        ["float"] = "anyAtomicType",
        ["double"] = "anyAtomicType",
        ["duration"] = "anyAtomicType",
        ["dateTime"] = "anyAtomicType",
        ["time"] = "anyAtomicType",
        ["date"] = "anyAtomicType",
        ["gYearMonth"] = "anyAtomicType",
        ["gYear"] = "anyAtomicType",
        ["gMonthDay"] = "anyAtomicType",
        ["gDay"] = "anyAtomicType",
        ["gMonth"] = "anyAtomicType",
        ["hexBinary"] = "anyAtomicType",
        ["base64Binary"] = "anyAtomicType",
        ["anyURI"] = "anyAtomicType",
        ["QName"] = "anyAtomicType",
        ["NOTATION"] = "anyAtomicType",
        ["normalizedString"] = "string",
        ["token"] = "normalizedString",
        ["language"] = "token",
        ["NMTOKEN"] = "token",
        ["Name"] = "token",
        ["NCName"] = "Name",
        ["ID"] = "NCName",
        ["IDREF"] = "NCName",
        ["ENTITY"] = "NCName",
        ["integer"] = "decimal",
        ["nonPositiveInteger"] = "integer",
        ["negativeInteger"] = "nonPositiveInteger",
        ["long"] = "integer",
        ["int"] = "long",
        ["short"] = "int",
        ["byte"] = "short",
        ["nonNegativeInteger"] = "integer",
        ["unsignedLong"] = "nonNegativeInteger",
        ["unsignedInt"] = "unsignedLong",
        ["unsignedShort"] = "unsignedInt",
        ["unsignedByte"] = "unsignedShort",
        ["positiveInteger"] = "nonNegativeInteger",
        ["yearMonthDuration"] = "duration",
        ["dayTimeDuration"] = "duration",
        ["dateTimeStamp"] = "dateTime",
    };

    private static readonly string[] NonAtomic = ["anyType", "anySimpleType", "untyped"];

    private static bool IsAncestorOrSelf(string type, string candidate)
    {
        for (var current = type; current.Length > 0; current = Parents[current])
        {
            if (string.Equals(current, candidate, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static IEnumerable<string> AtomicNames()
    {
        foreach (var name in Parents.Keys)
        {
            if (!NonAtomic.Contains(name))
            {
                yield return name;
            }
        }
    }

    [Test]
    public async Task Registry_AtomicTypes_AreExactlyTheExpectedBuiltIns()
    {
        var expected = new HashSet<string>(AtomicNames(), StringComparer.Ordinal);
        expected.Remove("untypedAtomic");
        var actual = new HashSet<string>(StringComparer.Ordinal);
        foreach (var descriptor in BuiltInDatatypes.For(XsdVersion.V11).All)
        {
            if (descriptor.Variety == DatatypeVariety.Atomic)
            {
                actual.Add(descriptor.Name);
            }
        }

        // anySimpleType is an atomic descriptor of the registry but not an atomic type of XDM; anyAtomicType is.
        actual.Remove("anySimpleType");
        actual.Remove("anyAtomicType");
        expected.Remove("anyAtomicType");

        await Assert.That(actual.SetEquals(expected)).IsTrue();
    }

    [Test]
    [Arguments("untyped", 1)]
    [Arguments("untypedAtomic", 2)]
    [Arguments("string", 3)]
    [Arguments("boolean", 4)]
    [Arguments("integer", 5)]
    [Arguments("double", 6)]
    [Arguments("decimal", 7)]
    [Arguments("float", 8)]
    [Arguments("duration", 9)]
    [Arguments("yearMonthDuration", 10)]
    [Arguments("dayTimeDuration", 11)]
    [Arguments("dateTime", 12)]
    [Arguments("date", 13)]
    [Arguments("time", 14)]
    [Arguments("anyURI", 15)]
    public async Task ExistingHandles_KeepTheirTypes(string name, int handle)
    {
        await Assert.That(XdmTypes.Get(name)).IsEqualTo(new XdmTypeId(handle));
        await Assert.That(XdmTypes.NameOf(new XdmTypeId(handle))).IsEqualTo(name);
    }

    [Test]
    public async Task WellKnownHandles_MapToTheNamedTypes()
    {
        await Assert.That(XdmTypes.Get("untyped")).IsEqualTo(XdmTypeId.Untyped);
        await Assert.That(XdmTypes.Get("untypedAtomic")).IsEqualTo(XdmTypeId.UntypedAtomic);
        await Assert.That(XdmTypes.Get("string")).IsEqualTo(XdmTypeId.XsString);
        await Assert.That(XdmTypes.Get("boolean")).IsEqualTo(XdmTypeId.Boolean);
        await Assert.That(XdmTypes.Get("integer")).IsEqualTo(XdmTypeId.XsInteger);
        await Assert.That(XdmTypes.Get("double")).IsEqualTo(XdmTypeId.XsDouble);
    }

    [Test]
    public async Task EveryType_HasADistinctHandleAndNameAndResolvesByName()
    {
        var handles = new HashSet<int>();
        foreach (var name in Parents.Keys)
        {
            var found = XdmTypes.TryGet(XdmTypes.SchemaNamespace, name, out var id);

            await Assert.That(found).IsTrue();
            await Assert.That(handles.Add(id.Value)).IsTrue();
            await Assert.That(XdmTypes.NameOf(id)).IsEqualTo(name);
        }
    }

    [Test]
    public async Task EveryBuiltInType_MatchesItsBaseChainAndNoUnrelatedType()
    {
        foreach (var type in Parents.Keys)
        {
            foreach (var other in Parents.Keys)
            {
                var actual = XdmTypes.IsDerivedFrom(XdmTypes.Get(type), XdmTypes.Get(other));

                await Assert.That(actual).IsEqualTo(IsAncestorOrSelf(type, other));
            }
        }
    }

    [Test]
    public async Task IntegerFamily_IsDerivedFromDecimalButNotFromDouble()
    {
        await Assert.That(XdmTypes.IsDerivedFrom(XdmTypes.Get("integer"), XdmTypes.Get("decimal"))).IsTrue();
        await Assert.That(XdmTypes.IsDerivedFrom(XdmTypes.Get("unsignedByte"), XdmTypes.Get("decimal"))).IsTrue();
        await Assert.That(XdmTypes.IsDerivedFrom(XdmTypes.Get("decimal"), XdmTypes.Get("integer"))).IsFalse();
        await Assert.That(XdmTypes.IsDerivedFrom(XdmTypes.Get("integer"), XdmTypes.Get("double"))).IsFalse();
        await Assert.That(XdmTypes.IsDerivedFrom(XdmTypes.Get("float"), XdmTypes.Get("double"))).IsFalse();
    }

    [Test]
    public async Task UntypedAtomic_IsDistinctFromString()
    {
        await Assert.That(XdmTypes.IsDerivedFrom(XdmTypes.Get("untypedAtomic"), XdmTypes.Get("string"))).IsFalse();
        await Assert.That(XdmTypes.IsDerivedFrom(XdmTypes.Get("string"), XdmTypes.Get("untypedAtomic"))).IsFalse();
    }

    [Test]
    public async Task IsAtomic_IsTrueForAtomicTypesOnly()
    {
        foreach (var name in Parents.Keys)
        {
            await Assert.That(XdmTypes.IsAtomic(XdmTypes.Get(name))).IsEqualTo(!NonAtomic.Contains(name));
        }
    }

    [Test]
    [Arguments("NMTOKENS")]
    [Arguments("IDREFS")]
    [Arguments("ENTITIES")]
    public async Task ListTypes_AreNamedButNotAtomic(string name)
    {
        var found = XdmTypes.TryGet(XdmTypes.SchemaNamespace, name, out var id);

        await Assert.That(found).IsTrue();
        await Assert.That(XdmTypes.IsAtomic(id)).IsFalse();
        await Assert.That(XdmTypes.IsDerivedFrom(id, XdmTypes.Get("anySimpleType"))).IsTrue();
        await Assert.That(XdmTypes.IsDerivedFrom(id, XdmTypes.Get("anyAtomicType"))).IsFalse();
    }

    [Test]
    [Arguments("http://www.w3.org/2001/XMLSchema", "nosuchtype")]
    [Arguments("urn:other", "integer")]
    [Arguments("", "integer")]
    public async Task TryGet_UnknownName_IsFalse(string schemaNamespace, string localName)
    {
        var found = XdmTypes.TryGet(schemaNamespace, localName, out var id);

        await Assert.That(found).IsFalse();
        await Assert.That(id).IsEqualTo(default(XdmTypeId));
    }

    [Test]
    public async Task UnknownHandle_IsDerivedOnlyFromItself()
    {
        var unknown = new XdmTypeId(9_999);

        await Assert.That(XdmTypes.IsDerivedFrom(unknown, unknown)).IsTrue();
        await Assert.That(XdmTypes.IsDerivedFrom(unknown, XdmTypes.Get("anyType"))).IsFalse();
        await Assert.That(XdmTypes.IsDerivedFrom(default, XdmTypes.Get("anyType"))).IsFalse();
        await Assert.That(XdmTypes.IsAtomic(unknown)).IsFalse();
        await Assert.That(XdmTypes.NameOf(unknown)).IsNull();
    }
}
