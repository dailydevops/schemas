namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Assertions.Static;

/// <summary>The outcome of evaluating one case: a sequence of items, an error, or the absence of an engine.</summary>
internal sealed class EngineResult
{
    private EngineResult(EngineResultKind kind, IReadOnlyList<object> items, QName errorCode)
    {
        Kind = kind;
        Items = items;
        ErrorCode = errorCode;
    }

    /// <summary>Gets the result for an engine that is not bound.</summary>
    public static EngineResult NotBound { get; } = new(EngineResultKind.NotBound, [], default);

    /// <summary>Gets the kind of result.</summary>
    public EngineResultKind Kind { get; }

    /// <summary>Gets the items of a value result; a <see cref="bool"/> item is an <c>xs:boolean</c>.</summary>
    public IReadOnlyList<object> Items { get; }

    /// <summary>Gets the code of an error result.</summary>
    public QName ErrorCode { get; }

    /// <summary>Creates a value result.</summary>
    /// <param name="items">The items of the sequence.</param>
    /// <returns>The result.</returns>
    public static EngineResult Value(params object[] items) => new(EngineResultKind.Value, items, default);

    /// <summary>Creates an error result.</summary>
    /// <param name="code">The error code.</param>
    /// <returns>The result.</returns>
    public static EngineResult Error(QName code) => new(EngineResultKind.Error, [], code);
}
