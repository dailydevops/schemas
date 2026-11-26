namespace NetEvolve.Schemas.Xsd.Tests.Unit.IdentityConstraints.Evaluation;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;
using NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;
using NetEvolve.Schemas.Xsd.Validation.Core;

public class ValueSpaceTests
{
    private static IXsdComponent BuiltIn(string name, XsdVersion version)
    {
        var types = XsdBuiltInTypes.For(version);
        for (var i = 0; i < types.Count; i++)
        {
            if (string.Equals(types[i].Name.LocalName, name, StringComparison.Ordinal))
            {
                return types[i];
            }
        }

        throw new InvalidOperationException(name);
    }

    private static SimpleTypeDefinition Definition(string name, XsdVersion version) =>
        SimpleTypeDefinition.FromDatatype(
            new NetEvolve.Schemas.XPath.Api.XPathQName(SchemaFixture.Xs, name),
            SchemaFixture.Where,
            BuiltInDatatypes.For(version).Get(name)
        );

    private static ValueSpace Space(XsdVersion version) => new(new DescriptorValueAssessor(version), version);

    private static async Task Check(
        ValueSpace space,
        IXsdComponent typeA,
        string a,
        IXsdComponent typeB,
        string b,
        bool expected
    )
    {
        await Assert.That(space.AreEqual(typeA, a, typeB, b)).IsEqualTo(expected);
        await Assert.That(space.AreEqual(typeB, b, typeA, a)).IsEqualTo(expected);
        if (expected)
        {
            await Assert.That(space.Hash(typeA, a)).IsEqualTo(space.Hash(typeB, b));
        }
    }

