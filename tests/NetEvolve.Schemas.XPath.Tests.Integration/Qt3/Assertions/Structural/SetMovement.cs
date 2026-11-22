namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Structural;

using System.Runtime.InteropServices;

/// <summary>The structural cases of one test set by outcome; every one of them was skipped before the structural checker existed.</summary>
/// <param name="TestSet">The test set name.</param>
/// <param name="Passed">The cases that now pass.</param>
/// <param name="Failed">The cases that now fail.</param>
/// <param name="Skipped">The cases that are still skipped.</param>
[StructLayout(LayoutKind.Auto)]
internal readonly record struct SetMovement(string TestSet, int Passed, int Failed, int Skipped)
{
    /// <summary>Gets the number of cases that moved from skipped to passed or failed.</summary>
    public int Moved => Passed + Failed;
}
