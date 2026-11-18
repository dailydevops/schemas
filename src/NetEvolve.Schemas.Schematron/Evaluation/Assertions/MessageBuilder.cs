namespace NetEvolve.Schemas.Schematron.Evaluation.Assertions;

using NetEvolve.Schemas.Schematron.Diagnostics;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>Builds the text of a compiled message into a <see cref="TextSink"/> without an intermediate string.</summary>
internal sealed class MessageBuilder
{
    internal MessageBuilder(IQueryRun run, ISlotBinder binder, IProblemSink problems) =>
        throw NotImplementedFailure.For(GetType().Name);

    /// <summary>Appends the text of a message; a part that fails is reported once and ends the text, which keeps what was built before it.</summary>
    internal QueryStatus Build(CompiledMessage message, in QueryFocus focus, TextSink into) =>
        throw NotImplementedFailure.For(GetType().Name);
}
