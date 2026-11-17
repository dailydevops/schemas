namespace NetEvolve.Schemas.Schematron.Seams;

using System;

/// <summary>The node kinds a compiled pattern can match; a cheap pre-test.</summary>
[Flags]
internal enum QueryNodeKinds : byte
{
    None = 0,
    Document = 1,
    Element = 2,
    Attribute = 4,
    Text = 8,
    Comment = 16,
    ProcessingInstruction = 32,
    Namespace = 64,
}
