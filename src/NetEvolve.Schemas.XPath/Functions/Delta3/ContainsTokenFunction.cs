namespace NetEvolve.Schemas.XPath.Functions.Delta3;

using System;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Functions.Sequences;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>fn:contains-token</c> (Functions and Operators 3.1, 5.6.2): whether any string of the input contains the token as one of its
/// whitespace-separated tokens. The token is stripped of leading and trailing whitespace first; a token that is empty then, or that
/// holds whitespace inside, matches nothing. The empty sequence gives false.
/// </summary>
/// <remarks>Limitation: the optional collation argument accepts the codepoint collation only, as <see cref="CollationArgument"/> checks (#267, #55).</remarks>
internal sealed class ContainsTokenFunction : Delta3Function
{
    /// <summary>Initializes a new instance of the <see cref="ContainsTokenFunction"/> class.</summary>
    public ContainsTokenFunction()
        : base("contains-token", 2, 3, [StringItems, OneString, OneString]) { }

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        CollationArgument.Check(invocation.Arguments, 2);

        _ = invocation.Arguments[1][0].TryGetString(out var token);
        var wanted = token.AsSpan();
        var position = 0;
        var found = false;
        if (WhitespaceTokens.Next(wanted, ref position, out var tokenStart, out var tokenLength))
        {
            // The wanted token is the one that remains after stripping; a second one inside it can never equal a token of the input.
            wanted = wanted.Slice(tokenStart, tokenLength);
            var more = WhitespaceTokens.Next(token.AsSpan(), ref position, out _, out _);
            var strings = invocation.Arguments[0];
            for (var i = 0; !more && !found && i < strings.Count; i++)
            {
                _ = strings[i].TryGetString(out var text);
                found = Contains(text.AsSpan(), wanted);
            }
        }

        invocation.Result = XdmSequence.Create(XdmItem.FromBoolean(found));
        return XPathEvaluationStatus.Ok;
    }

    private static bool Contains(ReadOnlySpan<char> text, ReadOnlySpan<char> wanted)
    {
        var position = 0;
        while (WhitespaceTokens.Next(text, ref position, out var start, out var length))
        {
            if (text.Slice(start, length).SequenceEqual(wanted))
            {
                return true;
            }
        }

        return false;
    }
}
