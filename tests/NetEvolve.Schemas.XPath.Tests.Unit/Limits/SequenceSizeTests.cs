namespace NetEvolve.Schemas.XPath.Tests.Unit.Limits;

using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Paths;
using NetEvolve.Schemas.XPath.Limits;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Arithmetic;
using NetEvolve.Schemas.XPath.Xdm.Values;
using static LimitsFixture;

/// <summary>
/// The sequence size budget. A path result is the one place where an evaluation multiplies items today, so the list that collects
/// it is bounded when it grows. In the tree of the axis fixture <c>child::node()</c> of the element <c>a</c> delivers five nodes.
/// </summary>
public class SequenceSizeTests
{
    [Test]
    public async Task PathResult_ExactlyAtTheLimit_Succeeds()
    {
        var diagnostic = Evaluate("child::node()", XPathLimits.Default.WithMaxSequenceSize(5), out var result);

        await Assert.That(diagnostic.Code).IsEqualTo(string.Empty);
        await Assert.That(result.Count).IsEqualTo(5);
    }

    [Test]
    public async Task PathResult_OneItemBeyondTheLimit_EndsInTheCodedDiagnostic()
    {
        var diagnostic = Evaluate("child::node()", XPathLimits.Default.WithMaxSequenceSize(4), out var result);

        await Assert.That(diagnostic.QName).IsEqualTo(XPathLimitErrors.SequenceSizeExceeded);
        await Assert.That(diagnostic.Kind).IsEqualTo(XPathErrorKind.Engine);
        await Assert.That(result.IsEmpty).IsTrue();
    }

    [Test]
    public async Task PathResult_BeyondTheLimit_NamesTheLimitAndTheValue_AndLocatesThePath()
    {
        var diagnostic = Evaluate("child::node()", XPathLimits.Default.WithMaxSequenceSize(4), out _);

        await Assert.That(diagnostic.Message).Contains("MaxSequenceSize");
        await Assert.That(diagnostic.Message).Contains("4");
        await Assert.That(diagnostic.Offset).IsEqualTo(0);
        await Assert.That(diagnostic.Length).IsEqualTo(13);
    }

    [Test]
    public async Task LaterStepOfAPath_IsBoundedToo()
    {
        // The first step delivers three elements, the second their four children.
        var inside = Evaluate("child::*/child::node()", XPathLimits.Default.WithMaxSequenceSize(4), out var result);
        var beyond = Evaluate("child::*/child::node()", XPathLimits.Default.WithMaxSequenceSize(3), out _);

        await Assert.That(inside.Code).IsEqualTo(string.Empty);
        await Assert.That(result.Count).IsEqualTo(4);
        await Assert.That(beyond.QName).IsEqualTo(XPathLimitErrors.SequenceSizeExceeded);
        await Assert.That(beyond.Offset).IsEqualTo(0);
        await Assert.That(beyond.Length).IsEqualTo(22);
    }

    [Test]
    public async Task PathInsideAParenthesizedExpression_IsLocatedAtTheInnerExpression()
    {
        var diagnostic = Evaluate("(child::node())", XPathLimits.Default.WithMaxSequenceSize(4), out _);

        await Assert.That(diagnostic.Offset).IsEqualTo(1);
        await Assert.That(diagnostic.Length).IsEqualTo(13);
    }

    [Test]
    public async Task ASmallLimit_DoesNotChangeASmallResult()
    {
        var diagnostic = Evaluate("child::b[1]", XPathLimits.Default.WithMaxSequenceSize(1), out var result);

        await Assert.That(diagnostic.Code).IsEqualTo(string.Empty);
        await Assert.That(result.Count).IsEqualTo(1);
    }

    // The list itself, which every bounded collection site uses.
    [Test]
    public async Task List_ExactlyAtTheLimit_HoldsEveryItem_AndOneMoreRaisesTheDiagnostic()
    {
        var location = new SourceSpan(7, 3);
        var list = new XdmItemList(100, location);
        try
        {
            for (var i = 0; i < 100; i++)
            {
                list.Add(XdmItem.FromInteger(i));
            }

            await Assert.That(list.Count).IsEqualTo(100);

            var raised = ArithmeticFixture.Raised(() =>
            {
                list.Add(XdmItem.FromInteger(100));
                return default;
            });

            await Assert.That(raised).IsNotNull();
            await Assert.That(raised!.Diagnostic.QName).IsEqualTo(XPathLimitErrors.SequenceSizeExceeded);
            await Assert.That(raised.Diagnostic.Offset).IsEqualTo(7);
            await Assert.That(raised.Diagnostic.Length).IsEqualTo(3);
            await Assert.That(list.Count).IsEqualTo(100);
        }
        finally
        {
            list.Dispose();
        }
    }

    [Test]
    public async Task List_AddRangeBeyondTheLimit_RaisesBeforeAnythingIsCopied()
    {
        var list = new XdmItemList(3, new SourceSpan(0, 1));
        try
        {
            var items = new[]
            {
                XdmItem.FromInteger(1),
                XdmItem.FromInteger(2),
                XdmItem.FromInteger(3),
                XdmItem.FromInteger(4),
            };

            var raised = ArithmeticFixture.Raised(() =>
            {
                list.AddRange(items);
                return default;
            });

            await Assert.That(raised).IsNotNull();
            await Assert.That(list.Count).IsEqualTo(0);
        }
        finally
        {
            list.Dispose();
        }
    }

    [Test]
    public async Task List_PooledArrayLargerThanTheLimit_IsNotUsedBeyondTheLimit()
    {
        // The pool rents at least 16 items; the limit still stops the list at three.
        var list = new XdmItemList(3, new SourceSpan(0, 1));
        try
        {
            list.Add(XdmItem.FromInteger(1));
            list.Add(XdmItem.FromInteger(2));
            list.Add(XdmItem.FromInteger(3));

            var raised = ArithmeticFixture.Raised(() =>
            {
                list.Add(XdmItem.FromInteger(4));
                return default;
            });

            await Assert.That(raised).IsNotNull();
        }
        finally
        {
            list.Dispose();
        }
    }

    [Test]
    public async Task List_WithoutALimit_IsUnbounded()
    {
        var list = default(XdmItemList);
        try
        {
            for (var i = 0; i < 1_000; i++)
            {
                list.Add(XdmItem.FromInteger(i));
            }

            await Assert.That(list.Count).IsEqualTo(1_000);
        }
        finally
        {
            list.Dispose();
        }
    }

    [Test]
    public async Task List_AfterDispose_CanBeUsedAgainWithinTheLimit()
    {
        var list = new XdmItemList(2, new SourceSpan(0, 1));
        list.Add(XdmItem.FromInteger(1));
        list.Dispose();

        list.Add(XdmItem.FromInteger(2));
        list.Add(XdmItem.FromInteger(3));

        await Assert.That(list.Count).IsEqualTo(2);
        list.Dispose();
    }

    [Test]
    public async Task List_LimitBelowOne_Throws() =>
        await Assert.That(() => new XdmItemList(0, new SourceSpan(0, 1))).Throws<ArgumentOutOfRangeException>();
}
