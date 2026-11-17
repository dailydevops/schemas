namespace NetEvolve.Schemas.Xsd.Tests.Unit.Fakes;

using System;
using NetEvolve.Schemas.Xsd.Contracts;

/// <summary>A matcher over <see cref="FakeContentModel"/>: the next child must carry the id of the next position.</summary>
internal sealed class FakeContentModelMatcher : IContentModelMatcher
{
    public ContentModelState Start => new() { Position = -1 };

    public bool TryStep(
        ICompiledContentModel model,
        ref ContentModelState state,
        int schemaNameId,
        out MatchedParticle matched
    )
    {
        var sequence = ((FakeContentModel)model).Sequence;
        var next = state.Position + 1;
        if (next < sequence.Length && sequence[next] == schemaNameId)
        {
            state.Position = next;
            matched = new MatchedParticle(next, isWildcard: false);
            return true;
        }

        matched = default;
        return false;
    }

    public bool CanEnd(ICompiledContentModel model, ContentModelState state) =>
        state.Position == model.PositionCount - 1;

    public int CollectExpected(ICompiledContentModel model, ContentModelState state, Span<int> destination)
    {
        var sequence = ((FakeContentModel)model).Sequence;
        var next = state.Position + 1;
        if (next >= sequence.Length || destination.IsEmpty)
        {
            return 0;
        }

        destination[0] = sequence[next];
        return 1;
    }
}
