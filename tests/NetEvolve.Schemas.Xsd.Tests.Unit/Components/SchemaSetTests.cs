namespace NetEvolve.Schemas.Xsd.Tests.Unit.Components;

using System.Reflection;
using System.Threading.Tasks;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using static NetEvolve.Schemas.Xsd.Tests.Unit.Components.Fixture;

public class SchemaSetTests
{
    private static XsdSchemaSet Frozen(out RefComponent[] components)
    {
        var registry = NewRegistry(out _);
        components = new RefComponent[200];
        for (var i = 0; i < components.Length; i++)
        {
            components[i] = new RefComponent(XsdComponentKind.ElementDeclaration, "e" + i, i + 1);
            registry.Add(components[i]);
        }

        return registry.Freeze(XsdVersion.V11);
    }

    [Test]
    public async Task SchemaSet_ReadFromManyThreads_GivesIdenticalResults()
    {
        var set = Frozen(out var components);
        var failures = 0;

        await ParallelPolyfills.ForEachAsync(Enumerable.Range(0, 64), async (_, _) =>
        {
            for (var i = 0; i < components.Length; i++)
            {
                if (
                    !set.TryGet(XsdComponentKind.ElementDeclaration, Name("e" + i), out var found)
                    || !ReferenceEquals(found, components[i])
                )
                {
                    _ = Interlocked.Increment(ref failures);
                }
            }

            await Task.Yield();
        });

        await Assert.That(failures).IsEqualTo(0);
        await Assert.That(set.Count).IsEqualTo(200);
        await Assert.That(set.Version).IsEqualTo(XsdVersion.V11);
    }

    [Test]
    public async Task SchemaSet_PublicSurface_HasNoMutationApi()
    {
        var members = typeof(XsdSchemaSet).GetMembers(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static
        );
        var mutating = new[] { "Add", "Remove", "Clear", "Set", "Register", "Freeze", "Resolve" };

        foreach (var member in members)
        {
            await Assert.That(mutating.Any(m => member.Name.StartsWith(m, StringComparison.Ordinal))).IsFalse();
            if (member is PropertyInfo property)
            {
                await Assert.That(property.SetMethod is null || !property.SetMethod.IsPublic).IsTrue();
            }

            if (member is FieldInfo field)
            {
                await Assert.That(field.IsInitOnly || field.IsLiteral).IsTrue();
            }
        }
    }

    [Test]
    public async Task Registry_AddAfterFreeze_Throws()
    {
        var registry = NewRegistry(out _);
        _ = registry.Freeze(XsdVersion.V11);

        _ = await Assert
            .That(() => registry.Add(new RefComponent(XsdComponentKind.ElementDeclaration, "late", 1)))
            .Throws<InvalidOperationException>();
    }
}
