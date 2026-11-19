namespace NetEvolve.Schemas.Xml.Tests.Unit.Limits;

using System;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Limits;

public class XmlBudgetTests
{
    [Test]
    public async Task ElementDepth_ChargingUpToTheLimitSucceedsAndOneMoreRaisesXml1102() =>
        await AssertBudget(
            l => l.WithMaxElementDepth(5),
            XmlBudget.ElementDepth,
            XmlErrorCodes.XML1102,
            XmlReaderLimits.Default.MaxElementDepth
        );

    [Test]
    public async Task TextLength_ChargingUpToTheLimitSucceedsAndOneMoreRaisesXml1107() =>
        await AssertBudget(
            l => l.WithMaxTextNodeLength(5),
            XmlBudget.TextLength,
            XmlErrorCodes.XML1107,
            XmlReaderLimits.Default.MaxTextNodeLength
        );

    [Test]
    public async Task ExpansionCharacters_ChargingUpToTheLimitSucceedsAndOneMoreRaisesXml1109() =>
        await AssertBudget(
            l => l.WithMaxEntityExpansionCharacters(5),
            XmlBudget.ExpansionCharacters,
            XmlErrorCodes.XML1109,
            XmlReaderLimits.Default.MaxEntityExpansionCharacters
        );

    [Test]
    public async Task EntityDepth_ChargingUpToTheLimitSucceedsAndOneMoreRaisesXml1110() =>
        await AssertBudget(
            l => l.WithMaxEntityDepth(5),
            XmlBudget.EntityDepth,
            XmlErrorCodes.XML1110,
            XmlReaderLimits.Default.MaxEntityDepth
        );

    [Test]
    public async Task Charge_BulkAmountBeyondTheLimit_FailsWithoutChangingUsed()
    {
        var budget = new XmlBudget(10, XmlErrorCodes.XML1101);

        await Assert.That(budget.TryCharge(6)).IsTrue();
        await Assert.That(budget.TryCharge(5)).IsFalse();
        await Assert.That(budget.Used).IsEqualTo(6L);
        await Assert.That(budget.TryCharge(4)).IsTrue();
        await Assert.That(budget.TryCharge(long.MaxValue)).IsFalse();
    }

    [Test]
    public async Task ReleaseAndReset_GiveChargeBack()
    {
        var budget = new XmlBudget(2, XmlErrorCodes.XML1102);

        _ = budget.TryCharge(2);
        budget.Release(1);
        await Assert.That(budget.Used).IsEqualTo(1L);
        await Assert.That(budget.TryCharge(1)).IsTrue();
        await Assert.That(budget.TryCharge(1)).IsFalse();

        budget.Reset();
        await Assert.That(budget.Used).IsEqualTo(0L);
    }

    [Test]
    public async Task Charge_AllocatesNothing()
    {
        var budget = new XmlBudget(long.MaxValue, XmlErrorCodes.XML1101);
        for (var i = 0; i < 100; i++)
        {
            _ = budget.TryCharge(1);
            budget.Release(1);
        }

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
        {
            _ = budget.TryCharge(1);
            budget.Release(1);
        }

        var after = GC.GetAllocatedBytesForCurrentThread();

        await Assert.That(after - before).IsEqualTo(0L);
    }

    private static async Task AssertBudget(
        Func<XmlReaderLimits, XmlReaderLimits> configure,
        Func<XmlReaderLimits, XmlBudget> create,
        XmlErrorCode code,
        long defaultLimit
    )
    {
        var budget = create(configure(XmlReaderLimits.Default));
        var succeeded = 0;
        for (var i = 0; i < 5; i++)
        {
            if (budget.TryCharge(1))
            {
                succeeded++;
            }
        }

        await Assert.That(succeeded).IsEqualTo(5);
        await Assert.That(budget.TryCharge(1)).IsFalse();
        await Assert.That(budget.Code).IsSameReferenceAs(code);
        await Assert.That(budget.Limit).IsEqualTo(5L);
        await Assert.That(create(XmlReaderLimits.Default).Limit).IsEqualTo(defaultLimit);
    }
}
