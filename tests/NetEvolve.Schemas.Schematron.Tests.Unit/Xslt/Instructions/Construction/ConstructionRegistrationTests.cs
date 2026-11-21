namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Construction;

using System;
using System.Threading.Tasks;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using ConstructionRegistration = NetEvolve.Schemas.Schematron.Xslt.Instructions.Construction.Registration;
using FlowRegistration = NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow.Registration;
using FunctionsRegistration = NetEvolve.Schemas.Schematron.Xslt.Functions.Registration;
using GroupingRegistration = NetEvolve.Schemas.Schematron.Xslt.Instructions.Grouping.Registration;
using KeysRegistration = NetEvolve.Schemas.Schematron.Xslt.Keys.Registration;

/// <summary>The registration of the construction folder (design #150 EP-9 and EP-10) and the synthetic entries it owns.</summary>
public class ConstructionRegistrationTests
{
    private const string Xslt = "http://www.w3.org/1999/XSL/Transform";

    [Test]
    [Arguments("element")]
    [Arguments("attribute")]
    [Arguments("copy")]
    [Arguments("copy-of")]
    [Arguments("comment")]
    [Arguments("processing-instruction")]
    [Arguments("namespace")]
    [Arguments("document")]
    public async Task Registration_RegistersTheInstructionOfTheFolder(string local)
    {
        var builder = new XsltRegistryBuilder();
        ConstructionRegistration.Register(builder);
        var registry = builder.Build();

        var name = new XPathQName(Xslt, local);
        _ = registry.TryGet(in name, out var instruction);

        await Assert.That(registry.IsAvailable(name)).IsTrue();
        await Assert.That(instruction.Position).IsEqualTo(XsltPosition.Instruction);
    }

    [Test]
    public async Task Registration_RegistersNoOtherXsltName()
    {
        var builder = new XsltRegistryBuilder();
        ConstructionRegistration.Register(builder);
        var registry = builder.Build();

        await Assert.That(registry.TryGet(new XPathQName(Xslt, "value-of"), out _)).IsFalse();
        await Assert.That(registry.TryGet(new XPathQName(Xslt, "text"), out _)).IsFalse();
        await Assert.That(registry.TryGet(new XPathQName(Xslt, "attribute-set"), out _)).IsFalse();
        await Assert.That(registry.TryGet(new XPathQName(Xslt, "namespace-alias"), out _)).IsFalse();
        await Assert.That(registry.TryGet(new XPathQName(Xslt, "number"), out _)).IsFalse();
        await Assert.That(registry.TryGet(new XPathQName(Xslt, "result-document"), out _)).IsFalse();
    }

    [Test]
    public async Task Registration_TogetherWithTheOtherFolders_HasNoDuplicateName()
    {
        var builder = new XsltRegistryBuilder();
        FunctionsRegistration.Register(builder);
        FlowRegistration.Register(builder);
        KeysRegistration.Register(builder);
        GroupingRegistration.Register(builder);
        ConstructionRegistration.Register(builder);
        var registry = builder.Build();

        await Assert.That(registry.IsAvailable(new XPathQName(Xslt, "element"))).IsTrue();
        await Assert.That(registry.IsAvailable(new XPathQName(Xslt, "value-of"))).IsTrue();
        await Assert.That(registry.TryGetSynthetic(XsltSyntheticElement.LiteralResultElement, out _)).IsTrue();
        await Assert.That(registry.TryGetSynthetic(XsltSyntheticElement.TextNode, out _)).IsTrue();
    }

    [Test]
    public async Task Registration_RegisteredTwice_IsADuplicateName()
    {
        var builder = new XsltRegistryBuilder();
        ConstructionRegistration.Register(builder);

        await Assert.That(() => ConstructionRegistration.Register(builder)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Registration_WithoutABuilder_IsRejected() =>
        await Assert.That(() => ConstructionRegistration.Register(null!)).Throws<ArgumentNullException>();

    [Test]
    public async Task Registration_ModuleInitializer_StagesIntoTheSharedCatalog()
    {
        var registry = XsltRegistryCatalog.Shared.Default;

        await Assert.That(registry.TryGet(new XPathQName(Xslt, "element"), out _)).IsTrue();
        await Assert.That(registry.TryGetSynthetic(XsltSyntheticElement.LiteralResultElement, out _)).IsTrue();
        await Assert.That(registry.TryGetSynthetic(XsltSyntheticElement.TextNode, out _)).IsTrue();
    }

    [Test]
    public async Task LiteralResultElement_CompilesThroughSyntheticEntry()
    {
        var harness = new ConstructionHarness("<out a=\"1\"/>");

        var resolved = XsltElementResolver.TryResolve(
            harness.Registry,
            harness.Element,
            XsltPosition.Instruction,
            ConstructionHarness.Module,
            out var instruction,
            out _
        );

        await Assert.That(resolved).IsTrue();
        await Assert.That(harness.Executable).IsNotNull();
        await Assert.That(instruction.Name).IsEqualTo(default(XPathQName));
    }

    [Test]
    public async Task TextNode_CompilesThroughSyntheticEntry()
    {
        var harness = new ConstructionHarness("<out>text</out>");
        var text = new XPathNode(harness.Element.Node.FirstChild);

        var resolved = XsltElementResolver.TryResolve(
            harness.Registry,
            text,
            XsltPosition.Instruction,
            ConstructionHarness.Module,
            out var instruction,
            out var problem
        );

        await Assert.That(resolved).IsTrue();
        await Assert.That(problem.Code == default).IsTrue();
        await Assert.That(instruction.Position).IsEqualTo(XsltPosition.Instruction);
    }

    [Test]
    public async Task TextNode_WithoutASyntheticEntry_IsTheUnsupportedConstructError_LocatedAtTheParent()
    {
        var tree = ConstructionSheet.Fragment("<out>\n<x>text</x></out>");
        var text = new XPathNode(tree.Node.FirstChild.NextSibling.FirstChild);
        var registry = new XsltRegistryBuilder().Build();

        var resolved = XsltElementResolver.TryResolve(
            registry,
            text,
            XsltPosition.Instruction,
            ConstructionHarness.Module,
            out _,
            out var problem
        );

        await Assert.That(resolved).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported);
        await Assert.That(problem.Message).Contains("text node");
        await Assert.That(problem.Location.Line).IsEqualTo(2);
    }
}
