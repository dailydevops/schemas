namespace NetEvolve.Schemas.Xml.Tests.Unit.Dtd.Entities;

using System.Collections.Generic;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>The outcome of one read: status, the alias of the error code, its position, the event dump, and the element positions.</summary>
internal sealed record EntityRun(
    XmlReadStatus Status,
    string? Code,
    XmlPosition? ErrorPosition,
    string Events,
    IReadOnlyList<XmlPosition> ElementPositions
);
