namespace NetEvolve.Schemas.Xsd.Tests.Unit.Validation.Core;

using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>Records every hook as one line: <c>begin</c>, <c>start name</c>, <c>attr name=value</c>, <c>value text</c>, <c>end name</c>, <c>endrun</c>.</summary>
internal sealed class RecordingListener(ListenerInterest interest = ListenerInterest.All) : ValidationListener
{
    private XmlNameTable? _names;

    public List<string> Events { get; } = [];

    public override ListenerInterest Interest => interest;

    public override void BeginRun(in ValidationRunContext context)
    {
        _names = context.Names;
        Events.Add("begin");
    }

    public override void StartElement(in ElementStartInfo info) =>
        Events.Add($"start {_names!.GetString(info.Name.Local)}#{info.Ordinal}@{info.Depth} line {info.Location.Line}");

    public override void Attribute(in AttributeInfo info) =>
        Events.Add(
            $"attr {_names!.GetString(info.Name.Local)}={info.Value.ToString()}{(info.IsDefaulted ? " (default)" : "")}{(info.IsValid ? "" : " (invalid)")}"
        );

    public override void SimpleValue(in SimpleValueInfo info) =>
        Events.Add(
            $"value {info.Value.ToString()}{(info.IsDefaulted ? " (default)" : "")}{(info.IsValid ? "" : " (invalid)")}"
        );

    public override void EndElement(in ElementEndInfo info) =>
        Events.Add($"end #{info.Ordinal}{(info.IsValid ? "" : " (invalid)")}");

    public override void EndRun() => Events.Add("endrun");
}
