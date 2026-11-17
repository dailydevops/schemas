namespace NetEvolve.Schemas.Xml.Tests.Unit.Contracts.Fakes;

using NetEvolve.Schemas.Xml.Contracts;

/// <summary>One sink event as data, used both as a script step and as a recorded event.</summary>
internal readonly record struct XmlEventRecord(
    XmlEventKind Kind,
    XmlName Name = default,
    XmlPosition Position = default,
    XmlSymbol Prefix = default,
    XmlSymbol Uri = default,
    string Text = "",
    XmlTextKind TextKind = XmlTextKind.Text
);
