namespace NetEvolve.Schemas.Schematron.Tests.Unit.Bindings.Registry;

using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Bindings.Registry;
using NetEvolve.Schemas.Schematron.Model;
using NetEvolve.Schemas.Schematron.Seams;

public class SlotReferencesTests
{
    private const string BaseUri = "file:///schema.sch";

    private static QueryStaticScope Scope(params string[] variableNames) =>
        new(
            [
                new SchematronNamespace(new DocumentLocation(BaseUri, 1, 1), "f", "urn:f"),
                new SchematronNamespace(new DocumentLocation(BaseUri, 2, 1), "g", "urn:g"),
            ],
            variableNames,
            BaseUri,
            SchematronEdition.Iso2016
        );

    [Test]
    public async Task Collect_OfAPrefixedReference_FindsTheSlotOfItsExpandedName()
    {
        SlotReferences.Collect("$f:v", Scope("{urn:g}v", "{urn:f}v"), out var slots, out var names);

        await Assert.That(slots).IsEquivalentTo([1]);
        await Assert.That(names[0].NamespaceName).IsEqualTo("urn:f");
        await Assert.That(names[0].LocalName).IsEqualTo("v");
    }

    [Test]
    public async Task Collect_OfAPrefixOtherThanTheDeclaredOne_FindsNoSlot()
    {
        SlotReferences.Collect("$g:v", Scope("{urn:f}v"), out var slots, out _);

        await Assert.That(slots).IsEmpty();
    }

    [Test]
    public async Task Collect_OfAnUndeclaredPrefix_FindsNoSlot()
    {
        SlotReferences.Collect("$h:v", Scope("{urn:f}v", "v"), out var slots, out _);

        await Assert.That(slots).IsEmpty();
    }

    [Test]
    public async Task Collect_OfAnUnprefixedReference_IgnoresThePrefixedVariableOfTheSameLocalName()
    {
        SlotReferences.Collect("$v", Scope("{urn:f}v"), out var slots, out _);

        await Assert.That(slots).IsEmpty();
    }

    [Test]
    [Arguments("for $v in 1 return $v", 0)]
    [Arguments("for $v in 1 return $f:v", 1)]
    public async Task Collect_OfARangeVariable_SkipsOnlyTheUnprefixedReference(string text, int count)
    {
        SlotReferences.Collect(text, Scope("v", "{urn:f}v"), out var slots, out _);

        await Assert.That(slots.Length).IsEqualTo(count);
    }
}
