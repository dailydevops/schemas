namespace NetEvolve.Schemas.Xsd.Contracts;

using System;

/// <summary>The hooks a listener wants. <c>BeginRun</c> and <c>EndRun</c> are always called.</summary>
[Flags]
internal enum ListenerInterest
{
    None = 0,
    StartElement = 1,
    Attribute = 2,
    SimpleValue = 4,
    EndElement = 8,
    XsiAttribute = 16,
    All = StartElement | Attribute | SimpleValue | EndElement,
}
