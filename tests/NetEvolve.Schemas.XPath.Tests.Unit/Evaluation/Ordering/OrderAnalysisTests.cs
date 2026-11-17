namespace NetEvolve.Schemas.XPath.Tests.Unit.Evaluation.Ordering;

using System;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Ordering;
using NetEvolve.Schemas.XPath.Syntax.Tree;

public class OrderAnalysisTests
{
    // Hand-written expectations, derived from the shape of each step sequence evaluated from one context node.
    [Test]
    [Arguments("Child", "None")]
    [Arguments("Attribute", "None")]
    [Arguments("Self", "None")]
    [Arguments("Parent", "None")]
    [Arguments("Descendant", "None")]
    [Arguments("DescendantOrSelf", "None")]
    [Arguments("FollowingSibling", "None")]
    [Arguments("Following", "None")]
    [Arguments("Ancestor", "Reverse")]
    [Arguments("AncestorOrSelf", "Reverse")]
    [Arguments("PrecedingSibling", "Reverse")]
    [Arguments("Preceding", "Reverse")]
    [Arguments("Child,Child", "None")]
    [Arguments("Child,Child,Child", "None")]
    [Arguments("Child,Attribute", "None")]
    [Arguments("Child,Descendant", "None")]
    [Arguments("Descendant,Attribute", "None")]
    [Arguments("Descendant,Self", "None")]
    [Arguments("Descendant,Child", "Sort")]
    [Arguments("DescendantOrSelf,Child", "Sort")]
    [Arguments("Descendant,Descendant", "Sort")]
    [Arguments("Ancestor,Child", "Sort")]
    [Arguments("Descendant,FollowingSibling", "Sort")]
    [Arguments("Descendant,Parent", "Sort")]
    [Arguments("Child,Parent", "Sort")]
    [Arguments("Child,FollowingSibling", "Sort")]
    [Arguments("Child,Ancestor", "Sort")]
    [Arguments("Descendant,Following", "Sort")]
    [Arguments("Descendant,Preceding", "Sort")]
    [Arguments("Parent,Child", "None")]
    [Arguments("Parent,Parent", "None")]
    [Arguments("Parent,Ancestor", "Reverse")]
    [Arguments("Self,Descendant", "None")]
    [Arguments("Child,Self,Child", "None")]
    [Arguments("Ancestor,Self", "Reverse")]
    public async Task Compose_FromOneContextNode_YieldsTheExpectedRequirement(string axes, string expected)
    {
        ArgumentNullException.ThrowIfNull(axes);
        var sequence = ParseAxes(axes);

        var properties = XPathOrderAnalysis.Compose(XPathOrderAnalysis.SingleNode, sequence);

        await Assert
            .That(XPathOrderAnalysis.GetRequirement(properties))
            .IsEqualTo(Enum.Parse<XPathOrderRequirement>(expected));
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
