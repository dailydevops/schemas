namespace NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Pipeline;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Pipeline.PipelineFixture;

/// <summary>
/// The compiled schema is built once and reused (SCH-71, #340): every expression is compiled exactly once at <c>Build</c>, the names of the
/// schema live in the name table of the pooled per-validation context, and concurrent validations give the results of sequential ones.
/// </summary>
public class CompileOnceTests
{
    private static CompiledProgram Program(ISchematronValidator validator) =>
        (CompiledProgram)((SchematronValidator)validator).Compiled;

    [Test]
    public async Task Validate_CompilesEveryExpressionExactlyOnceAcrossManyValidations()
    {
        var program = Program(Build(Schema));
        var atBuild = program.CompileCount;
        var texts = new HashSet<string>(program.CompiledTexts, StringComparer.Ordinal);
        var validator = Build(Schema);
        var again = Program(validator);

        for (var i = 0; i < 100; i++)
        {
            _ = Validate(validator, Instance);
        }

        await Assert.That(atBuild).IsEqualTo(5);
        await Assert.That(texts.Count).IsEqualTo(atBuild);
        await Assert.That(again.CompileCount).IsEqualTo(atBuild);
        await Assert.That(again.CompiledTexts.Count).IsEqualTo(atBuild);
    }

    [Test]
    public async Task Names_OfTheSchemaAreTheSameInstancesInTheTableOfTheContext()
    {
        var validator = Build(Schema);
        var program = Program(validator);
        var context = program.Contexts.Rent();
        var seeded = context.Names.Count;
        var options = XmlIssues.Options(ValidationLimits.Default).WithNameTable(context.Names);
        var tree = XmlDocumentLoader.Load(Utf8(Instance), options, null);

        await Assert.That(program.SchemaNames).Contains("invoice");
        await Assert.That(program.SchemaNames).Contains("urn:invoice");
        await Assert.That(ReferenceEquals(tree.Names, context.Names)).IsTrue();
        var root = tree.Document.FirstChild;
        var local = tree.Names.GetString(root.Name.Local);
        await Assert.That(ReferenceEquals(local, context.Names.Lookup("invoice".AsSpan()))).IsTrue();
        await Assert.That(ReferenceEquals(tree.Names.GetString(root.Name.NamespaceUri), context.Names.Lookup("urn:invoice".AsSpan()))).IsTrue();
        await Assert.That(context.Names.Count).IsEqualTo(seeded);
        program.Contexts.Return(context);

        _ = Validate(validator, Instance);

        var reused = program.Contexts.Rent();
        await Assert.That(ReferenceEquals(reused, context)).IsTrue();
        await Assert.That(reused.Names.Count).IsEqualTo(seeded);
    }

    [Test]
    public async Task Contexts_AreReusedAndDroppedWhenTheirTableGrewTooMuch()
    {
        var program = Program(Build(Schema));
        var first = program.Contexts.Rent();
        program.Contexts.Return(first);

        var second = program.Contexts.Rent();
        await Assert.That(ReferenceEquals(first, second)).IsTrue();

        for (var i = 0; i < ValidationContextPool.MaxGrowth + 1; i++)
        {
            _ = second.Names.Intern("n" + i);
        }

        program.Contexts.Return(second);
        var third = program.Contexts.Rent();

        await Assert.That(ReferenceEquals(second, third)).IsFalse();
        await Assert.That(third.Names.Count).IsEqualTo(program.SchemaNames.Count + 1);
    }

    [Test]
    public async Task Validate_OnSixteenThreads_GivesTheResultsOfSequentialRuns()
    {
        var validator = Build(Schema);
        var documents = new string[16];
        for (var i = 0; i < documents.Length; i++)
        {
            documents[i] =
                i % 2 == 0
                    ? "<invoice xmlns=\"urn:invoice\"><number>" + i + "</number><total>" + (i * 300) + "</total><buyer>b" + i + "</buyer></invoice>"
                    : "<invoice xmlns=\"urn:invoice\"><extra" + i + "/><total>" + (i * 300) + "</total></invoice>";
        }

        var expected = new string[documents.Length];
        for (var i = 0; i < documents.Length; i++)
        {
            expected[i] = Describe(Validate(validator, documents[i]));
        }

        var actual = new string[documents.Length];
        using var barrier = new Barrier(documents.Length);
        var tasks = new Task[documents.Length];
        for (var i = 0; i < tasks.Length; i++)
        {
            var index = i;
            tasks[i] = Task.Factory.StartNew(
                () =>
                {
                    _ = barrier.SignalAndWait(TimeSpan.FromSeconds(30));
                    for (var round = 0; round < 20; round++)
                    {
                        actual[index] = Describe(Validate(validator, documents[index]));
                        if (actual[index] != expected[index])
                        {
                            return;
                        }
                    }
                },
                TaskCreationOptions.LongRunning
            );
        }

        await Task.WhenAll(tasks);

        for (var i = 0; i < documents.Length; i++)
        {
            await Assert.That(actual[i]).IsEqualTo(expected[i]);
        }
    }
}
