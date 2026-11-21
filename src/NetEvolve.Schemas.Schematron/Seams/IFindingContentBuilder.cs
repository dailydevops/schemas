namespace NetEvolve.Schemas.Schematron.Seams;

using System;

/// <summary>
/// Receives the items and the text of one property in order and completes them into an <see cref="IFindingContent"/>. A builder that was
/// not completed releases what it holds when it is disposed.
/// </summary>
internal interface IFindingContentBuilder : ISequenceSink, IDisposable
{
    /// <summary>Ends the content.</summary>
    /// <param name="content">The snapshot when the result is <see langword="true"/>.</param>
    /// <param name="problem">The problem when the result is <see langword="false"/>.</param>
    /// <returns><see langword="false"/> when a push was not allowed, such as an attribute after a child node.</returns>
    bool TryComplete(out IFindingContent content, out SchematronProblem problem);
}
