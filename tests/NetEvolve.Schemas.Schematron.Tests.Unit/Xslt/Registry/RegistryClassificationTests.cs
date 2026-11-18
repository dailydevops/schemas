namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Registry;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Tests.Unit.Fakes;
using NetEvolve.Schemas.Schematron.Xslt.Registry;
using NetEvolve.Schemas.XPath.Api;

public class RegistryClassificationTests
{
    private const string Module = "file:///stylesheets/rules.xsl";

    private static XsltInstructionRegistry Registry(params FakeXsltInstruction[] instructions)
    {
        var builder = new XsltRegistryBuilder();
        foreach (var instruction in instructions)
        {
            builder.Add(instruction);
        }

        return builder.Build();
    }

    [Test]
    public async Task Classify_Registered_ReturnsTheInstructionForItsExpandedName()
    {
        var name = RegistryTree.Name("fake");
        var fake = new FakeXsltInstruction(name);
        var registry = Registry(fake);

        var found = registry.TryGet(in name, out var viaTryGet);
        var kind = registry.Classify(in name, XsltPosition.Instruction, out var viaClassify);

        await Assert.That(found).IsTrue();
        await Assert.That(viaTryGet).IsSameReferenceAs(fake);
        await Assert.That(kind).IsEqualTo(XsltElementClass.Registered);
        await Assert.That(viaClassify).IsSameReferenceAs(fake);
    }

    [Test]
    public async Task TryGet_UnregisteredName_ReturnsFalse()
    {
        var registry = Registry(new FakeXsltInstruction(RegistryTree.Name("fake")));
        var other = RegistryTree.Name("template");

        await Assert.That(registry.TryGet(in other, out _)).IsFalse();
    }

