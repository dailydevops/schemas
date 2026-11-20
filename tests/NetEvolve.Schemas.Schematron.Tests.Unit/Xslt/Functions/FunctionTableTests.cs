namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Functions;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionFixture;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.FunctionTree;
using static NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Functions.Fx;

/// <summary>
/// The table of compiled functions, keyed by expanded name and arity (XSLT 2.0 section 10.3: functions are not polymorphic): the
/// duplicate rule XTSE0770, import precedence, and the registration of the instructions of the folder (design #150 EP-9, EP-10).
/// </summary>
public class FunctionTableTests
{
    private static XsltFunctionComponent Component(string name, int arity, int line)
    {
        var parameters = new string[arity];
        for (var i = 0; i < arity; i++)
        {
            parameters[i] = "p" + i;
        }

        var fixture = Create();
        return fixture.CompileOne(Simple(name, "xs:integer", parameters, "1", line))!;
    }

    [Test]
    public async Task Table_SameNameAndArityAtTheSamePrecedence_IsAStaticErrorXtse0770()
    {
        var fixture = Create();

        _ = fixture.CompileAll(
            Simple("dup", "xs:integer", ["a"], "1", 10),
            Simple("dup", "xs:integer", ["b"], "2", 20)
        );

        await Assert.That(Codes(fixture.Problems)).IsEqualTo("NES3200/XTSE0770");
        await Assert.That(fixture.Problems.Problems[0].Location.Line).IsEqualTo(20);
        await Assert.That(fixture.Problems.Problems[0].Message).Contains("f:dup").And.Contains("line 10");
    }

    [Test]
    public async Task Table_SameNameWithDifferentArity_IsNotADuplicate()
    {
        var builder = new XsltFunctionTableBuilder();

        var first = builder.TryAdd(Component("same", 1, 10), 0, out _);
        var second = builder.TryAdd(Component("same", 2, 20), 0, out _);
        var table = builder.Build();

        await Assert.That(first).IsTrue();
        await Assert.That(second).IsTrue();
        await Assert.That(table.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Table_HigherPrecedenceWins_AndLowerPrecedenceIsIgnored()
    {
        var low = Component("p", 1, 10);
        var high = Component("p", 1, 20);
        var lower = Component("p", 1, 30);
        var name = new XPathQName(Functions, "p");
        var builder = new XsltFunctionTableBuilder();

        var a = builder.TryAdd(low, 1, out _);
        var b = builder.TryAdd(high, 2, out _);
        var c = builder.TryAdd(lower, 1, out _);
        var table = builder.Build();

        await Assert.That(a && b && c).IsTrue();
        await Assert.That(table.Count).IsEqualTo(1);
        await Assert.That(table.TryGet(name, 1, out var found)).IsTrue();
        await Assert.That(found).IsSameReferenceAs(high);
    }

    [Test]
    public async Task Table_Lookup_ByNameAndArityOnly()
    {
        var builder = new XsltFunctionTableBuilder();
        _ = builder.TryAdd(Component("a", 1, 10), 0, out _);
        var table = builder.Build();

        await Assert.That(table.TryGet(new XPathQName(Functions, "a"), 1, out _)).IsTrue();
        await Assert.That(table.TryGet(new XPathQName(Functions, "a"), 0, out _)).IsFalse();
        await Assert.That(table.TryGet(new XPathQName(Functions, "b"), 1, out _)).IsFalse();
        await Assert.That(table.TryGet(new XPathQName("urn:other", "a"), 1, out _)).IsFalse();
    }

    [Test]
    public async Task Table_BuiltInstance_IsIndependentOfLaterBuilderChanges()
    {
        var builder = new XsltFunctionTableBuilder();
        var table = builder.Build();

        _ = builder.TryAdd(Component("late", 0, 10), 0, out _);

        await Assert.That(table.Count).IsEqualTo(0);
        await Assert.That(table.TryGet(new XPathQName(Functions, "late"), 0, out _)).IsFalse();
    }

    [Test]
    public async Task Table_Lookup_RejectsNegativeArity()
    {
        var table = new XsltFunctionTableBuilder().Build();

        await Assert
            .That(() => table.TryGet(new XPathQName(Functions, "a"), -1, out _))
            .Throws<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Registration_RegistersTheFourInstructionsWithTheirPositions()
    {
        var builder = new XsltRegistryBuilder();
        Registration.Register(builder);
        var registry = builder.Build();

        await Assert.That(PositionOf(registry, "function")).IsEqualTo(XsltPosition.Declaration);
        await Assert.That(PositionOf(registry, "sequence")).IsEqualTo(XsltPosition.Instruction);
        await Assert
            .That(PositionOf(registry, "variable"))
            .IsEqualTo(XsltPosition.Instruction | XsltPosition.Declaration);
        await Assert.That(PositionOf(registry, "param")).IsEqualTo(XsltPosition.Child | XsltPosition.Declaration);
        await Assert.That(registry.IsAvailable(RegistryTreeName("sequence"))).IsTrue();
        await Assert.That(registry.IsAvailable(RegistryTreeName("function"))).IsFalse();
        await Assert.That(registry.IsAvailable(RegistryTreeName("param"))).IsFalse();
    }

    [Test]
    public async Task Registration_ModuleInitializer_StagesIntoTheSharedCatalog()
    {
        var registry = XsltRegistryCatalog.Shared.Default;

        await Assert.That(registry.TryGet(RegistryTreeName("function"), out var function)).IsTrue();
        await Assert.That(function).IsTypeOf<XsltFunctionInstruction>();
        await Assert.That(registry.TryGet(RegistryTreeName("sequence"), out _)).IsTrue();
    }

    [Test]
    public async Task Registration_RegisterTwice_FailsAtOnceOnTheDuplicateName()
    {
        var builder = new XsltRegistryBuilder();
        Registration.Register(builder);

        await Assert.That(() => Registration.Register(builder)).Throws<InvalidOperationException>();
        await Assert.That(() => Registration.Register(null!)).Throws<ArgumentNullException>();
    }

    private static XPathQName RegistryTreeName(string local) => new(Xslt, local);

    private static XsltPosition PositionOf(XsltInstructionRegistry registry, string local)
    {
        var name = RegistryTreeName(local);
        _ = registry.TryGet(in name, out var instruction);
        return instruction.Position;
    }
}
