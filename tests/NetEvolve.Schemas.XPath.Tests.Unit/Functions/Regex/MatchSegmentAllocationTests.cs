namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Regex;

using System.Text;
using System.Threading.Tasks;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.Xml.Regex.Operations;
using NetEvolve.Schemas.XPath.Functions.Regex;

/// <summary>The match-segment iterator allocates nothing per segment: the shared allocation assertion on a short and a 10,000-character input.</summary>
public class MatchSegmentAllocationTests
{
    private const int Warmups = 20;
    private const int LargeLength = 10_000;

    [Test]
    public async Task Iteration_OfALongInput_AllocatesNothingPerSegment()
    {
        var operations = RegexHarness.Compile("(a)(b)?");
        var small = Text(10);
        var large = Text(LargeLength);

        var forSmall = AllocationAssert.Measure(() => Walk(operations, small), warmups: Warmups);
        var forLarge = AllocationAssert.Measure(() => Walk(operations, large), warmups: Warmups);

        await Assert.That(large.Length).IsEqualTo(LargeLength);
        await Assert.That(Walk(operations, large)).IsGreaterThan(1_000);
        await Assert.That(forLarge).IsEqualTo(forSmall);
        await Assert.That(forLarge).IsEqualTo(0L);
    }

    [Test]
    public async Task Iteration_OfALongInputWithoutAMatch_AllocatesNothing()
    {
        var operations = RegexHarness.Compile("z+");
        var large = Text(LargeLength);

        var bytes = AllocationAssert.Measure(() => Walk(operations, large), warmups: Warmups);

        await Assert.That(Walk(operations, large)).IsEqualTo(1);
        await Assert.That(bytes).IsEqualTo(0L);
    }

    // Reads the bounds and the groups of every segment, the way a consumer does, and returns the number of segments.
    private static int Walk(RegexOperations operations, string input)
    {
        var count = 0;
        var segments = new MatchSegmentEnumerator(operations, input.AsSpan());
        try
        {
            while (segments.MoveNext())
            {
                var segment = segments.Current;
                count += segment.Length >= 0 && segment.Index >= 0 ? 1 : 0;
                if (segment.Kind == MatchSegmentKind.Match)
                {
                    _ = segment.TryGetGroup(1, out _);
                    _ = segment.GetGroupText(2).Length;
                }
            }
        }
        finally
        {
            segments.Dispose();
        }

        return count;
    }

    // Matches and gaps alternate: "ab" then "-" repeated, "a" without its optional "b" every third time.
    private static string Text(int length)
    {
        var text = new StringBuilder(length + 3);
        var index = 0;
        while (text.Length < length)
        {
            _ = text.Append(index % 3 == 2 ? "a-" : "ab-");
            index++;
        }

        return text.ToString(0, length);
    }
}
