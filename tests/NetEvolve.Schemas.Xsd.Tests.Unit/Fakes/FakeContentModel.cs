namespace NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>A sequence of schema name ids: position <c>i</c> accepts the id at index <c>i</c>.</summary>
internal sealed class FakeContentModel : ICompiledContentModel
{
    public FakeContentModel(params int[] sequence) => Sequence = sequence;

    public int[] Sequence { get; }

    public int PositionCount => Sequence.Length;

    public int CounterSlots => 0;
}
