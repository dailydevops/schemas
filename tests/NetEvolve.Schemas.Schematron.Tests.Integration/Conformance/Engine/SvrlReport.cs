namespace NetEvolve.Schemas.Schematron.Tests.Integration.Conformance.Engine;

/// <summary>The SVRL output of one validation, loaded once so that the assertions of a case can be evaluated against it.</summary>
internal sealed class SvrlReport
{
    public bool HasError => throw new NotSupportedException();

    public static SvrlReport Load(ReadOnlyMemory<byte> svrl) =>
        svrl.IsEmpty ? new() : throw new NotSupportedException();

    public bool Holds(string xpath) => throw new NotSupportedException();

    public IReadOnlyList<string> Check(IReadOnlyList<string> assertions) => throw new NotSupportedException();
}
