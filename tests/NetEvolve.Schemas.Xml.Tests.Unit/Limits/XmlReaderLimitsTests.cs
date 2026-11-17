namespace NetEvolve.Schemas.Xml.Tests.Unit.Limits;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;

public class XmlReaderLimitsTests
{
    private static readonly (
        string Name,
        long Default,
        Func<XmlReaderLimits, long> Get,
        Func<XmlReaderLimits, long, XmlReaderLimits> With
    )[] Table =
    [
        ("MaxDocumentCharacters", 134_217_728, l => l.MaxDocumentCharacters, (l, v) => l.WithMaxDocumentCharacters(v)),
        ("MaxElementDepth", 256, l => l.MaxElementDepth, (l, v) => l.WithMaxElementDepth((int)v)),
        (
            "MaxAttributesPerElement",
            256,
            l => l.MaxAttributesPerElement,
            (l, v) => l.WithMaxAttributesPerElement((int)v)
        ),
        (
            "MaxNamespaceDeclarationsPerElement",
            256,
            l => l.MaxNamespaceDeclarationsPerElement,
            (l, v) => l.WithMaxNamespaceDeclarationsPerElement((int)v)
        ),
        ("MaxNameLength", 1_024, l => l.MaxNameLength, (l, v) => l.WithMaxNameLength((int)v)),
        ("MaxTokenLength", 1_048_576, l => l.MaxTokenLength, (l, v) => l.WithMaxTokenLength((int)v)),
        ("MaxTextNodeLength", 16_777_216, l => l.MaxTextNodeLength, (l, v) => l.WithMaxTextNodeLength((int)v)),
        ("MaxNodeCount", 10_000_000, l => l.MaxNodeCount, (l, v) => l.WithMaxNodeCount((int)v)),
        (
            "MaxEntityExpansionCharacters",
            10_485_760,
            l => l.MaxEntityExpansionCharacters,
            (l, v) => l.WithMaxEntityExpansionCharacters(v)
        ),
        ("MaxEntityDepth", 16, l => l.MaxEntityDepth, (l, v) => l.WithMaxEntityDepth((int)v)),
        ("MaxDistinctNames", 1_048_576, l => l.MaxDistinctNames, (l, v) => l.WithMaxDistinctNames((int)v)),
        (
            "MaxAttributeValueLength",
            1_048_576,
            l => l.MaxAttributeValueLength,
            (l, v) => l.WithMaxAttributeValueLength((int)v)
        ),
        (
            "MaxStartTagCharacters",
            4_194_304,
            l => l.MaxStartTagCharacters,
            (l, v) => l.WithMaxStartTagCharacters((int)v)
        ),
        (
            "MaxXmlDeclarationLength",
            1_024,
            l => l.MaxXmlDeclarationLength,
            (l, v) => l.WithMaxXmlDeclarationLength((int)v)
        ),
        ("MaxDoctypeLength", 1_048_576, l => l.MaxDoctypeLength, (l, v) => l.WithMaxDoctypeLength((int)v)),
    ];

    [Test]
    public async Task Default_PinsEveryApprovedDefaultValue()
    {
        var actual = new List<string>();
        var expected = new List<string>();
        foreach (var (name, defaultValue, get, _) in Table)
        {
            actual.Add($"{name}={get(XmlReaderLimits.Default)}");
            expected.Add($"{name}={defaultValue}");
        }

        await Assert.That(Table.Length).IsEqualTo(15);
        await Assert.That(actual).IsEquivalentTo(expected);
    }

    [Test]
    public async Task With_ReturnsACopyAndLeavesTheSourceUnchanged()
    {
        var failures = new List<string>();
        foreach (var (name, defaultValue, get, with) in Table)
        {
            var source = XmlReaderLimits.Default;
            var copy = with(source, 7);
            if (ReferenceEquals(copy, source) || get(copy) != 7 || get(source) != defaultValue)
            {
                failures.Add(name);
            }

            foreach (var (otherName, otherDefault, otherGet, _) in Table)
            {
                if (!string.Equals(otherName, name, StringComparison.Ordinal) && otherGet(copy) != otherDefault)
                {
                    failures.Add(name + "->" + otherName);
                }
            }
        }

        await Assert.That(failures).IsEmpty();
    }

    [Test]
    public async Task With_ZeroOrNegativeValue_IsRejected()
    {
        var failures = new List<string>();
        foreach (var (name, _, get, with) in Table)
        {
            foreach (var value in new long[] { 0, -1, int.MinValue })
            {
                try
                {
                    _ = with(XmlReaderLimits.Default, value);
                    failures.Add($"{name}:{value}");
                }
                catch (ArgumentOutOfRangeException)
                {
                    // expected
                }
            }

            if (get(with(XmlReaderLimits.Default, 1)) != 1)
            {
                failures.Add(name + ":1");
            }
        }

        await Assert.That(failures).IsEmpty();
    }
}
