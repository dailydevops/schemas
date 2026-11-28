namespace NetEvolve.Schemas.Xml.Tests.Unit.Datatypes.Core;

using System.Reflection;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Core;

/// <summary>Asserts that the fused check and map path of every built-in mapping matches the separate Check and Map path.</summary>
public class FusedMappingParityTests
{
    private static readonly MethodInfo SummaryMethod = typeof(FusedMappingParityTests).GetMethod(
        nameof(Summary),
        BindingFlags.NonPublic | BindingFlags.Static
    )!;

    // Valid forms, lexical errors, facet violations, the 18/19 digit boundary and the missing timezone.
    public static IEnumerable<(string Type, string Lexical)> Cases()
    {
        string[] decimals =
        [
            "0",
            "-0",
            "+1.50",
            "  12.5  ",
            ".5",
            "5.",
            "1e3",
            "",
            "+",
            "-",
            ".",
            "12x",
            "1.2.3",
            "123456789012345678",
            "-123456789012345678",
            "1234567890123456789",
            "+1234567890123456789",
            new string('9', 10_000),
        ];
        foreach (
            var type in new[]
            {
                "decimal",
                "integer",
                "long",
                "int",
                "short",
                "byte",
                "nonNegativeInteger",
                "unsignedLong",
                "negativeInteger",
            }
        )
        {
            foreach (var lexical in decimals)
            {
                yield return (type, lexical);
            }

            foreach (
                var lexical in new[]
                {
                    "127",
                    "128",
                    "-128",
                    "-129",
                    "65535",
                    "65536",
                    "9223372036854775807",
                    "9223372036854775808",
                    "18446744073709551615",
                    "18446744073709551616",
                    "1000000000000000000",
                }
            )
            {
                yield return (type, lexical);
            }
        }

        foreach (var type in new[] { "double", "float" })
        {
            foreach (
                var lexical in new[]
                {
                    "0",
                    "-0",
                    "1.5E3",
                    "1e-400",
                    "1e400",
                    "3.4028235E38",
                    "3.5E38",
                    "INF",
                    "-INF",
                    "NaN",
                    "inf",
                    "",
                    "1.",
                    ".1",
                    "1e",
                    "abc",
                    " 2.5 ",
                    "0.1",
                    "16777217",
                }
            )
            {
                yield return (type, lexical);
            }
        }

        foreach (var type in new[] { "dateTime", "dateTimeStamp" })
        {
            foreach (
                var lexical in new[]
                {
                    "2024-02-29T12:00:00",
                    "2024-02-29T12:00:00Z",
                    "2024-02-29T12:00:00+05:30",
                    "2024-02-30T12:00:00Z",
                    "2024-02-29T24:00:00Z",
                    "2024-02-29T12:00:00.123456789Z",
                    "-0001-01-01T00:00:00Z",
                    "2024-02-29",
                    "",
                    "x",
                    " 2024-02-29T12:00:00 ",
                }
            )
            {
                yield return (type, lexical);
            }
        }

        foreach (var type in new[] { "duration", "yearMonthDuration", "dayTimeDuration" })
        {
            foreach (
                var lexical in new[]
                {
                    "P1Y2M3DT4H5M6S",
                    "-P1D",
                    "PT0S",
                    "P",
                    "PT",
                    "P1.5Y",
                    "PT1.5S",
                    "P1Y1D",
                    "PT1H",
                    "1D",
                    "",
                    "P-1D",
                }
            )
            {
                yield return (type, lexical);
            }
        }

        foreach (var type in new[] { "hexBinary", "base64Binary" })
        {
            foreach (
                var lexical in new[]
                {
                    "",
                    "0FB7",
                    "0fb7",
                    "0FB",
                    "0G",
                    "AAEC",
                    "AAE=",
                    "AA==",
                    "AAE",
                    "A A E C",
                    "=AAA",
                    "AA=A",
                }
            )
            {
                yield return (type, lexical);
            }
        }
    }

    [Test]
    [MethodDataSource(nameof(Cases))]
    public async Task ParseAndValidate_FusedAndSeparatePath_ReturnTheSameResult(string type, string lexical)
    {
        ArgumentNullException.ThrowIfNull(type);
        ArgumentNullException.ThrowIfNull(lexical);
        var descriptor = BuiltInDatatypes.For(XsdVersion.V11).Get(type);
        var valueType = descriptor.GetType().GetGenericArguments()[0];

        var fused = (string)SummaryMethod.MakeGenericMethod(valueType).Invoke(null, [descriptor, false, lexical])!;
        var separate = (string)SummaryMethod.MakeGenericMethod(valueType).Invoke(null, [descriptor, true, lexical])!;

        await Assert.That(fused).IsEqualTo(separate);
    }

    private static string Summary<T>(AtomicDescriptor<T> descriptor, bool separate, string lexical)
    {
        var subject = descriptor;
        if (separate)
        {
            var mapping =
                (ILexicalMapping<T>)
                    typeof(AtomicDescriptor<T>)
                        .GetField("_mapping", BindingFlags.NonPublic | BindingFlags.Instance)!
                        .GetValue(descriptor)!;
            subject = new AtomicDescriptor<T>(
                descriptor.Name,
                descriptor,
                new SeparateMapping<T>(mapping),
                descriptor.Whitespace,
                []
            );
        }

        var parse = subject.Parse(lexical);
        var validate = subject.Validate(lexical);
        return $"{parse.Success}|{parse.Error}|{parse.ErrorOffset}|{(parse.Success ? parse.Value : default)}"
            + $"#{validate.IsValid}|{validate.Error}|{validate.FailedFacet}|{validate.Offset}";
    }

    /// <summary>Hides the fused interface of a mapping so the descriptor takes the separate Check and Map path.</summary>
    private sealed class SeparateMapping<T>(ILexicalMapping<T> inner) : ILexicalMapping<T>
    {
        public WhitespaceMode Whitespace => inner.Whitespace;

        public bool IsOrdered => inner.IsOrdered;

        public DatatypeError Check(ReadOnlySpan<char> normalized, XsdVersion version, out int errorOffset) =>
            inner.Check(normalized, version, out errorOffset);

        public T Map(ReadOnlySpan<char> normalized, WhitespaceMode mode, XsdVersion version) =>
            inner.Map(normalized, mode, version);

        public XsdOrder Compare(in T a, in T b, XsdVersion version) => inner.Compare(in a, in b, version);

        public bool IsIdentical(in T a, in T b, XsdVersion version) => inner.IsIdentical(in a, in b, version);
    }
}
