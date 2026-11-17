namespace NetEvolve.Schemas.XPath.Tests.Unit.Syntax.Tree;

using NetEvolve.Schemas.XPath.Syntax.Tree;

public class LocationTests
{
    [Test]
    public async Task EveryNode_ReportsTheLocationItWasBuiltWith()
    {
        var factory = new TreeFactory();
        var root = factory.AllKinds();

        foreach (var node in TreeWalker.Descendants(root))
        {
            await Assert.That(node.Location).IsEqualTo(factory.Built[node]);
        }
    }

    [Test]
    public async Task EveryKind_ReportsItsLocation()
    {
        var factory = new TreeFactory();
        var root = factory.AllKinds();
        var seen = new System.Collections.Generic.HashSet<System.Type>();

        foreach (var node in TreeWalker.Descendants(root))
        {
            seen.Add(node.GetType());
            await Assert.That(node.Location.Length).IsEqualTo(3);
        }

        await Assert.That(seen.Count).IsEqualTo(VisitorTests.NodeKinds().Count);
    }

    [Test]
    public async Task SourceSpan_HasValueEquality_AndExposesOffsetLengthAndEnd()
    {
        var span = new SourceSpan(4, 6);

        await Assert.That(span).IsEqualTo(new SourceSpan(4, 6));
        await Assert.That(span == new SourceSpan(4, 7)).IsFalse();
        await Assert.That(span.Offset).IsEqualTo(4);
        await Assert.That(span.Length).IsEqualTo(6);
        await Assert.That(span.End).IsEqualTo(10);
    }

    [Test]
    public async Task SyntaxTreeNamespace_ReferencesNoEvaluationTypes()
    {
        var offenders = new System.Collections.Generic.List<string>();
        foreach (var type in typeof(SyntaxNode).Assembly.GetTypes())
        {
            if (type.Namespace != typeof(SyntaxNode).Namespace)
            {
                continue;
            }

            foreach (var property in type.GetProperties())
            {
                var ns = property.PropertyType.Namespace ?? string.Empty;
                if (ns != type.Namespace && !ns.StartsWith("System", System.StringComparison.Ordinal))
                {
                    offenders.Add(type.Name + "." + property.Name);
                }
            }
        }

        await Assert.That(offenders).IsEmpty();
    }
}
