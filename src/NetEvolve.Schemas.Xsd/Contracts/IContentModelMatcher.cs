namespace NetEvolve.Schemas.Xsd.Contracts;

using System;

/// <summary>Steps through a content model program, one child element at a time. Matchers are stateless and shared.</summary>
internal interface IContentModelMatcher
{
    /// <summary>Tries to accept a child element.</summary>
    /// <param name="model">The program.</param>
    /// <param name="state">The frame state, advanced on success.</param>
    /// <param name="schemaNameId">The bound schema name id of the child.</param>
    /// <param name="matched">The matched particle when accepted.</param>
    /// <returns><see langword="true"/> when the child is accepted.</returns>
    bool TryStep(
        ICompiledContentModel model,
        ref ContentModelState state,
        int schemaNameId,
        out MatchedParticle matched
    );

    /// <summary>Determines whether the content may end in the given state.</summary>
    /// <param name="model">The program.</param>
    /// <param name="state">The frame state.</param>
    /// <returns><see langword="true"/> when the content is complete.</returns>
    bool CanEnd(ICompiledContentModel model, ContentModelState state);

    /// <summary>Collects the schema name ids a child may have next, as many as fit.</summary>
    /// <param name="model">The program.</param>
    /// <param name="state">The frame state.</param>
    /// <param name="destination">The bounded destination.</param>
    /// <returns>The number of ids written.</returns>
    int CollectExpected(ICompiledContentModel model, ContentModelState state, Span<int> destination);
}
