namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Regex;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Regex;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;
using FlowRegistration = NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow.Registration;
using GroupingRegistration = NetEvolve.Schemas.Schematron.Xslt.Instructions.Grouping.Registration;
using KeysRegistration = NetEvolve.Schemas.Schematron.Xslt.Keys.Registration;

/// <summary>The registration of the regex folder (design #150 EP-9 and EP-10): one instruction and its two structural children.</summary>
public class RegexRegistrationTests
{
    private const string Xslt = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    public async Task Registration_RegistersAnalyzeStringAsAnInstruction_AndTheTwoSubstringsAsChildren()
    {
        var builder = new XsltRegistryBuilder();
        Registration.Register(builder);
        var registry = builder.Build();

        _ = registry.TryGet(new XPathQName(Xslt, "analyze-string"), out var instruction);
        _ = registry.TryGet(new XPathQName(Xslt, "matching-substring"), out var matching);
        _ = registry.TryGet(new XPathQName(Xslt, "non-matching-substring"), out var nonMatching);

        await Assert.That(instruction.Position).IsEqualTo(XsltPosition.Instruction);
        await Assert.That(matching.Position).IsEqualTo(XsltPosition.Child);
        await Assert.That(nonMatching.Position).IsEqualTo(XsltPosition.Child);
        await Assert.That(registry.TryGet(new XPathQName(Xslt, "for-each"), out _)).IsFalse();
    }

    [Test]
    public async Task Registration_TogetherWithTheOtherFolders_HasNoDuplicateName()
    {
        var builder = new XsltRegistryBuilder();
        FlowRegistration.Register(builder);
        GroupingRegistration.Register(builder);
        KeysRegistration.Register(builder);
        Registration.Register(builder);
        var registry = builder.Build();

        await Assert.That(registry.IsAvailable(new XPathQName(Xslt, "analyze-string"))).IsTrue();
        await Assert.That(registry.IsAvailable(new XPathQName(Xslt, "for-each-group"))).IsTrue();
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
            .That(XsltRegistryCatalog.Shared.Default.TryGet(new XPathQName(Xslt, "analyze-string"), out _))
            .IsTrue();

    [Test]
    public async Task Registration_WithoutABuilder_IsRejected() =>
        await Assert.That(() => Registration.Register(null!)).Throws<ArgumentNullException>();
}
