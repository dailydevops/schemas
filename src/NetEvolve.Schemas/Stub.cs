namespace NetEvolve.Schemas;

using System;

/// <summary>Marks a contract member whose behavior belongs to a later work package.</summary>
internal static class Stub
{
    internal static NotImplementedException For(string workPackage) =>
        new($"Not implemented until work package {workPackage} lands.");
}
