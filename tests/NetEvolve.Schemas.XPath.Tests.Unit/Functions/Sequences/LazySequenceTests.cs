namespace NetEvolve.Schemas.XPath.Tests.Unit.Functions.Sequences;

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NetEvolve.Schemas.Testing.Allocations;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// A counting lazy source proves that <c>fn:exists</c> and <c>fn:empty</c> read at most one item and that <c>fn:count</c> does not
/// buffer the items (#123, AP3): the source is a range of integers that hands out one item at a time and reuses one enumerator, so
/// any allocation is the function's own.
/// </summary>
public class LazySequenceTests
{
    private const int Size = 100_000;

    [Test]
    [Arguments("exists", "boolean:true")]
    [Arguments("empty", "boolean:false")]
    public async Task ExistsAndEmpty_OverALargeLazyRange_ReadAtMostOneItem(string function, string expected)
    {
        using var source = new RangeSource(Size);

        var result = SequencesHarness.Show(SequencesHarness.CallWith(function, XdmSequence.FromLazy(source)));

        await Assert.That(result).IsEqualTo(expected);
        await Assert.That(source.Read).IsLessThanOrEqualTo(1);
    }

    [Test]
    [Arguments("exists", "boolean:false")]
    [Arguments("empty", "boolean:true")]
    public async Task ExistsAndEmpty_OverAnEmptyLazySource_ReadTheEndOnce(string function, string expected)
    {
        using var source = new RangeSource(0);

        var result = SequencesHarness.Show(SequencesHarness.CallWith(function, XdmSequence.FromLazy(source)));

        await Assert.That(result).IsEqualTo(expected);
        await Assert.That(source.Read).IsLessThanOrEqualTo(1);
    }

    [Test]
    public async Task Count_OverALazyRange_ReadsEveryItemOnceAndKeepsNone()
    {
        using var source = new RangeSource(Size);
        var sequence = XdmSequence.FromLazy(source);

        var result = SequencesHarness.Show(SequencesHarness.CallWith("count", sequence));

        await Assert.That(result).IsEqualTo("integer:100000");
        await Assert.That(source.Read).IsEqualTo(Size);
        await Assert.That(source.Enumerations).IsEqualTo(1);
    }

    [Test]
    public async Task Count_OverALazyRange_AllocatesNothing()
    {
        using var source = new RangeSource(Size);
        XdmSequence[] arguments = [XdmSequence.FromLazy(source)];
        var count = SequencesHarness.Resolve("count", 1);
        using var context = new XPathEvaluationContext();
        var result = XdmSequence.Empty;

        AllocationAssert.NoAllocation(() => result = count.Call(context, arguments), warmups: 50);

        await Assert.That(SequencesHarness.Show(result)).IsEqualTo("integer:100000");
    }

    [Test]
    [Arguments("exists")]
    [Arguments("empty")]
    public async Task ExistsAndEmpty_OverALazyRange_AllocateNothing(string name)
    {
        using var source = new RangeSource(Size);
        XdmSequence[] arguments = [XdmSequence.FromLazy(source)];
        var function = SequencesHarness.Resolve(name, 1);
        using var context = new XPathEvaluationContext();

        AllocationAssert.NoAllocation(() => _ = function.Call(context, arguments), warmups: 50);

        await Assert.That(source.Read).IsGreaterThan(0);
    }

    [Test]
    public async Task Count_OverAnItemArray_AllocatesNothing()
    {
        XdmSequence[] arguments = [XdmSequence.Create(new XdmItem[1000])];
        var count = SequencesHarness.Resolve("count", 1);
        using var context = new XPathEvaluationContext();
        var result = XdmSequence.Empty;

        AllocationAssert.NoAllocation(() => result = count.Call(context, arguments), warmups: 50);

        await Assert.That(SequencesHarness.Show(result)).IsEqualTo("integer:1000");
    }

    [Test]
    public async Task Count_ThenReadingTheSameLazySequence_StillSeesEveryItem()
    {
        using var source = new RangeSource(10);
        var sequence = XdmSequence.FromLazy(source);

        _ = SequencesHarness.CallWith("count", sequence);

        await Assert.That(sequence.Count).IsEqualTo(10);
        await Assert.That(sequence[9]).IsEqualTo(XdmItem.FromInteger(9));
    }

    [Test]
    public async Task Exists_OverALazySequenceThatWasAlreadyRead_UsesTheItemsItHolds()
    {
        using var source = new RangeSource(10);
        var sequence = XdmSequence.FromLazy(source);
        _ = sequence.Count;
        var read = source.Read;

        var result = SequencesHarness.Show(SequencesHarness.CallWith("exists", sequence));

        await Assert.That(result).IsEqualTo("boolean:true");
        await Assert.That(source.Read).IsEqualTo(read);
    }

    [Test]
    [Arguments("sum", "integer:4950")]
    [Arguments("max", "integer:99")]
    [Arguments("min", "integer:0")]
    public async Task OtherFunctions_OverALazySequence_ReadItWholeAndAnswer(string function, string expected)
    {
        using var source = new RangeSource(100);
        var sequence = XdmSequence.FromLazy(source);

        await Assert.That(SequencesHarness.Show(SequencesHarness.CallWith(function, sequence))).IsEqualTo(expected);
    }

    /// <summary>The integers 0 up to a count, one per step; one enumerator serves every enumeration, so that handing out items allocates nothing.</summary>
    private sealed class RangeSource(int count) : IEnumerable<XdmItem>, IEnumerator<XdmItem>
    {
        private int _next;

        public int Read { get; private set; }

        public int Enumerations { get; private set; }

        public XdmItem Current => XdmItem.FromInteger(_next - 1);

        object IEnumerator.Current => Current;

        public IEnumerator<XdmItem> GetEnumerator()
        {
            Enumerations++;
            _next = 0;
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool MoveNext()
        {
            if (_next >= count)
            {
                return false;
            }

            _next++;
            Read++;
            return true;
        }

        public void Reset() => _next = 0;

        public void Dispose() { }
    }
}
