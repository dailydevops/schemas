namespace NetEvolve.Schemas.Xml.Reader.Core;

/// <summary>The outcome of <see cref="XmlTokenizer.Next"/>.</summary>
internal enum XmlTokenStatus
{
    /// <summary>A token was produced.</summary>
    Token = 0,

    /// <summary>The input ends inside a construct that is not final; supply more data and call again with the unconsumed tail.</summary>
    NeedMoreData = 1,

    /// <summary>The final input is exhausted between constructs.</summary>
    End = 2,

    /// <summary>The input is not well formed or a limit is exceeded; the tokenizer has a diagnostic.</summary>
    Error = 3,
}
