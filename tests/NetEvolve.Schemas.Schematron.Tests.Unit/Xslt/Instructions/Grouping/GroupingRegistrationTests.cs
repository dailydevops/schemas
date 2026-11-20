namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Grouping;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Grouping;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;
using FlowRegistration = NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow.Registration;
using FunctionsRegistration = NetEvolve.Schemas.Schematron.Xslt.Functions.Registration;
using KeysRegistration = NetEvolve.Schemas.Schematron.Xslt.Keys.Registration;

/// <summary>The registration of the grouping folder (design #150 EP-9 and EP-10): one instruction, and no second <c>xsl:sort</c>.</summary>
public class GroupingRegistrationTests
{
    private const string Xslt = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public async Task Registration_RegistersForEachGroupAsAnInstruction_AndNothingElse()
    {
        var builder = new XsltRegistryBuilder();
        Registration.Register(builder);
        var registry = builder.Build();

        var name = new XPathQName(Xslt, "for-each-group");
        _ = registry.TryGet(in name, out var instruction);

        await Assert.That(registry.IsAvailable(name)).IsTrue();
        await Assert.That(instruction.Position).IsEqualTo(XsltPosition.Instruction);
        await Assert.That(registry.TryGet(new XPathQName(Xslt, "sort"), out _)).IsFalse();
        await Assert.That(registry.TryGet(new XPathQName(Xslt, "for-each"), out _)).IsFalse();
    }

    [Test]
    public async Task Registration_TogetherWithTheOtherFolders_HasNoDuplicateName_AndSortStaysTheOneOfTheFlowFolder()
    {
        var builder = new XsltRegistryBuilder();
        FunctionsRegistration.Register(builder);
        FlowRegistration.Register(builder);
        KeysRegistration.Register(builder);
        Registration.Register(builder);
        var registry = builder.Build();

        await Assert.That(registry.IsAvailable(new XPathQName(Xslt, "for-each-group"))).IsTrue();
        await Assert.That(registry.IsAvailable(new XPathQName(Xslt, "for-each"))).IsTrue();
    }

    [Test]
    public async Task Registration_RegisteredTwice_IsADuplicateName()
    {
        var builder = new XsltRegistryBuilder();
        Registration.Register(builder);

        await Assert.That(() => Registration.Register(builder)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Registration_ModuleInitializer_StagesIntoTheSharedCatalog() =>
        await Assert
            .That(XsltRegistryCatalog.Shared.Default.TryGet(new XPathQName(Xslt, "for-each-group"), out _))
            .IsTrue();

    [Test]
    public async Task Registration_WithoutABuilder_IsRejected() =>
        await Assert.That(() => Registration.Register(null!)).Throws<ArgumentNullException>();
}
