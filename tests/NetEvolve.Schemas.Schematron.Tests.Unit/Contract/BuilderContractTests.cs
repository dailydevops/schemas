namespace NetEvolve.Schemas.Schematron.Tests.Unit.Contract;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using NetEvolve.Schemas.Resources;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Testing.Fakes;

public class BuilderContractTests
{
    private static SchematronValidatorBuilder NewBuilder(FakePipeline pipeline) =>
        SchemaValidator.ForSchematron("<schema/>".AsSpan()).UsePipeline(pipeline);

    [Test]
    public async Task Builder_PhaseParametersResolverLimits_RoundTripToValidator()
    {
        var pipeline = new FakePipeline();
        var resolver = new InMemoryResourceResolver();
        var limits = ValidationLimits.Default.WithMaxIssues(7);

        var validator = NewBuilder(pipeline)
            .WithPhase("basic")
            .WithParameter("country", "DE")
            .WithLimits(limits)
            .WithResolver(resolver)
            .BuildNow();

        await Assert.That(validator.Phase).IsEqualTo("basic");
        await Assert.That(validator.Parameters["country"]).IsEqualTo("DE");
        await Assert.That(validator.Parameters.Count).IsEqualTo(1);
        await Assert.That(validator.Limits).IsSameReferenceAs(limits);
        await Assert.That(validator.Resolver).IsSameReferenceAs(resolver);
        await Assert.That(pipeline.LastSettings!.Phase).IsEqualTo("basic");
        await Assert.That(pipeline.LastSettings!.Parameters["country"]).IsEqualTo("DE");
        await Assert.That(pipeline.LastSettings!.Limits).IsSameReferenceAs(limits);
        await Assert.That(pipeline.LastSettings!.Resolver).IsSameReferenceAs(resolver);
    }

    [Test]
    public async Task Builder_Defaults_AreHashDefaultPhaseNoParametersAndCoreDefaults()
    {
        var builder = NewBuilder(new FakePipeline());
        var validator = builder.BuildNow();

        await Assert.That(builder.Phase).IsEqualTo("#DEFAULT");
        await Assert.That(builder.Parameters.Count).IsEqualTo(0);
        await Assert.That(validator.Phase).IsEqualTo("#DEFAULT");
        await Assert.That(validator.Limits).IsSameReferenceAs(ValidationLimits.Default);
        await Assert.That(validator.Resolver).IsSameReferenceAs(DenyAllResourceResolver.Instance);
    }

    [Test]
    public async Task Builder_WithParameter_LastCallWins()
    {
        var validator = NewBuilder(new FakePipeline())
            .WithParameter("country", "DE")
            .WithParameter("country", "FR")
            .BuildNow();

        await Assert.That(validator.Parameters["country"]).IsEqualTo("FR");
    }

    [Test]
    public async Task Builder_GlobalPatternLets_IsOffByDefault_AndTheLastCallWins()
    {
        var byDefault = new FakePipeline();
        var on = new FakePipeline();
        var last = new FakePipeline();

        _ = NewBuilder(byDefault).BuildNow();
        _ = NewBuilder(on).WithGlobalPatternLets(true).BuildNow();
        _ = NewBuilder(last).WithGlobalPatternLets(true).WithGlobalPatternLets(false).BuildNow();

        await Assert.That(byDefault.LastSettings!.GlobalPatternLets).IsFalse();
        await Assert.That(on.LastSettings!.GlobalPatternLets).IsTrue();
        await Assert.That(last.LastSettings!.GlobalPatternLets).IsFalse();
    }

    [Test]
    public async Task Builder_Parameters_AreACopyWithOrdinalKeys()
    {
        var builder = NewBuilder(new FakePipeline()).WithParameter("country", "DE");

        var snapshot = builder.Parameters;
        builder.WithParameter("language", "de");

        await Assert.That(snapshot.Count).IsEqualTo(1);
        await Assert.That(builder.Parameters.Count).IsEqualTo(2);
        await Assert.That(builder.Parameters.ContainsKey("COUNTRY")).IsFalse();
    }