    [Test]
    // decimal, integer, and the types derived from them share the decimal value space
    [Arguments("decimal", "1", "decimal", "1.0", true)]
    [Arguments("decimal", "1", "decimal", "2", false)]
    [Arguments("decimal", "-0", "decimal", "0", true)]
    [Arguments("decimal", "+01.500", "decimal", "1.5", true)]
    [Arguments("integer", "01", "decimal", "1.0", true)]
    [Arguments("byte", "5", "short", "5", true)]
    [Arguments("decimal", "abc", "decimal", "abc", false)]
    [Arguments("decimal", "1", "double", "1", false)]
    [Arguments("decimal", "1", "string", "1", false)]
    // double and float: zero has one value, NaN equals itself, and the two types are different primitives
    [Arguments("double", "1.0E0", "double", "1", true)]
    [Arguments("double", "0", "double", "-0", true)]
    [Arguments("double", "NaN", "double", "NaN", true)]
    [Arguments("double", "INF", "double", "INF", true)]
    [Arguments("double", "INF", "double", "-INF", false)]
    [Arguments("double", "NaN", "double", "1", false)]
    [Arguments("double", "abc", "double", "abc", false)]
    [Arguments("float", "0.5", "float", "5E-1", true)]
    [Arguments("float", "0", "float", "-0", true)]
    [Arguments("float", "NaN", "float", "NaN", true)]
    [Arguments("float", "1", "double", "1", false)]
    // boolean
    [Arguments("boolean", "true", "boolean", "1", true)]
    [Arguments("boolean", "false", "boolean", "0", true)]
    [Arguments("boolean", "true", "boolean", "false", false)]
    [Arguments("boolean", "x", "boolean", "x", false)]
    // string and the types derived from it: the value is the text after the white space handling of each type
    [Arguments("string", "a", "string", "a", true)]
    [Arguments("string", "a", "string", "A", false)]
    [Arguments("string", "a  b", "string", "a b", false)]
    [Arguments("token", " a  b ", "token", "a b", true)]
    [Arguments("normalizedString", "a\tb", "normalizedString", "a b", true)]
    [Arguments("string", "a b", "token", "a  b", true)]
    [Arguments("string", "a  b", "token", "a b", false)]
    [Arguments("NCName", "x", "string", "x", true)]
    [Arguments("ID", "x", "IDREF", "x", true)]
    [Arguments("anySimpleType", "a", "anySimpleType", "a", true)]
    [Arguments("anyType", "a", "anyType", "a", true)]
    // anyURI is a primitive of its own
    [Arguments("anyURI", "urn:x", "anyURI", "urn:x", true)]
    [Arguments("anyURI", "urn:x", "string", "urn:x", false)]
    // QName and NOTATION are compared by their collapsed lexical form (no namespace context here)
    [Arguments("QName", "p:a", "QName", " p:a ", true)]
    [Arguments("QName", "p:a", "QName", "p:b", false)]
    [Arguments("QName", "a", "string", "a", false)]
    // date and time values: equal values of different timezones are equal, and each primitive is its own space
    [Arguments("dateTime", "2000-01-01T00:00:00Z", "dateTime", "2000-01-01T01:00:00+01:00", true)]
    [Arguments("dateTime", "2000-01-01T00:00:00", "dateTime", "2000-01-01T00:00:00", true)]
    [Arguments("dateTime", "2000-01-01T00:00:00", "dateTime", "2000-01-01T00:00:00Z", false)]
    [Arguments("dateTime", "2000-01-01T00:00:00Z", "dateTime", "2000-01-01T00:00:01Z", false)]
    [Arguments("date", "2000-01-01", "dateTime", "2000-01-01T00:00:00", false)]
    [Arguments("time", "12:00:00Z", "time", "13:00:00+01:00", true)]
    [Arguments("gYear", "2000", "gYear", "2000", true)]
    [Arguments("duration", "P1D", "duration", "PT24H", true)]
    [Arguments("duration", "P1M", "duration", "P30D", false)]
    // binary values
    [Arguments("hexBinary", "0A", "hexBinary", "0a", true)]
    [Arguments("hexBinary", "0A", "hexBinary", "0B", false)]
    [Arguments("base64Binary", "AQI=", "base64Binary", "AQI=", true)]
    [Arguments("hexBinary", "0A", "base64Binary", "0A", false)]
    // lists
    [Arguments("NMTOKENS", "a b", "NMTOKENS", "a  b", true)]
    [Arguments("NMTOKENS", "a b", "NMTOKENS", "a", false)]
    [Arguments("NMTOKENS", "a b", "NMTOKENS", "a c", false)]
    [Arguments("NMTOKENS", "a", "string", "a", false)]
    public async Task AreEqual_BuiltInTypes_FollowTheValueSpace(
        string typeA,
        string a,
        string typeB,
        string b,
        bool expected
    )
    {
        foreach (var version in new[] { XsdVersion.V10, XsdVersion.V11 })
        {
            await Check(Space(version), BuiltIn(typeA, version), a, BuiltIn(typeB, version), b, expected);
        }
    }

    [Test]
    public async Task AreEqual_TypesDerivedByRestriction_TakeThePrimitiveOfTheirBase()
    {
        var schema = new SchemaFixture();
        var version = XsdVersion.V11;
        var derived = Restrict(schema, Definition("decimal", version));
        var twice = Restrict(schema, derived);

        await Check(Space(version), twice, "1.0", Definition("decimal", version), "1", true);
        await Check(Space(version), twice, "1", Definition("string", version), "1", false);
    }

    [Test]
    public async Task AreEqual_ListsOfTheSameItemType_CompareItemByItem()
    {
        var schema = new SchemaFixture();
        var version = XsdVersion.V11;
        var list = SimpleTypeDefinition.List(
            default,
            SchemaFixture.Where,
            XsdDerivationMethod.None,
            Definition("decimal", version),
            [],
            schema.Reporter
        )!;

        var space = Space(version);
        await Check(space, list, "1 2.0", list, "1.0 2", true);
        await Check(space, list, "1 2", list, "1 3", false);
        await Check(space, list, "1 2", list, "1", false);
        await Check(space, list, "", list, "", true);
    }

    [Test]
    public async Task AreEqual_ListWithoutItemType_ComparesTheItemsAsStrings()
    {
        var version = XsdVersion.V11;
        var list = Definition("NMTOKENS", version);

        var space = Space(version);
        await Check(space, list, "a b", list, "a  b", true);
        await Check(space, list, "a b", list, "a c", false);
    }

