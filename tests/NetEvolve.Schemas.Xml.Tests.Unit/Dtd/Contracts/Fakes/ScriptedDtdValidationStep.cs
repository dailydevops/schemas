namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Contracts.Fakes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;

/// <summary>A fake validation step that records every event and reacts to named events with scripted actions, for example reporting an issue.</summary>
internal sealed class ScriptedDtdValidationStep : IDtdValidationStep
{
    private readonly Dictionary<string, Action<DtdValidationContext>> _script = new(StringComparer.Ordinal);

    /// <summary>Gets the received events in arrival order, for example <c>Begin</c> or <c>StartElement:root</c>.</summary>
    internal List<string> Events { get; } = [];

    /// <summary>Gets the context received by <see cref="BeginDocument"/>.</summary>
    internal DtdValidationContext? Context { get; private set; }

    /// <summary>Gets the last start element, as received.</summary>
    internal ValidationElement LastStartElement { get; private set; }

    /// <summary>Gets the last entity reference, as received.</summary>
    internal ValidationEntityReference LastEntityReference { get; private set; }

    /// <summary>Gets the position of the last text, comment, or processing instruction event.</summary>
    internal XmlPosition LastPosition { get; private set; }

    /// <summary>Scripts a reaction for an event name; the name is the entry of <see cref="Events"/>.</summary>
    /// <param name="eventName">The event name.</param>
    /// <param name="reaction">The reaction, called after the event is recorded.</param>
    /// <returns>This step.</returns>
    internal ScriptedDtdValidationStep On(string eventName, Action<DtdValidationContext> reaction)
    {
        _script[eventName] = reaction;

        return this;
    }

    public void BeginDocument(DtdValidationContext context)
    {
        Context = context;
        Record("Begin");
    }

    public void StartElement(in ValidationElement element)
    {
        LastStartElement = element;
        Record("StartElement:" + element.Name.ToString());
    }

    public void EntityReference(in ValidationEntityReference reference)
    {
        LastEntityReference = reference;
        Record("EntityReference:" + reference.Name.ToString());
    }

    public void Text(ReadOnlySpan<char> text, XmlTextKind kind, in XmlPosition position)
    {
        LastPosition = position;
        Record("Text:" + kind + ":" + text.ToString());
    }

    public void Comment(in XmlPosition position)
    {
        LastPosition = position;
        Record("Comment");
    }

    public void ProcessingInstruction(in XmlPosition position)
    {
        LastPosition = position;
        Record("ProcessingInstruction");
    }

    public void EndElement(in ValidationElement element) => Record("EndElement:" + element.Name.ToString());

    public void EndDocument() => Record("End");

    private void Record(string eventName)
    {
        Events.Add(eventName);
        if (_script.TryGetValue(eventName, out var reaction))
        {
            reaction(Context!);
        }
    }
}
