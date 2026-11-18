namespace NetEvolve.Schemas.Schematron.Seams;

using System;

/// <summary>
/// Binds variable slots before an expression reads them: every consumer (rule contexts, assertion tests, messages, dynamic
/// attributes, variable and function bodies) calls this one helper instead of binding on its own (design #150 EP-5).
/// </summary>
internal interface ISlotBinder
{
    /// <summary>
    /// Makes sure that every slot is bound together with the slots its value reads. A slot is evaluated at most once per run, pattern
    /// pass, or firing, depending on its level; a failed slot stays failed for the same span.
    /// </summary>
    /// <param name="slots">The slots an expression reads.</param>
    /// <returns><see cref="QueryStatus.Problem"/> when a slot is or became failed; the problem was reported once, when it first occurred.</returns>
    QueryStatus EnsureBound(ReadOnlySpan<int> slots);
}
