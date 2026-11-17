namespace NetEvolve.Schemas.Tests.Unit.Inputs;

using System;
using System.Threading;
using System.Threading.Tasks;
using NetEvolve.Schemas.Formats;

/// <summary>One public overload of the validator and the kind of content the core must receive.</summary>
/// <param name="Name">The display name.</param>
/// <param name="Sync">Calls the synchronous overload with the shared sample content and the system id <c>s</c>, or <see langword="null"/>.</param>
/// <param name="Async">Calls the asynchronous overload with the shared sample content and the system id <c>s</c>, or <see langword="null"/>.</param>
/// <param name="Kind">The expected kind.</param>
public sealed record OverloadCase(
    string Name,
    Func<RecordingValidator, ValidationResult>? Sync,
    Func<RecordingValidator, CancellationToken, Task<ValidationResult>>? Async,
    DocumentSourceKind Kind
)
{
    public override string ToString() => Name;
}
