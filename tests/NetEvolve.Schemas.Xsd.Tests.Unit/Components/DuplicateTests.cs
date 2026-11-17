namespace NetEvolve.Schemas.Xsd.Tests.Unit.Components;

using NetEvolve.Schemas.Xsd.Contracts;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Components.Fixture;

public class DuplicateTests
{
    [Test]
    public async Task Duplicate_ReportsRuleCodeAndBothLocations_AndKeepsTheFirst()
    {
        var registry = NewRegistry(out var collector);
        var first = new RefComponent(XsdComponentKind.ElementDeclaration, "e", 3);
        var second = new RefComponent(XsdComponentKind.ElementDeclaration, "e", 8);
        registry.Add(first);
        registry.Add(second);

        var issues = collector.ToResult().Issues;

        await Assert.That(issues.Count).IsEqualTo(1);
        await Assert.That(issues[0].Code).IsEqualTo(CodeOf("sch-props-correct"));
        await Assert.That(issues[0].Constraint).IsEqualTo("sch-props-correct.2");
        await Assert.That(issues[0].Location).IsEqualTo(second.Location);
        await Assert.That(issues[0].Message).Contains(first.Location.ToString());
        await Assert.That(registry.Count).IsEqualTo(1);
        await Assert.That(registry.TryGet(XsdComponentKind.ElementDeclaration, Name("e"), out var kept)).IsTrue();
        await Assert.That(kept).IsSameReferenceAs(first);
    }
}
