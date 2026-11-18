namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Registry;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;

public class RegistryDiscoveryTests
{
    private const string Prefix = "NetEvolve.Schemas.Schematron.Xslt.";

    [Test]
    public async Task Registry_DuplicateExpandedName_FailsTest()
    {
        var builder = new XsltRegistryBuilder();
        builder.Add(new FakeXsltInstruction(RegistryTree.Name("fake")));

        await Assert
            .That(() => builder.Add(new FakeXsltInstruction(RegistryTree.Name("fake"))))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Registry_DuplicateSyntheticEntry_Fails()
    {
        var builder = new XsltRegistryBuilder();
        builder.AddSynthetic(XsltSyntheticElement.TextNode, new FakeXsltInstruction(default));

        await Assert
            .That(() => builder.AddSynthetic(XsltSyntheticElement.TextNode, new FakeXsltInstruction(default)))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Registry_NameOutsideXsltNamespace_IsRejected()
    {
        var builder = new XsltRegistryBuilder();
        var name = new XPathQName("urn:other", "fake");

        await Assert.That(() => builder.Add(new FakeXsltInstruction(name))).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Registry_Builder_RejectsNullAndUnknownKinds()
    {
        var builder = new XsltRegistryBuilder();
        var catalog = new XsltRegistryCatalog();

        await Assert.That(() => builder.Add(null!)).Throws<ArgumentNullException>();
        await Assert
            .That(() => builder.AddSynthetic(XsltSyntheticElement.TextNode, null!))
            .Throws<ArgumentNullException>();
        await Assert
            .That(() => builder.AddSynthetic((XsltSyntheticElement)9, new FakeXsltInstruction(default)))
            .Throws<ArgumentOutOfRangeException>();
        await Assert.That(() => catalog.Stage(null!)).Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Registry_Staging_BuildsNothingUntilTheDefaultIsRequested()
    {
        var catalog = new XsltRegistryCatalog();
        var calls = 0;

        catalog.Stage(_ => calls++);
        catalog.Stage(_ => calls++);

        await Assert.That(calls).IsEqualTo(0);
        await Assert.That(catalog.IsDefaultCreated).IsFalse();

        _ = catalog.Default;

        await Assert.That(calls).IsEqualTo(2);
        await Assert.That(catalog.IsDefaultCreated).IsTrue();
    }

    [Test]
    public async Task Registry_DefaultInstance_ClosedAfterFirstBuild()
    {
        var catalog = new XsltRegistryCatalog();
        catalog.Stage(static b => b.Add(new FakeXsltInstruction(RegistryTree.Name("early"))));
        var first = catalog.Default;

        await Assert.That(() => catalog.Stage(static _ => { })).Throws<InvalidOperationException>();

        var early = RegistryTree.Name("early");
        await Assert.That(first.TryGet(in early, out _)).IsTrue();
        await Assert.That(catalog.Default).IsSameReferenceAs(first);
    }

    [Test]
    public async Task Registry_ConcurrentFirstBuild_IsSafe()
    {
        var catalog = new XsltRegistryCatalog();
        var calls = 0;
        catalog.Stage(_ => Interlocked.Increment(ref calls));
        var tasks = new Task<XsltInstructionRegistry>[16];
        using var start = new ManualResetEventSlim(false);
        for (var i = 0; i < tasks.Length; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                start.Wait();
                return catalog.Default;
            });
        }

        start.Set();
        var registries = await Task.WhenAll(tasks);

        await Assert.That(Volatile.Read(ref calls)).IsEqualTo(1);
        foreach (var registry in registries)
        {
            await Assert.That(registry).IsSameReferenceAs(registries[0]);
        }
    }

    [Test]
    public async Task Registry_InjectedInstance_IgnoresGlobalState()
    {
        var builder = new XsltRegistryBuilder();
        builder.Add(new FakeXsltInstruction(RegistryTree.Name("only-here")));
        var injected = builder.Build();
        var name = RegistryTree.Name("only-here");

        await Assert.That(injected.TryGet(in name, out _)).IsTrue();
        await Assert.That(XsltRegistryCatalog.Shared.Default.TryGet(in name, out _)).IsFalse();
    }

    [Test]
    public async Task Registry_BuiltInstance_IsIndependentOfLaterBuilderChanges()
    {
        var builder = new XsltRegistryBuilder();
        var registry = builder.Build();
        builder.Add(new FakeXsltInstruction(RegistryTree.Name("late")));
        var name = RegistryTree.Name("late");

        await Assert.That(registry.TryGet(in name, out _)).IsFalse();
    }

    [Test]
    public async Task Registry_RegistrationOrder_DoesNotAffectLookup()
    {
        var names = new[] { "a", "b", "c" };
        var forward = new XsltRegistryBuilder();
        var backward = new XsltRegistryBuilder();
        for (var i = 0; i < names.Length; i++)
        {
            forward.Add(new FakeXsltInstruction(RegistryTree.Name(names[i])));
            backward.Add(new FakeXsltInstruction(RegistryTree.Name(names[names.Length - 1 - i])));
        }

        var first = forward.Build();
        var second = backward.Build();
        foreach (var local in names)
        {
            var name = RegistryTree.Name(local);
            await Assert.That(first.TryGet(in name, out _)).IsTrue();
            await Assert.That(second.TryGet(in name, out _)).IsTrue();
        }
    }

    [Test]
    public async Task Registry_EveryInstructionFolder_HasExactlyOneRegistrationFile()
    {
        var registrations = new Dictionary<string, int>(StringComparer.Ordinal);
        var folders = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in typeof(XsltRegistryCatalog).Assembly.GetTypes())
        {
            var folder = type.Namespace;
            if (folder is null || !folder.StartsWith(Prefix, StringComparison.Ordinal) || type.IsNested)
            {
                continue;
            }

            if (typeof(IXsltInstruction).IsAssignableFrom(type) && !type.IsAbstract)
            {
                _ = folders.Add(folder);
            }

            if (string.Equals(type.Name, "Registration", StringComparison.Ordinal))
            {
                registrations[folder] = registrations.TryGetValue(folder, out var count) ? count + 1 : 1;
            }
        }

        // A folder that defines instructions has exactly one registration type; folders without instructions are free.
        foreach (var folder in folders)
        {
            await Assert.That(registrations.TryGetValue(folder, out var count) ? count : 0).IsEqualTo(1);
        }
    }
}