    [Test]
    public async Task Builder_NullArguments_Throw()
    {
        var builder = NewBuilder(new FakePipeline());

        await Assert.That(() => builder.WithPhase(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => builder.WithParameter(null!, "x")).Throws<ArgumentNullException>();
        await Assert.That(() => builder.WithParameter("name", null!)).Throws<ArgumentNullException>();
        await Assert.That(() => builder.WithLimits(null!)).Throws<ArgumentNullException>();
        await Assert.That(() => builder.WithResolver(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task WithPhase_Empty_ThrowsArgumentException()
    {
        var builder = NewBuilder(new FakePipeline());

        await Assert.That(() => builder.WithPhase(string.Empty)).Throws<ArgumentException>();
    }

    [Test]
    public async Task WithParameter_EmptyName_ThrowsArgumentException()
    {
        var builder = NewBuilder(new FakePipeline());

        await Assert.That(() => builder.WithParameter(string.Empty, "x")).Throws<ArgumentException>();
    }

    [Test]
    public async Task Build_TwoCalls_ProduceIndependentValidators()
    {
        var pipeline = new FakePipeline();
        var builder = NewBuilder(pipeline).WithParameter("country", "DE");

        var first = builder.BuildNow();
        builder.WithParameter("country", "FR").WithPhase("other");
        var second = builder.BuildNow();

        await Assert.That(second).IsNotSameReferenceAs(first);
        await Assert.That(first.Parameters["country"]).IsEqualTo("DE");
        await Assert.That(first.Phase).IsEqualTo("#DEFAULT");
        await Assert.That(second.Parameters["country"]).IsEqualTo("FR");
        await Assert.That(second.Phase).IsEqualTo("other");
        await Assert.That(pipeline.LoadCount).IsEqualTo(2);
    }

    [Test]
    public async Task Validator_ReadBack_ComesFromTheCompiledSchema()
    {
        var validator = NewBuilder(new FakePipeline()).BuildNow();

        await Assert.That(validator.Schema.Id).IsEqualTo("fake");
        await Assert.That(validator.Edition).IsEqualTo(SchematronEdition.Iso2016);
        await Assert.That(validator.EffectivePhase).IsEqualTo("#ALL");
    }

    [Test]
    public async Task BuildAsync_ReachesTheSameLoader(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var pipeline = new FakePipeline();

        var validator = await NewBuilder(pipeline).WithPhase("basic").BuildAsync(cancellationToken);

        await Assert.That(pipeline.LoadCount).IsEqualTo(1);
        await Assert.That(validator.Phase).IsEqualTo("basic");
    }

    [Test]
    public async Task Validator_Interface_HasAdditiveMembersOnly()
    {
        var members = typeof(ISchematronValidator)
            .GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(member => member is PropertyInfo or MethodInfo { IsSpecialName: false })
            .Select(member => member.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        await Assert
            .That(members)
            .IsEquivalentTo([
                "Edition",
                "EffectivePhase",
                "Limits",
                "Parameters",
                "Phase",
                "Resolver",
                "Schema",
                "WithReportSink",
            ]);
        await Assert.That(typeof(ISchematronValidator).GetInterfaces()).Contains(typeof(ISchemaValidator));
    }

    [Test]
    public async Task WithReportSink_View_ReadBackEqualsParent()
    {
        var resolver = new InMemoryResourceResolver();
        var parent = NewBuilder(new FakePipeline())
            .WithPhase("basic")
            .WithParameter("country", "DE")
            .WithResolver(resolver)
            .BuildNow();

        var view = parent.WithReportSink(new FakeReportSink());

        await Assert.That(view).IsNotSameReferenceAs(parent);
        await Assert.That(view.Schema).IsSameReferenceAs(parent.Schema);
        await Assert.That(view.Edition).IsEqualTo(parent.Edition);
        await Assert.That(view.Phase).IsEqualTo(parent.Phase);
        await Assert.That(view.EffectivePhase).IsEqualTo(parent.EffectivePhase);
        await Assert.That(view.Parameters).IsSameReferenceAs(parent.Parameters);
        await Assert.That(view.Limits).IsSameReferenceAs(parent.Limits);
        await Assert.That(view.Resolver).IsSameReferenceAs(parent.Resolver);
    }

    [Test]
    public async Task WithReportSink_ReturnsNewInstance_SharesCompiledSchema_AndCarriesTheSink()
    {
        var sink = new FakeReportSink();
        var parent = (SchematronValidator)NewBuilder(new FakePipeline()).BuildNow();

        var view = (SchematronValidator)parent.WithReportSink(sink);

        await Assert.That(view.Compiled).IsSameReferenceAs(parent.Compiled);
        await Assert.That(view.ReportSink).IsSameReferenceAs(sink);
        await Assert.That(parent.ReportSink).IsNull();
    }

    [Test]
    public async Task WithReportSink_Null_Throws()
    {
        var parent = NewBuilder(new FakePipeline()).BuildNow();

        await Assert.That(() => parent.WithReportSink(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Validate_UntilEvaluationLands_FailsWithNotImplementedDiagnostic()
    {
        var validator = NewBuilder(new FakePipeline()).BuildNow();

        var exception = EntryPointContractTests.Capture(() => validator.Validate("<a/>".AsSpan()));

        await Assert.That(exception.Issues[0].Code).IsEqualTo(SchematronDiagnosticCodes.NotImplemented);
    }

    [Test]
    public async Task Validator_Constructor_RejectsNullArguments()
    {
        var compiled = FakeModel.Compiled();
        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);

        await Assert
            .That(() =>
                new SchematronValidator(
                    null!,
                    "p",
                    parameters,
                    ValidationLimits.Default,
                    DenyAllResourceResolver.Instance
                )
            )
            .Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                new SchematronValidator(
                    compiled,
                    null!,
                    parameters,
                    ValidationLimits.Default,
                    DenyAllResourceResolver.Instance
                )
            )
            .Throws<ArgumentNullException>();
        await Assert
            .That(() =>
                new SchematronValidator(
                    compiled,
                    "p",
                    null!,
                    ValidationLimits.Default,
                    DenyAllResourceResolver.Instance
                )
            )
            .Throws<ArgumentNullException>();
    }
}
