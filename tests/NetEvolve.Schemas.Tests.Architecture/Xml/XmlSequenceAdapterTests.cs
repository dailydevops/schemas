namespace NetEvolve.Schemas.Tests.Architecture.Xml;

using NetEvolve.Schemas.Tests.Architecture.Shared;

/// <summary>The sequence adapter (XML-56) only moves segments into the parser; the tokenizer stays in the reader core.</summary>
public class XmlSequenceAdapterTests
{
    private const string Adapter = $"{XmlLayering.Root}.Input.Sequence";

    private static System.Reflection.Assembly XmlAssembly => SourceAssemblies.Load(SourceAssemblies.Xml);

    [Test]
    public async Task SequenceAdapter_ContainsNoTokenizerLogic()
    {
        // The prefix covers XmlToken, XmlTokenizer, XmlTokenKind, and XmlTokenStatus.
        var violations = XmlLayering.FindDependencies(
            XmlAssembly,
            Adapter,
            [$"{XmlLayering.Root}.Reader.Core.XmlToken"]
        );

        await Assert.That(violations).IsEmpty();
    }

    [Test]
    public async Task SequenceAdapter_FeedsTheParserOfTheReaderCore()
    {
        // The rule above must see dependencies inside method bodies: the adapter does depend on the parser.
        var violations = XmlLayering.FindDependencies(
            XmlAssembly,
            Adapter,
            [$"{XmlLayering.Root}.Reader.Core.XmlParser"]
        );

        await Assert.That(violations).IsNotEmpty();
    }

    [Test]
    public async Task SequenceAdapter_UsesNoLinq()
    {
        var violations = XmlLayering.FindDependencies(XmlAssembly, Adapter, ["System.Linq"]);

        await Assert.That(violations).IsEmpty();
    }
}
