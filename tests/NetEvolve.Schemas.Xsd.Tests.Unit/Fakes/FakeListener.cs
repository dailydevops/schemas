namespace NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

using System.Collections.Generic;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>A listener that logs every hook call with its arguments.</summary>
internal sealed class FakeListener : ValidationListener
{
    private readonly string _label;
    private readonly List<string> _log;

    public FakeListener(string label, ListenerInterest interest, List<string> log)
    {
        _label = label;
        Interest = interest;
        _log = log;
    }

    public override ListenerInterest Interest { get; }

    public override void BeginRun(in ValidationRunContext context) =>
        _log.Add($"{_label}:BeginRun:maxIssues={context.Limits.MaxIssues}");

    public override void StartElement(in ElementStartInfo info) =>
        _log.Add($"{_label}:StartElement:{info.Ordinal}:depth={info.Depth}");

    public override void Attribute(in AttributeInfo info) =>
        _log.Add(
            $"{_label}:Attribute:owner={info.OwnerOrdinal}:value={info.Value.ToString()}:defaulted={info.IsDefaulted}"
        );

    public override void SimpleValue(in SimpleValueInfo info) =>
        _log.Add($"{_label}:SimpleValue:{info.Ordinal}:value={info.Value.ToString()}:member={info.MemberIndex}");

    public override void EndElement(in ElementEndInfo info) =>
        _log.Add($"{_label}:EndElement:{info.Ordinal}:valid={info.IsValid}");

    public override void EndRun() => _log.Add($"{_label}:EndRun");
}
