namespace NetEvolve.Schemas.Benchmarks.Tests.Unit.XPath.Core;

using System.Security.Cryptography;
using System.Text;
using NetEvolve.Schemas.Benchmarks.XPath.Core;

/// <summary>The invoice of the XPath benchmarks (XP-56) is generated, so every run and every test leg must see the same bytes.</summary>
public class XPathBenchmarkInputTests
{
    // Recorded once; a change of the generator changes every recorded benchmark target.
    private const string ExpectedSha256 = "pending";

    [Test]
    public async Task Invoice_IsByteIdenticalAcrossGenerations()
    {
        var first = Encoding.UTF8.GetBytes(XPathBenchmarkInput.CreateInvoice());
        var second = Encoding.UTF8.GetBytes(XPathBenchmarkInput.CreateInvoice());

        await Assert.That(second).IsEquivalentTo(first);
        await Assert.That(first.Length).IsGreaterThan(20_000);
    }

    [Test]
    public async Task Invoice_MatchesTheRecordedHash_OnEveryTestLeg()
    {
        var bytes = Encoding.UTF8.GetBytes(XPathBenchmarkInput.CreateInvoice());
        var hash = Convert.ToHexString(SHA256.HashData(bytes));

        await Assert.That(hash).IsEqualTo(ExpectedSha256);
    }

    [Test]
    public async Task Expressions_AreListed_WithCompatibleOnesAsSubsetOfAll()
    {
        await Assert.That(XPathBenchmarkInput.Compatible).IsNotEmpty();
        await Assert.That(XPathBenchmarkInput.Extended).IsNotEmpty();
        await Assert.That(XPathBenchmarkInput.Compatible.Intersect(XPathBenchmarkInput.Extended)).IsEmpty();
    }
}
