namespace NetEvolve.Schemas.Schematron.Tests.Unit.Xslt.Instructions.Flow;

using System.Collections.Generic;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>The diagnostic channel of the tests: it records the messages that <c>xsl:message</c> sends.</summary>
internal sealed class RecordingMessages : IXsltMessageSink
{
    internal List<string> Texts { get; } = [];

    internal List<DocumentLocation> Locations { get; } = [];

    public void Message(string text, DocumentLocation location)
    {
        Texts.Add(text);
        Locations.Add(location);
    }
}
