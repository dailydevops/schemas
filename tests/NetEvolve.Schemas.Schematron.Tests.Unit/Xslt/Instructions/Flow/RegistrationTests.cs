namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;
using FunctionsRegistration = NetEvolve.Schemas.Schematron.Xslt.Functions.Registration;

/// <summary>The registration of the flow instructions (design #150 EP-9 and EP-10): which element sits in which position.</summary>
public class RegistrationTests
{
    private const string Xslt = "http://www.w3.org/1999/XSL/Transform";

    private static XsltPosition PositionOf(XsltInstructionRegistry registry, string local)
    {
        var name = new XPathQName(Xslt, local);
        _ = registry.TryGet(in name, out var instruction);
        return instruction.Position;
    }

    [Test]
    [Arguments("value-of", "Instruction")]
    [Arguments("text", "Instruction")]
    [Arguments("if", "Instruction")]
    [Arguments("choose", "Instruction")]
    [Arguments("for-each", "Instruction")]
    [Arguments("message", "Instruction")]
    [Arguments("when", "Child")]
    [Arguments("otherwise", "Child")]
    [Arguments("sort", "Child")]
    public async Task Registration_RegistersTheNineElementsWithTheirPositions(string local, string position)
    {
        var builder = new XsltRegistryBuilder();
        Registration.Register(builder);
        var registry = builder.Build();

        await Assert.That(PositionOf(registry, local).ToString()).IsEqualTo(position);
    }

    [Test]
    public async Task Registration_InstructionsAreAvailable_ChildrenAreNot()
    {
        var builder = new XsltRegistryBuilder();
        Registration.Register(builder);
        var registry = builder.Build();

        await Assert.That(registry.IsAvailable(new XPathQName(Xslt, "for-each"))).IsTrue();
        await Assert.That(registry.IsAvailable(new XPathQName(Xslt, "sort"))).IsFalse();
        await Assert.That(registry.IsAvailable(new XPathQName(Xslt, "when"))).IsFalse();
    }

    [Test]
    public async Task Registration_ModuleInitializer_StagesIntoTheSharedCatalog()
    {
        var registry = XsltRegistryCatalog.Shared.Default;

        await Assert.That(registry.TryGet(new XPathQName(Xslt, "value-of"), out _)).IsTrue();
        await Assert.That(registry.TryGet(new XPathQName(Xslt, "function"), out _)).IsTrue();
    }

    [Test]
    public async Task Registration_TogetherWithTheFunctionFolder_HasNoDuplicateName()
    {
        var builder = new XsltRegistryBuilder();

        FunctionsRegistration.Register(builder);
        Registration.Register(builder);

        await Assert.That(builder.Build().TryGet(new XPathQName(Xslt, "sequence"), out _)).IsTrue();
    }

    [Test]
    public async Task Registration_RegisterTwice_FailsAtOnceOnTheDuplicateName()
    {
        var builder = new XsltRegistryBuilder();
        Registration.Register(builder);

        await Assert.That(() => Registration.Register(builder)).Throws<InvalidOperationException>();
        await Assert.That(() => Registration.Register(null!)).Throws<ArgumentNullException>();
    }
}
