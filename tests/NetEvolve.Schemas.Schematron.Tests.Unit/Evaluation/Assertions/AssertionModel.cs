namespace NetEvolve.Schemas.Schematron.Tests.Unit.Evaluation.Assertions;

using System.Threading;
using NetEvolve.Schemas.Schematron.Model;

/// <summary>Builds the small model elements of the assertion tests; every element gets its own line so problems can be told apart.</summary>
internal static class AssertionModel
{
    private static int _line = 100;

    internal static DocumentLocation Next() => new("rules.sch", Interlocked.Increment(ref _line), 1);

    internal static SchematronText Text(string text) => new(Next(), text);

    internal static SchematronValueOf ValueOf(string select) => new(Next(), select);

    internal static SchematronName Name(string? path = null) => new(Next(), path);

    internal static SchematronEmphasis Emph(params SchematronMessagePart[] parts) => new(Next(), parts);

    internal static SchematronDirection Dir(string? value, params SchematronMessagePart[] parts) =>
        new(Next(), value, parts);

    internal static SchematronSpan Span(string? styleClass, params SchematronMessagePart[] parts) =>
        new(Next(), styleClass, parts);

    internal static SchematronMessage Message(params SchematronMessagePart[] parts) => new(Next(), parts);

    internal static SchematronMessage Message(string text) => Message(Text(text));

    internal static SchematronAssert MakeAssert(
        string test,
        SchematronMessage message,
        string? id = null,
        string? subject = null,
        string[]? diagnostics = null,
        string[]? properties = null
    ) => new(Next(), test, id, [], null, null, subject, diagnostics ?? [], properties ?? [], message);

    internal static SchematronReport MakeReport(
        string test,
        SchematronMessage message,
        string? id = null,
        string? subject = null,
        string[]? diagnostics = null,
        string[]? properties = null
    ) => new(Next(), test, id, [], null, null, subject, diagnostics ?? [], properties ?? [], message);

    internal static SchematronDiagnostic Diagnostic(string id, SchematronMessage message) =>
        new(Next(), id, null, message);

    internal static SchematronProperty Property(string id, SchematronMessage message) =>
        new(Next(), id, null, null, message);

    internal static SchematronRule Rule(string? id = "r1", string? subject = null) =>
        new(Next(), id, "order", null, [], null, subject, false, null, [], [], null);

    internal static SchematronSchema Schema(
        SchematronDiagnostic[]? diagnostics = null,
        SchematronProperty[]? properties = null
    ) =>
        new(
            Next(),
            null,
            [],
            [],
            [],
            [],
            diagnostics is null ? null : new SchematronDiagnostics(Next(), diagnostics),
            properties is null ? null : new SchematronProperties(Next(), properties),
            "s",
            null,
            null,
            SchematronEdition.Iso2016,
            null,
            null,
            [],
            [],
            [],
            []
        );
}