    [Test]
    public async Task AreEqual_Unions_CompareByTheAcceptingMemberOfEachValue()
    {
        var schema = new SchemaFixture();
        var version = XsdVersion.V11;
        var union = SimpleTypeDefinition.Union(
            default,
            SchemaFixture.Where,
            XsdDerivationMethod.None,
            [Definition("decimal", version), Definition("string", version)],
            [],
            schema.Reporter
        )!;

        var space = Space(version);
        await Check(space, union, "1", union, "1.0", true);
        await Check(space, union, "x", union, "x", true);
        await Check(space, union, "x", union, "y", false);
        await Check(space, union, "1", union, "x", false);
        await Check(space, union, "1.0", BuiltIn("decimal", version), "1", true);
        await Check(space, union, "x", BuiltIn("decimal", version), "1", false);
    }

    [Test]
    public async Task AreEqual_UnionOfNoMemberAcceptsTheValue_FallsBackToTheCollapsedText()
    {
        var schema = new SchemaFixture();
        var version = XsdVersion.V11;
        var union = SimpleTypeDefinition.Union(
            default,
            SchemaFixture.Where,
            XsdDerivationMethod.None,
            [Definition("decimal", version)],
            [],
            schema.Reporter
        )!;

        var space = Space(version);
        await Check(space, union, "x", union, "x", true);
        await Check(space, union, "x", union, "y", false);
    }

    [Test]
    public async Task AreEqual_TypeWithAnUnknownName_ComparesTheText()
    {
        var space = Space(XsdVersion.V11);
        var unknown = SchemaFixture.Simple("noSuchType");

        await Check(space, unknown, "a", unknown, "a", true);
        await Check(space, unknown, "a", unknown, "b", false);
    }

    [Test]
    public async Task AreEqual_TypeOfAnotherNamespace_ComparesTheText()
    {
        var space = Space(XsdVersion.V11);
        var local = SimpleTypeDefinition.FromDatatype(
            new NetEvolve.Schemas.XPath.Api.XPathQName("urn:other", "decimal"),
            SchemaFixture.Where,
            BuiltInDatatypes.For(XsdVersion.V11).Get("decimal")
        );

        await Check(space, local, "1", local, "1.0", false);
        await Check(space, local, "1", local, "1", true);
    }

    [Test]
    public async Task AreEqual_ComponentThatIsNoSimpleType_ComparesTheText()
    {
        var space = Space(XsdVersion.V11);
        var component = new FakeComponent(XsdComponentKind.ComplexType, "t");

        await Check(space, component, "a", component, "a", true);
    }

    [Test]
    public async Task Hash_EqualDecimalsWithDifferentLexicalForms_HaveOneHash()
    {
        var space = Space(XsdVersion.V11);
        var type = BuiltIn("decimal", XsdVersion.V11);

        await Assert.That(space.Hash(type, "100")).IsEqualTo(space.Hash(type, "100.0"));
        await Assert.That(space.Hash(type, "1")).IsNotEqualTo(space.Hash(type, "2"));
    }

    [Test]
    [Arguments("dateTime", "2020-01-01T12:00:00Z", "2020-01-01T14:00:00+02:00")]
    [Arguments("dateTime", "2020-01-01T12:00:00Z", "2020-01-01T12:00:00.000+00:00")]
    [Arguments("dateTime", "2020-01-01T24:00:00", "2020-01-02T00:00:00")]
    [Arguments("date", "2020-01-01Z", "2020-01-01+00:00")]
    [Arguments("time", "12:00:00Z", "14:00:00+02:00")]
    [Arguments("gYear", "2020Z", "2020+00:00")]
    [Arguments("gMonthDay", "--01-01Z", "--01-01+00:00")]
    [Arguments("duration", "P1Y", "P12M")]
    [Arguments("duration", "P1D", "PT24H")]
    [Arguments("duration", "PT1.50S", "PT1.5S")]
    [Arguments("hexBinary", "0aFF", "0AFF")]
    [Arguments("base64Binary", "AQID", "AQ ID")]
    public async Task Hash_EqualDateTimeDurationAndBinaryValues_HaveOneHash(string type, string a, string b)
    {
        var space = Space(XsdVersion.V11);
        var component = BuiltIn(type, XsdVersion.V11);

        await Assert.That(space.AreEqual(component, a, component, b)).IsTrue();
        await Assert.That(space.Hash(component, a)).IsEqualTo(space.Hash(component, b));
    }

