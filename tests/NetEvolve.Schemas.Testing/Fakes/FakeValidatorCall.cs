namespace NetEvolve.Schemas.Testing.Fakes;

using System;
using System.Threading;

/// <summary>One recorded call on <see cref="FakeValidator"/>.</summary>
/// <param name="Method">The invoked method name, <c>Validate</c> or <c>ValidateAsync</c>.</param>
/// <param name="ParameterType">The type of the content parameter, for example <c>ReadOnlySpan&lt;Byte&gt;</c>.</param>
/// <param name="Bytes">The received content for byte and stream overloads, otherwise empty.</param>
/// <param name="Chars">The received content for char overloads, otherwise <see langword="null"/>.</param>
/// <param name="SystemId">The received system identifier.</param>
/// <param name="CancellationToken">The received token.</param>
public sealed record FakeValidatorCall(
    string Method,
    string ParameterType,
    ReadOnlyMemory<byte> Bytes,
    string? Chars,
    string? SystemId,
    CancellationToken CancellationToken
);
