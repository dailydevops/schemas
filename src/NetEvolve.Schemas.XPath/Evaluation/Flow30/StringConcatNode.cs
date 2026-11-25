namespace NetEvolve.Schemas.XPath.Evaluation.Flow30;

using System;
using System.Buffers;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Compilation;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The executable form of a chain of string concatenation operators, <c>a || b || c</c> (XPath 3.0, 3.8): every operand is atomized, an
/// empty operand counts as the zero-length string, and an operand with more than one item is the type error XPTY0004. The operands are
/// evaluated in source order and the text is built once, in a scratch buffer from the operand strings, so the only string allocated is the result.
/// </summary>
/// <remarks>The length is checked against the string length limit of the evaluation before any growth.</remarks>
internal sealed class StringConcatNode : ExecutableNode
{
    private const int InitialBuffer = 256;

    private readonly ExecutableNode[] _operands;
    private readonly SourceSpan _location;

    /// <summary>Initializes a new instance of the <see cref="StringConcatNode"/> class.</summary>
    /// <param name="operands">The operands in source order.</param>
    /// <param name="location">The location of the expression, reported with a type error.</param>
    /// <exception cref="System.ArgumentNullException"><paramref name="operands"/> is <see langword="null"/>.</exception>
    public StringConcatNode(ExecutableNode[] operands, SourceSpan location)
    {
        ArgumentNullException.ThrowIfNull(operands);

        _operands = operands;
        _location = location;
    }

    /// <inheritdoc />
    /// <exception cref="XPathException">An operand has more than one item (XPTY0004), or the result exceeds the string length limit.</exception>
    public override XdmSequence Evaluate(XPathEvaluationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var parts = ArrayPool<string?>.Shared.Rent(_operands.Length);
        char[]? rented = null;
        try
        {
            var total = 0L;
            for (var i = 0; i < _operands.Length; i++)
            {
                var value = XPathAtomization.Atomize(_operands[i].Evaluate(context));
                if (value.Count > 1)
                {
                    throw XPathErrors.Exception(XPathErrorCodes.XPTY0004, _location.Offset, _location.Length);
                }

                var text = value.Count == 1 ? TextOf(value[0]) : null;
                parts[i] = text;
                total += text?.Length ?? 0;

                // The check comes before any text is copied, so a result that the limit forbids is never built.
                context.Limits.ThrowIfStringTooLong(total, _location);
            }

            rented = total <= InitialBuffer ? null : ArrayPool<char>.Shared.Rent((int)total);
            var buffer = rented is null ? stackalloc char[InitialBuffer] : new Span<char>(rented);
            var length = 0;
            for (var i = 0; i < _operands.Length; i++)
            {
                var text = parts[i];
                if (text is not null)
                {
                    text.AsSpan().CopyTo(buffer.Slice(length));
                    length += text.Length;
                }
            }

            return XdmSequence.Create(XdmItem.FromString(buffer.Slice(0, length).ToString()));
        }
        finally
        {
            ArrayPool<string?>.Shared.Return(parts, clearArray: true);
            if (rented is not null)
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }
    }

    private static string TextOf(in XdmItem item)
    {
        if (item.TryGetString(out var text))
        {
            return text!;
        }

        var status = CastingCatalog.Default.Table.TryCast(in item, XdmTypeId.XsString, out var result);
        if (status != CastStatus.Ok)
        {
            throw XPathErrors.Exception(status.ToErrorCode());
        }

        _ = result.TryGetString(out text);
        return text!;
    }
}
