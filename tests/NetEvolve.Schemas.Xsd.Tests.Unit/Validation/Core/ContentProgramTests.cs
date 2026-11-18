namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Elements;
using NetEvolve.Schemas.Xsd.Validation.Core;

public class ContentProgramTests
{
    [Test]
    public async Task Positions_MapToTheirTerms_SkippingParticlesThatCannotOccur()
    {
        var schema = new SchemaFixture();
        var a = SchemaFixture.Element("a", SchemaFixture.Simple("string"));
        var removed = SchemaFixture.Element("removed", SchemaFixture.Simple("string"));
        var wildcard = SchemaFixture.Wild("urn:w", XsdProcessContents.Lax);
        var b = SchemaFixture.Element("b", SchemaFixture.Simple("string"));
        var type = schema.ElementOnly(
            SchemaFixture.Seq(
                SchemaFixture.Part(a),
                SchemaFixture.Part(removed, 0, 0),
                SchemaFixture.Part(wildcard),
                SchemaFixture.Part(b)
            )
        );
        schema.Global("r", type);
        var model = schema.Model();

        var program = model.GetProgram(type);

        await Assert.That(program).IsNotNull();
        await Assert.That(program!.AllGroup).IsNull();
        await Assert.That(program.Terms.Count).IsEqualTo(3);
        var run = program.Automaton!.CreateRun();
        await Assert.That(model.TryGetNameId(new XPathQName(string.Empty, "a"), out var idA)).IsTrue();
        await Assert.That(model.TryGetNameId(new XPathQName(string.Empty, "b"), out var idB)).IsTrue();
        await Assert.That(model.TryGetNameId(new XPathQName(string.Empty, "removed"), out _)).IsFalse();
        await Assert.That(run.TryStep(idA, out var first)).IsTrue();
        await Assert.That(program.Terms[first.ParticleIndex]).IsSameReferenceAs(a);
        await Assert.That(run.TryStep(-1, new XPathQName("urn:w", "x"), out var second)).IsTrue();
        await Assert.That(program.Terms[second.ParticleIndex]).IsSameReferenceAs(wildcard);
        await Assert.That(run.TryStep(idB, out var third)).IsTrue();
        await Assert.That(program.Terms[third.ParticleIndex]).IsSameReferenceAs(b);
        await Assert.That(run.CanEnd()).IsTrue();
    }

    [Test]
    public async Task AllGroup_IsCompiledToTheBitsetMatcher()
    {
        var schema = new SchemaFixture();
        var type = schema.ElementOnly(
            SchemaFixture.All(
                SchemaFixture.Part("a", SchemaFixture.Simple("string"), 0, 1),
                SchemaFixture.Part("b", SchemaFixture.Simple("string"))
            )
        );
        schema.Global("r", type);
        var model = schema.Model();

        var program = model.GetProgram(type);

        await Assert.That(program).IsNotNull();
        await Assert.That(program!.Automaton).IsNull();
        await Assert.That(program.AllGroup!.MemberCount).IsEqualTo(2);
        await Assert.That(program.Terms.Count).IsEqualTo(2);
    }

    [Test]
    public async Task SimpleAndEmptyContent_HaveNoProgram()
    {
        var schema = new SchemaFixture();
        var empty = schema.Empty();
        var simple = schema.SimpleContent("string");
        schema.Global("e", empty);
        schema.Global("s", simple);
        var model = schema.Model();

        await Assert.That(model.GetProgram(empty)).IsNull();
        await Assert.That(model.GetProgram(simple)).IsNull();
    }

    [Test]
    public async Task AnonymousTypesBelowElements_GetAProgramToo()
    {
        var schema = new SchemaFixture();
        var inner = schema.ElementOnly(SchemaFixture.Seq(SchemaFixture.Part("leaf", SchemaFixture.Simple("string"))));
        var outer = schema.ElementOnly(SchemaFixture.Seq(SchemaFixture.Part("item", inner)));
        schema.Global("r", outer);
        var model = schema.Model();

        await Assert.That(model.GetProgram(outer)).IsNotNull();
        await Assert.That(model.GetProgram(inner)).IsNotNull();
    }

    [Test]
    public async Task AnyType_HasAProgramWithOneLaxWildcardPosition()
    {
        var schema = new SchemaFixture();
        schema.Global("r", null);
        var model = schema.Model();

        var program = model.GetProgram(ComplexTypeDefinition.AnyType);

        await Assert.That(program).IsNotNull();
        await Assert.That(program!.Terms.Count).IsEqualTo(1);
        await Assert.That(program.Terms[0]).IsAssignableTo<IWildcard>();
    }

    [Test]
    public async Task CircularGroupDefinition_TerminatesAndCompilesWhatIsLeft()
    {
        var schema = new SchemaFixture();
        var holder = Particle.CreateReference(
            SchemaFixture.Where,
            1,
            1,
            XsdSymbolSpace.Groups,
            new XPathQName(string.Empty, "g")
        );
        var group = ModelGroup.Create(
            ModelGroupCompositor.Sequence,
            SchemaFixture.Where,
            [SchemaFixture.Part("a", SchemaFixture.Simple("string")), holder]
        );
        var definition = ModelGroupDefinition.Create(new XPathQName(string.Empty, "g"), SchemaFixture.Where, group);
        holder.Bind(0, definition);
        var type = schema.ElementOnly(SchemaFixture.Part(definition));
        schema.Global("r", type);

        var model = schema.Model();

        var program = model.GetProgram(type);
        await Assert.That(program).IsNotNull();
        await Assert.That(program!.Terms.Count).IsEqualTo(1);
    }

    [Test]
    public async Task ModelOverNothing_HasNoNamesAndNoPrograms()
    {
        var model = new SchemaFixture().Model();

        await Assert.That(model.TryGetNameId(new XPathQName(string.Empty, "a"), out _)).IsFalse();
        await Assert.That(model.GetProgram(ComplexTypeDefinition.AnyType)).IsNull();
    }
}
