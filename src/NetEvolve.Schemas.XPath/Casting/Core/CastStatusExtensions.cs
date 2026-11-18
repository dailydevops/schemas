namespace NetEvolve.Schemas.XPath.Casting.Core;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Errors;

/// <summary>Maps the failed outcomes of a cast to the error codes of the specification.</summary>
internal static class CastStatusExtensions
{
    /// <summary>Gets the error code of a failed cast.</summary>
    /// <param name="status">The status of the cast.</param>
    /// <returns>The error code.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException"><paramref name="status"/> is <see cref="CastStatus.Ok"/> or not a defined status.</exception>
    public static XPathErrorCode ToErrorCode(this CastStatus status)
    {
        Argument.ThrowIfLessThan((int)status, (int)CastStatus.NotAllowed);
        Argument.ThrowIfGreaterThan((int)status, (int)CastStatus.IntegerOverflow);

        return status switch
        {
            CastStatus.NotAllowed => XPathErrorCodes.XPTY0004,
            CastStatus.InvalidLexical => XPathErrorCodes.FORG0001,
            CastStatus.InvalidValue => XPathErrorCodes.FOCA0002,
            CastStatus.DecimalOverflow => XPathErrorCodes.FOCA0001,
            _ => XPathErrorCodes.FOCA0003,
        };
    }
}
