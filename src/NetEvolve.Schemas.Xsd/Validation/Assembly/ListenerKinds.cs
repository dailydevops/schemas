namespace NetEvolve.Schemas.Xsd.Validation.Assembly;

using System;

/// <summary>The optional listeners of a validator.</summary>
[Flags]
internal enum ListenerKinds
{
    None = 0,
    Ids = 1,
    Entities = 2,
}
