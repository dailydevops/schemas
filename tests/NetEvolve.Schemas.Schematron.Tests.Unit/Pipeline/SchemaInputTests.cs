namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Model;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// One schema through every input kind gives one compiled schema (SCH-55, #311): the same model, the same edition and phase, and the
/// same results for an instance. The byte sequences are cut inside the two bytes of a character.
/// </summary>
public class SchemaInputTests
{
    [Test]
    public async Task EveryInputKind_GivesTheSameCompiledSchema()
    {
        var kinds = BuildEveryKind(Schema);
        var expected = Describe(kinds[0].Validator);
        var expectedResult = Describe(Validate(kinds[0].Validator, Instance));

        await Assert.That(kinds.Count).IsEqualTo(8);
        foreach (var (kind, validator) in kinds)
        {
            await Assert.That(Describe(validator)).IsEqualTo(expected).Because(kind);
            await Assert.That(Describe(Validate(validator, Instance))).IsEqualTo(expectedResult).Because(kind);
        }
    }

    [Test]
    public async Task EveryInputKind_ReadsTheModelOfTheSchema()
    {
        foreach (var (kind, validator) in BuildEveryKind(Schema))
        {
            await Assert.That(validator.Schema.Patterns.Count).IsEqualTo(1).Because(kind);
            await Assert.That(validator.Schema.Patterns[0].Id).IsEqualTo("p1").Because(kind);
            await Assert.That(validator.Schema.Patterns[0].Rules[0].Items.Count).IsEqualTo(2).Because(kind);
            await Assert.That(validator.Edition).IsEqualTo(SchematronEdition.Iso2016).Because(kind);
            await Assert.That(validator.EffectivePhase).IsEqualTo("#ALL").Because(kind);
        }
    }

    [Test]
    public async Task SystemId_IsTheSourceOfTheSchemaLocations()
    {
        var failure = BuildFailure(TwoDefects, "file:///elsewhere.sch");

        foreach (var issue in failure.Issues)
        {
            await Assert.That(issue.Location.Source).IsEqualTo("file:///elsewhere.sch");
        }
    }
}