    [Test]
    [Arguments("dateTime", "2020-01-01T12:00:00Z", "2020-01-01T12:00:01Z")]
    [Arguments("date", "2020-01-01", "2020-01-02")]
    [Arguments("duration", "P1Y", "P2Y")]
    [Arguments("hexBinary", "0A", "0B")]
    public async Task Hash_DifferentDateTimeDurationAndBinaryValues_HaveDifferentHashes(string type, string a, string b)
    {
        var space = Space(XsdVersion.V11);
        var component = BuiltIn(type, XsdVersion.V11);

        await Assert.That(space.Hash(component, a)).IsNotEqualTo(space.Hash(component, b));
    }

    [Test]
    [Arguments("dateTime", 0)]
    [Arguments("duration", 1)]
    [Arguments("hexBinary", 2)]
    public async Task Hash_ManyDistinctValues_SpreadOverManyHashValues(string type, int shape)
    {
        var space = Space(XsdVersion.V11);
        var component = BuiltIn(type, XsdVersion.V11);

        foreach (var count in new[] { 100, 1000, 10000 })
        {
            var hashes = new HashSet<int>();
            for (var i = 0; i < count; i++)
            {
                var lexical = shape switch
                {
                    0 => new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                        .AddMinutes(i)
                        .ToString("yyyy-MM-ddTHH:mm:ssZ", System.Globalization.CultureInfo.InvariantCulture),
                    1 => $"PT{i}S",
                    _ => i.ToString("X8", System.Globalization.CultureInfo.InvariantCulture),
                };
                _ = hashes.Add(space.Hash(component, lexical));
            }

            // distinct values must not collapse into one bucket: at least nine in ten hash values are distinct
            await Assert.That(hashes.Count).IsGreaterThanOrEqualTo(count * 9 / 10 - 5);
        }
    }

    [Test]
    [Arguments("decimal", "abc")]
    [Arguments("double", "abc")]
    [Arguments("float", "abc")]
    [Arguments("boolean", "abc")]
    [Arguments("string", "\uFFFE")]
    public async Task Hash_OfAValueThatIsNotInTheLexicalSpace_IsZero(string type, string lexical)
    {
        var space = Space(XsdVersion.V11);

        await Assert.That(space.Hash(BuiltIn(type, XsdVersion.V11), lexical)).IsEqualTo(0);
    }

    [Test]
    public async Task AreEqual_StringsWithACharacterOutsideTheXmlSet_AreNotEqual()
    {
        var space = Space(XsdVersion.V11);
        var type = BuiltIn("string", XsdVersion.V11);

        await Assert.That(space.AreEqual(type, "\uFFFE", type, "\uFFFE")).IsFalse();
    }

    [Test]
    public async Task Guards_NullArguments_Throw()
    {
        var space = Space(XsdVersion.V11);
        var type = BuiltIn("string", XsdVersion.V11);

        await Assert.That(() => new ValueSpace(null!, XsdVersion.V11)).Throws<ArgumentNullException>();
        await Assert
            .That(() => new ValueSpace(new DescriptorValueAssessor(XsdVersion.V11), (XsdVersion)(-1)))
            .Throws<ArgumentOutOfRangeException>();
        await Assert
            .That(() => new ValueSpace(new DescriptorValueAssessor(XsdVersion.V11), (XsdVersion)99))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => space.Hash(null!, "a")).Throws<ArgumentNullException>();
        await Assert.That(() => space.AreEqual(null!, "a", type, "a")).Throws<ArgumentNullException>();
        await Assert.That(() => space.AreEqual(type, "a", null!, "a")).Throws<ArgumentNullException>();
    }

    private static SimpleTypeDefinition Restrict(SchemaFixture schema, SimpleTypeDefinition baseType) =>
        SimpleTypeDefinition.Restrict(
            default,
            SchemaFixture.Where,
            XsdDerivationMethod.None,
            baseType,
            [],
            schema.Reporter
        )!;
}
