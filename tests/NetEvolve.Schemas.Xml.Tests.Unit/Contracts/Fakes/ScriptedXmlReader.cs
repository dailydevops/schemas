namespace NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;

using System;
using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>A reader that replays a script of events, need-more-data stops, and one optional error.</summary>
internal sealed class ScriptedXmlReader : IXmlReader
{
    private readonly List<Step> _steps = [];
    private int _next;

    public XmlPosition Position { get; private set; }

    public XmlDiagnostic? Diagnostic { get; private set; }

    public ScriptedXmlReader Event(XmlEventRecord record)
    {
        _steps.Add(new Step(record, XmlReadStatus.Completed, null));
        return this;
    }

    public ScriptedXmlReader NeedMoreData()
    {
        _steps.Add(new Step(null, XmlReadStatus.NeedMoreData, null));
        return this;
    }

    public ScriptedXmlReader Fail(XmlErrorCode code, XmlPosition position)
    {
        _steps.Add(new Step(null, XmlReadStatus.Error, new XmlDiagnostic(code, position)));
        return this;
    }

    public XmlReadStatus Pump<TSink>(ref TSink sink)
        where TSink : IXmlSink
    {
        while (_next < _steps.Count)
        {
            var step = _steps[_next];
            _next++;
            if (step.Record is { } record)
            {
                Dispatch(ref sink, record);
                continue;
            }

            if (step.Status == XmlReadStatus.Error)
            {
                Diagnostic = step.Failure;
                Position = step.Failure!.Position;
                _next = _steps.Count;
            }

            return step.Status;
        }

        return XmlReadStatus.Completed;
    }

    private void Dispatch<TSink>(ref TSink sink, XmlEventRecord record)
        where TSink : IXmlSink
    {
        switch (record.Kind)
        {
            case XmlEventKind.StartDocument:
                sink.StartDocument(new XmlDeclaration(record.Text.AsSpan(), default, null));
                break;
            case XmlEventKind.EndDocument:
                sink.EndDocument();
                break;
            case XmlEventKind.StartElement:
                sink.StartElement(record.Name, record.Position);
                Position = record.Position;
                break;
            case XmlEventKind.NamespaceDeclaration:
                sink.NamespaceDeclaration(record.Prefix, record.Uri);
                break;
            case XmlEventKind.Attribute:
                sink.Attribute(record.Name, record.Text.AsSpan());
                break;
            case XmlEventKind.EndAttributes:
                sink.EndAttributes();
                break;
            case XmlEventKind.EndElement:
                sink.EndElement(record.Name);
                break;
            case XmlEventKind.Text:
                sink.Text(record.Text.AsSpan(), record.TextKind);
                break;
            case XmlEventKind.Comment:
                sink.Comment(record.Text.AsSpan());
                break;
            case XmlEventKind.ProcessingInstruction:
                sink.ProcessingInstruction(record.Prefix, record.Text.AsSpan());
                break;
            default:
                sink.Doctype(new XmlDoctypeInfo(record.Text.AsSpan(), default, default, default));
                break;
        }
    }

    private readonly record struct Step(XmlEventRecord? Record, XmlReadStatus Status, XmlDiagnostic? Failure);
}