    [Test]
    public async Task Resolve_UnregisteredTemplate_RaisesUnsupportedWithLineAndColumn()
    {
        var registry = Registry();
        var element = RegistryTree.Element("xsl", "template", RegistryTree.Xslt, 7, 5);

        var resolved = XsltElementResolver.TryResolve(
            registry,
            element,
            XsltPosition.Declaration,
            Module,
            out _,
            out var problem
        );

        await Assert.That(resolved).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported);
        await Assert.That(problem.Location.Source).IsEqualTo(Module);
        await Assert.That(problem.Location.Line).IsEqualTo(7);
        await Assert.That(problem.Location.Column).IsEqualTo(5);
        await Assert.That(problem.Message).Contains("xsl:template");
    }

    [Test]
    public async Task Resolve_DeclarationInInstructionPosition_ReportsMisplaced()
    {
        var registry = Registry(new FakeXsltInstruction(RegistryTree.Name("function"), XsltPosition.Declaration));
        var element = RegistryTree.Element("xsl", "function", RegistryTree.Xslt, 3, 9);

        var resolved = XsltElementResolver.TryResolve(
            registry,
            element,
            XsltPosition.Instruction,
            Module,
            out _,
            out var problem
        );

        await Assert.That(resolved).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.ElementMisplaced);
        await Assert.That(problem.Constraint).IsEqualTo("XTSE0010");
        await Assert.That(problem.Location.Line).IsEqualTo(3);
        await Assert.That(problem.Location.Column).IsEqualTo(9);
        await Assert.That(problem.Message).Contains("xsl:function");
    }

    [Test]
    public async Task Resolve_UnregisteredElementInWrongPosition_IsUnsupportedNotMisplaced()
    {
        var registry = Registry(new FakeXsltInstruction(RegistryTree.Name("function"), XsltPosition.Declaration));
        var element = RegistryTree.Element("xsl", "template", RegistryTree.Xslt, 1, 1);

        _ = XsltElementResolver.TryResolve(registry, element, XsltPosition.Instruction, null, out _, out var problem);

        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported);
    }

    [Test]
    public async Task Resolve_LiteralResultElement_UsesSyntheticEntry()
    {
        var literal = new FakeXsltInstruction(default);
        var builder = new XsltRegistryBuilder();
        builder.AddSynthetic(XsltSyntheticElement.LiteralResultElement, literal);
        var registry = builder.Build();
        var element = RegistryTree.Element("html", "p", "http://www.w3.org/1999/xhtml", 4, 2);

        var resolved = XsltElementResolver.TryResolve(
            registry,
            element,
            XsltPosition.Instruction,
            null,
            out var instruction,
            out _
        );

        await Assert.That(resolved).IsTrue();
        await Assert.That(instruction).IsSameReferenceAs(literal);
    }

    [Test]
    public async Task Resolve_LiteralResultElementWithoutSyntheticEntry_IsUnsupported()
    {
        var element = RegistryTree.Element("html", "p", "http://www.w3.org/1999/xhtml", 4, 2);

        var resolved = XsltElementResolver.TryResolve(
            Registry(),
            element,
            XsltPosition.Instruction,
            null,
            out _,
            out var problem
        );

        await Assert.That(resolved).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported);
        await Assert.That(problem.Message).Contains("literal result element");
        await Assert.That(problem.Location.Line).IsEqualTo(4);
    }

    [Test]
    public async Task Synthetic_Entries_ResolveWithoutXPathQName()
    {
        var text = new FakeXsltInstruction(default);
        var builder = new XsltRegistryBuilder();
        builder.AddSynthetic(XsltSyntheticElement.TextNode, text);
        var registry = builder.Build();

        var found = registry.TryGetSynthetic(XsltSyntheticElement.TextNode, out var instruction);
        var missing = registry.TryGetSynthetic(XsltSyntheticElement.LiteralResultElement, out _);

        await Assert.That(found).IsTrue();
        await Assert.That(instruction).IsSameReferenceAs(text);
        await Assert.That(missing).IsFalse();
    }

    [Test]
    [Arguments("xsl", "function")]
    [Arguments("", "function")]
    [Arguments("zz", "function")]
    public async Task Classify_DefaultNamespaceAndUnusualPrefix_AreRecognized(string prefix, string local)
    {
        var fake = new FakeXsltInstruction(RegistryTree.Name(local), XsltPosition.Declaration);
        var registry = Registry(fake);
        var element = RegistryTree.Element(prefix, local, RegistryTree.Xslt, 2, 3);

        var resolved = XsltElementResolver.TryResolve(
            registry,
            element,
            XsltPosition.Declaration,
            null,
            out var instruction,
            out _
        );

        await Assert.That(resolved).IsTrue();
        await Assert.That(instruction).IsSameReferenceAs(fake);
    }

    [Test]
    public async Task Classify_XslPrefixBoundToAnotherNamespace_IsNotXslt()
    {
        var registry = Registry(new FakeXsltInstruction(RegistryTree.Name("function"), XsltPosition.Declaration));
        var element = RegistryTree.Element("xsl", "function", "urn:other", 2, 3);

        _ = XsltElementResolver.TryResolve(registry, element, XsltPosition.Declaration, null, out _, out var problem);
        var name = new XPathQName("urn:other", "function");
        var kind = registry.Classify(in name, XsltPosition.Declaration, out _);

        await Assert.That(kind).IsEqualTo(XsltElementClass.NotXslt);
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported);
    }

    [Test]
    public async Task IsAvailable_RegisteredInstruction_True_DeclarationFalse_UnregisteredFalse()
    {
        var registry = Registry(
            new FakeXsltInstruction(RegistryTree.Name("value-of"), XsltPosition.Instruction),
            new FakeXsltInstruction(RegistryTree.Name("function"), XsltPosition.Declaration),
            new FakeXsltInstruction(RegistryTree.Name("variable"), XsltPosition.Instruction | XsltPosition.Declaration)
        );
        var valueOf = RegistryTree.Name("value-of");
        var function = RegistryTree.Name("function");
        var variable = RegistryTree.Name("variable");
        var template = RegistryTree.Name("template");

        await Assert.That(registry.IsAvailable(in valueOf)).IsTrue();
        await Assert.That(registry.IsAvailable(in function)).IsFalse();
        await Assert.That(registry.IsAvailable(in variable)).IsTrue();
        await Assert.That(registry.IsAvailable(in template)).IsFalse();
    }

    [Test]
    public async Task Classify_XslVariable_InBothPositions()
    {
        var registry = Registry(
            new FakeXsltInstruction(RegistryTree.Name("variable"), XsltPosition.Instruction | XsltPosition.Declaration)
        );
        var name = RegistryTree.Name("variable");

        await Assert
            .That(registry.Classify(in name, XsltPosition.Declaration, out _))
            .IsEqualTo(XsltElementClass.Registered);
        await Assert
            .That(registry.Classify(in name, XsltPosition.Instruction, out _))
            .IsEqualTo(XsltElementClass.Registered);
        await Assert.That(registry.Classify(in name, XsltPosition.Child, out _)).IsEqualTo(XsltElementClass.Misplaced);
    }

    [Test]
    public async Task Classify_XslSort_InForEachVersusTopLevel()
    {
        var registry = Registry(new FakeXsltInstruction(RegistryTree.Name("sort"), XsltPosition.Child));
        var name = RegistryTree.Name("sort");

        await Assert.That(registry.Classify(in name, XsltPosition.Child, out _)).IsEqualTo(XsltElementClass.Registered);
        await Assert
            .That(registry.Classify(in name, XsltPosition.Declaration, out _))
            .IsEqualTo(XsltElementClass.Misplaced);
    }

    [Test]
    [MethodDataSource(nameof(AllNames))]
    public async Task Classify_EveryXslt20And30Name_UnregisteredRaisesCodedError(string local)
    {
        var element = RegistryTree.Element("xsl", local, RegistryTree.Xslt, 11, 13);

        var resolved = XsltElementResolver.TryResolve(
            Registry(),
            element,
            XsltPosition.Instruction,
            Module,
            out _,
            out var problem
        );

        await Assert.That(resolved).IsFalse();
        await Assert.That(problem.Code).IsEqualTo(SchematronDiagnosticCodes.XsltUnsupported);
        await Assert.That(problem.Message).Contains("xsl:" + local);
        await Assert.That(problem.Location.Line).IsEqualTo(11);
        await Assert.That(problem.Location.Column).IsEqualTo(13);
    }

    [Test]
    [MethodDataSource(nameof(AllNames))]
    public async Task Classify_EveryXslt20And30Name_InDefaultRegistry_IsRegisteredOrUnsupported(string local)
    {
        var registry = XsltRegistryCatalog.Shared.Default;
        var name = RegistryTree.Name(local);
        var element = RegistryTree.Element("xsl", local, RegistryTree.Xslt, 5, 6);
        var positions = new[] { XsltPosition.Instruction, XsltPosition.Declaration, XsltPosition.Child };

        var registered = registry.TryGet(in name, out _);
        var anyPosition = false;
        foreach (var position in positions)
        {
            anyPosition |= registry.Classify(in name, position, out _) == XsltElementClass.Registered;
        }

        _ = XsltElementResolver.TryResolve(registry, element, XsltPosition.None, null, out _, out var problem);

        await Assert.That(anyPosition).IsEqualTo(registered);
        await Assert
            .That(problem.Code)
            .IsEqualTo(
                registered ? SchematronDiagnosticCodes.ElementMisplaced : SchematronDiagnosticCodes.XsltUnsupported
            );
    }

    [Test]
    public async Task AllNames_ListsXslt20And30Elements_EachOnce()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in AllNames())
        {
            await Assert.That(seen.Add(name)).IsTrue();
        }

        await Assert.That(seen.Count).IsEqualTo(78);
        await Assert.That(seen.Contains("template")).IsTrue();
        await Assert.That(seen.Contains("on-completion")).IsTrue();
    }

    public static IEnumerable<string> AllNames() => XsltElementNames.All();
}
