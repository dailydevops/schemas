namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Ordering;

using System;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Ordering;
using NetEvolve.Schemas.XPath.Syntax.Tree;

public class OrderAnalysisTests
{
    // Hand-written expectations, derived from the shape of each step sequence evaluated from one context node.
    [Test]
    [Arguments("Child", XPathOrderRequirement.None)]
    [Arguments("Attribute", XPathOrderRequirement.None)]
    [Arguments("Self", XPathOrderRequirement.None)]
    [Arguments("Parent", XPathOrderRequirement.None)]
    [Arguments("Descendant", XPathOrderRequirement.None)]
    [Arguments("DescendantOrSelf", XPathOrderRequirement.None)]
    [Arguments("FollowingSibling", XPathOrderRequirement.None)]
    [Arguments("Following", XPathOrderRequirement.None)]
    [Arguments("Ancestor", XPathOrderRequirement.Reverse)]
    [Arguments("AncestorOrSelf", XPathOrderRequirement.Reverse)]
    [Arguments("PrecedingSibling", XPathOrderRequirement.Reverse)]
    [Arguments("Preceding", XPathOrderRequirement.Reverse)]
    [Arguments("Child,Child", XPathOrderRequirement.None)]
    [Arguments("Child,Child,Child", XPathOrderRequirement.None)]
    [Arguments("Child,Attribute", XPathOrderRequirement.None)]
    [Arguments("Child,Descendant", XPathOrderRequirement.None)]
    [Arguments("Descendant,Attribute", XPathOrderRequirement.None)]
    [Arguments("Descendant,Self", XPathOrderRequirement.None)]
    [Arguments("Descendant,Child", XPathOrderRequirement.Sort)]
    [Arguments("DescendantOrSelf,Child", XPathOrderRequirement.Sort)]
    [Arguments("Descendant,Descendant", XPathOrderRequirement.Sort)]
    [Arguments("Ancestor,Child", XPathOrderRequirement.Sort)]
    [Arguments("Descendant,FollowingSibling", XPathOrderRequirement.Sort)]
    [Arguments("Descendant,Parent", XPathOrderRequirement.Sort)]
    [Arguments("Child,Parent", XPathOrderRequirement.Sort)]
    [Arguments("Child,FollowingSibling", XPathOrderRequirement.Sort)]
    [Arguments("Child,Ancestor", XPathOrderRequirement.Sort)]
    [Arguments("Descendant,Following", XPathOrderRequirement.Sort)]
    [Arguments("Descendant,Preceding", XPathOrderRequirement.Sort)]
    [Arguments("Parent,Child", XPathOrderRequirement.None)]
    [Arguments("Parent,Parent", XPathOrderRequirement.None)]
    [Arguments("Parent,Ancestor", XPathOrderRequirement.Reverse)]
    [Arguments("Self,Descendant", XPathOrderRequirement.None)]
    [Arguments("Child,Self,Child", XPathOrderRequirement.None)]
    [Arguments("Ancestor,Self", XPathOrderRequirement.Reverse)]
    public async Task Compose_FromOneContextNode_YieldsTheExpectedRequirement(
        string axes,
        XPathOrderRequirement expected
    )
    {
        var sequence = ParseAxes(axes);

        var properties = XPathOrderAnalysis.Compose(XPathOrderAnalysis.SingleNode, sequence);

        await Assert.That(XPathOrderAnalysis.GetRequirement(properties)).IsEqualTo(expected);
    }

    [Test]
    public async Task Compose_DescendantThenChild_IsDuplicateFreeButNotOrdered()
    {
        var properties = XPathOrderAnalysis.Compose(XPathOrderAnalysis.SingleNode, [Axis.Descendant, Axis.Child]);

        await Assert.That(properties.HasFlag(XPathOrderProperties.DuplicateFree)).IsTrue();
        await Assert.That(properties.HasFlag(XPathOrderProperties.Ordered)).IsFalse();
        await Assert.That(XPathOrderAnalysis.GetRequirement(properties)).IsEqualTo(XPathOrderRequirement.Sort);
    }

    [Test]
    public async Task GetRequirement_OrderedWithDuplicates_NeedsOnlyDeduplication()
    {
        var requirement = XPathOrderAnalysis.GetRequirement(XPathOrderProperties.Ordered);

        await Assert.That(requirement).IsEqualTo(XPathOrderRequirement.Deduplicate);
    }

    [Test]
    public async Task GetRequirement_UnknownInput_NeedsSort()
    {
        var requirement = XPathOrderAnalysis.GetRequirement(XPathOrderAnalysis.Unknown);

        await Assert.That(requirement).IsEqualTo(XPathOrderRequirement.Sort);
    }

    [Test]
    public async Task Step_UnknownInputThroughSelf_StaysUnknown()
    {
        var properties = XPathOrderAnalysis.Step(XPathOrderAnalysis.Unknown, Axis.Self);

        await Assert.That(XPathOrderAnalysis.GetRequirement(properties)).IsEqualTo(XPathOrderRequirement.Sort);
    }

    [Test]
    public async Task Step_OrderedDuplicateFreeInputThroughChild_IsOrderedOnlyWhenNonNested()
    {
        var nested = XPathOrderProperties.Ordered | XPathOrderProperties.DuplicateFree;
        var flat = nested | XPathOrderProperties.NonNested;

        await Assert
            .That(XPathOrderAnalysis.GetRequirement(XPathOrderAnalysis.Step(nested, Axis.Child)))
            .IsEqualTo(XPathOrderRequirement.Sort);
        await Assert
            .That(XPathOrderAnalysis.GetRequirement(XPathOrderAnalysis.Step(flat, Axis.Child)))
            .IsEqualTo(XPathOrderRequirement.None);
    }

    [Test]
    public async Task Step_NamespaceAxis_RaisesXpst0010() =>
        await Assert
            .That(() => XPathOrderAnalysis.Step(XPathOrderAnalysis.SingleNode, Axis.Namespace))
            .Throws<XPathException>();

    private static Axis[] ParseAxes(string axes)
    {
        var parts = axes.Split(',');
        var result = new Axis[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            result[i] = Enum.Parse<Axis>(parts[i]);
        }

        return result;
    }
}
