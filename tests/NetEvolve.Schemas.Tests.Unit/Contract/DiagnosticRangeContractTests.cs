namespace NetEvolve.Schemas.Tests.Unit.Contract;

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;

public class DiagnosticRangeContractTests
{
    private static readonly (string Name, int First, int Last)[] Approved =
    [
        ("Core", 1, 999),
        ("Xml", 1000, 1999),
        ("XPath", 2000, 2999),
        ("Schematron", 3000, 3999),
        ("Xsd", 4000, 4999),
        ("DependencyInjection", 5000, 5999),
        ("Reserved", 6000, 8999),
        ("Consumer", 9000, 9999),
    ];

    private static readonly (string Name, int Number)[] CoreCodes =
    [
        ("InputSizeLimitExceeded", 1),
        ("DepthLimitExceeded", 2),
        ("AttributeLimitExceeded", 3),
        ("EntityExpansionLimitExceeded", 4),
        ("EntityDepthLimitExceeded", 5),
        ("ExternalResourceLimitExceeded", 6),
        ("ResourceSizeLimitExceeded", 7),
        ("ResourceDenied", 100),
        ("ResourceBaseUriMissing", 101),
        ("ResourceNotFound", 102),
        ("ResourceReferenceInvalid", 103),
    ];

    private static DiagnosticCodeRange RangeOf(string name) =>
        (DiagnosticCodeRange)typeof(DiagnosticRanges).GetProperty(name)!.GetValue(null)!;

    [Test]
    public async Task Ranges_MatchApprovedDesign()
    {
        foreach (var (name, first, last) in Approved)
        {
            var range = RangeOf(name);
            await Assert.That(range.First).IsEqualTo(first);
            await Assert.That(range.Last).IsEqualTo(last);
        }

        await Assert
            .That(typeof(DiagnosticRanges).GetProperties(BindingFlags.Public | BindingFlags.Static))
            .Count()
            .IsEqualTo(Approved.Length);
    }

    [Test]
    public async Task Ranges_AreDisjointAndOrdered()
    {
        var previousLast = 0;
        foreach (var (name, _, _) in Approved)
        {
            var range = RangeOf(name);
            await Assert.That(range.First).IsGreaterThan(previousLast);
            previousLast = range.Last;
        }
    }

    [Test]
    public async Task Contains_IsInclusiveOnBothEnds()
    {
        var range = new DiagnosticCodeRange(1000, 1999);

        await Assert.That(range.Contains(new DiagnosticCode(1000))).IsTrue();
        await Assert.That(range.Contains(new DiagnosticCode(1999))).IsTrue();
        await Assert.That(range.Contains(new DiagnosticCode(999))).IsFalse();
        await Assert.That(range.Contains(new DiagnosticCode(2000))).IsFalse();
    }

    [Test]
    public async Task CoreDiagnosticCodes_MatchRecordedListAndLieInCoreRange()
    {
        var fields = typeof(CoreDiagnosticCodes).GetFields(BindingFlags.Public | BindingFlags.Static);
        var seen = new HashSet<int>();

        await Assert.That(fields).Count().IsEqualTo(CoreCodes.Length);
        foreach (var (name, number) in CoreCodes)
        {
            var code = (DiagnosticCode)typeof(CoreDiagnosticCodes).GetField(name)!.GetValue(null)!;
            await Assert.That(code.Number).IsEqualTo(number);
            await Assert.That(DiagnosticRanges.Core.Contains(code)).IsTrue();
            await Assert.That(seen.Add(code.Number)).IsTrue();
        }
    }

    [Test]
    public async Task Constructors_OutOfRange_Throw()
    {
        await Assert.That(() => new DiagnosticCode(0)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => new DiagnosticCode(10000)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => new DiagnosticCodeRange(5, 4)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => new DiagnosticCodeRange(0, 4)).Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => new DiagnosticCodeRange(1, 10000)).Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task ToString_UsesPrefixAndFourDigits()
    {
        await Assert.That(new DiagnosticCode(100).ToString()).IsEqualTo("NES0100");
        await Assert.That(new DiagnosticCodeRange(1000, 1999).ToString()).IsEqualTo("NES1000-NES1999");
        await Assert.That(default(DiagnosticCode).ToString()).IsEqualTo("NES0000");
    }

    [Test]
    public async Task DefaultCode_EqualsOnlyDefaultAndLiesInNoRange()
    {
        var unassigned = default(DiagnosticCode);

        await Assert.That(unassigned.Number).IsEqualTo(0);
        await Assert.That(unassigned == default).IsTrue();
        await Assert.That(unassigned.Equals(new DiagnosticCode(1))).IsFalse();
        await Assert.That(unassigned.GetHashCode()).IsEqualTo(0);
        foreach (var (name, _, _) in Approved)
        {
            await Assert.That(RangeOf(name).Contains(unassigned)).IsFalse();
        }
    }
}
