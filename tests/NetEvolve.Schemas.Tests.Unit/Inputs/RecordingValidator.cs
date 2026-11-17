namespace NetEvolve.Schemas.Tests.Unit.Inputs;

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading;
using NetEvolve.Schemas.Formats;
using NetEvolve.Schemas.Resources;

/// <summary>A format core that records what it receives.</summary>
public sealed class RecordingValidator : SchemaValidatorBase
{
    private readonly List<Array> _segments = [];

    public RecordingValidator(ValidationLimits? limits = null)
        : base(limits ?? ValidationLimits.Default, DenyAllResourceResolver.Instance) { }

    public int Calls { get; private set; }

    public DocumentSourceKind Kind { get; private set; }

    public bool IsSingleSegment { get; private set; }

    public long Length { get; private set; }

    public string? SystemId { get; private set; }

    public CancellationToken Token { get; private set; }

    /// <summary>Gets the received bytes (Kind Bytes) or characters (Kind Chars) as one flat array.</summary>
    public Array? Content { get; private set; }

    /// <summary>Gets the segments of a multi-segment input, in order.</summary>
    public IReadOnlyList<Array> Segments => _segments;

    public Exception? Throw { get; set; }

    public Action? OnCore { get; set; }

    protected override ValidationResult ValidateCore(in DocumentSource source, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls++;
        Kind = source.Kind;
        IsSingleSegment = source.IsSingleSegment;
        Length = source.Length;
        SystemId = source.SystemId;
        Token = cancellationToken;

        OnCore?.Invoke();

        if (Throw is not null)
        {
            throw Throw;
        }

        if (source.Kind == DocumentSourceKind.Bytes)
        {
            RecordBytes(in source);
        }
        else
        {
            RecordChars(in source);
        }

        return ValidationResult.Valid;
    }

    private void RecordBytes(in DocumentSource source)
    {
        if (source.IsSingleSegment)
        {
            Content = source.Bytes.ToArray();
            return;
        }

        Content = source.ByteSequence.ToArray();
        foreach (var segment in source.ByteSequence)
        {
            _segments.Add(segment.ToArray());
        }
    }

    private void RecordChars(in DocumentSource source)
    {
        if (source.IsSingleSegment)
        {
            Content = source.Chars.ToArray();
            return;
        }

        Content = source.CharSequence.ToArray();
        foreach (var segment in source.CharSequence)
        {
            _segments.Add(segment.ToArray());
        }
    }
}
