namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

using System;
using System.Reflection;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Validation.Core;

public class GuardTests
{
    private static XsdIssueReporter Reporter => new(new ValidationIssueCollector(ValidationLimits.Default));

    private static ValidationModel Model => new SchemaFixture().Model();

    private static XsdSchemaSet EmptySet => new ComponentRegistry(Reporter).Freeze(XsdVersion.V11);

    [Test]
    public async Task Validator_NullModel_Throws() =>
        await Assert
            .That(() =>
                new StreamingValidator(null!, new ScriptedAssessor(), Reporter, ValidationLimits.Default, null, [])
            )
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Validator_NullAssessor_Throws() =>
        await Assert
            .That(() => new StreamingValidator(Model, null!, Reporter, ValidationLimits.Default, null, []))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Validator_NullReporter_Throws() =>
        await Assert
            .That(() =>
                new StreamingValidator(Model, new ScriptedAssessor(), null!, ValidationLimits.Default, null, [])
            )
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Validator_NullLimits_Throws() =>
        await Assert
            .That(() => new StreamingValidator(Model, new ScriptedAssessor(), Reporter, null!, null, []))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Validator_NullListeners_Throws() =>
        await Assert
            .That(() =>
                new StreamingValidator(Model, new ScriptedAssessor(), Reporter, ValidationLimits.Default, null, null!)
            )
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Validator_NullListenerEntry_Throws() =>
        await Assert
            .That(() =>
                new StreamingValidator(
                    Model,
                    new ScriptedAssessor(),
                    Reporter,
                    ValidationLimits.Default,
                    null,
                    new ValidationListener[] { null! }
                )
            )
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Model_NullSet_Throws() =>
        await Assert.That(() => ValidationModel.Build(null!, [], Reporter)).Throws<ArgumentNullException>();

    [Test]
    public async Task Model_NullComponents_Throws() =>
        await Assert.That(() => ValidationModel.Build(EmptySet, null!, Reporter)).Throws<ArgumentNullException>();

    [Test]
    public async Task Model_NullReporter_Throws() =>
        await Assert.That(() => ValidationModel.Build(EmptySet, [], null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task Model_NullComponentEntry_Throws() =>
        await Assert
            .That(() => ValidationModel.Build(EmptySet, new IXsdComponent[] { null! }, Reporter))
            .Throws<ArgumentNullException>();

    [Test]
    public async Task Model_NullType_Throws() =>
        await Assert.That(() => Model.GetProgram(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task Binding_NullModel_Throws() =>
        await Assert.That(() => new NameBinding(null!, new SpanInterningNameTable(10))).Throws<ArgumentNullException>();

    [Test]
    public async Task Binding_NullNames_Throws() =>
        await Assert.That(() => new NameBinding(Model, null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task ModelBuilder_NullReporter_Throws()
    {
        var builder = typeof(ValidationModel).GetNestedType("Builder", BindingFlags.NonPublic)!;
        var constructor = builder.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];

        Exception? inner = null;
        try
        {
            _ = constructor.Invoke([null]);
        }
        catch (TargetInvocationException ex)
        {
            inner = ex.InnerException;
        }

        await Assert.That(inner).IsTypeOf<ArgumentNullException>();
    }

    [Test]
    public async Task Program_NullTerms_Throws() =>
        await Assert.That(() => new ContentProgram(null, null, null!, 0)).Throws<ArgumentNullException>();

    [Test]
    public async Task Program_NegativePoolIndex_Throws() =>
        await Assert.That(() => new ContentProgram(null, null, [], -1)).Throws<ArgumentOutOfRangeException>();

    [Test]
    [Arguments(-1)]
    [Arguments(2)]
    public async Task Assessor_UndefinedVersion_Throws(int version) =>
        await Assert.That(() => new DescriptorValueAssessor((XsdVersion)version)).Throws<ArgumentOutOfRangeException>();

    [Test]
    public async Task Listener_RunContext_ExposesTheLimitsAndTheReporter()
    {
        var listener = new ContextListener();

        _ = ValidationHarness.Run(new SchemaFixture(), "<r/>", null, listener);

        await Assert.That(listener.Limits).IsSameReferenceAs(ValidationLimits.Default);
        await Assert.That(listener.HasReporter).IsTrue();
    }

    private sealed class ContextListener : ValidationListener
    {
        public ValidationLimits? Limits { get; private set; }

        public bool HasReporter { get; private set; }

        public override ListenerInterest Interest => ListenerInterest.None;

        public override void BeginRun(in ValidationRunContext context)
        {
            Limits = context.Limits;
            HasReporter = context.Reporter is not null;
        }
    }
}
