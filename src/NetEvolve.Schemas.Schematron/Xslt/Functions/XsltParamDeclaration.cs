namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;

/// <summary>The compiled form of an <c>xsl:param</c> in a function head: it carries the parameter and does nothing when executed, because the call binds the argument.</summary>
internal sealed class XsltParamDeclaration : XsltExecutable
{
    internal XsltParamDeclaration(XsltFunctionParameter parameter)
    {
        Argument.ThrowIfNull(parameter);

        Parameter = parameter;
    }

    /// <summary>Gets the parameter.</summary>
    internal XsltFunctionParameter Parameter { get; }

    /// <inheritdoc />
    internal override QueryStatus TryExecute(XsltFrame frame, ISequenceSink sink)
    {
        Argument.ThrowIfNull(frame);
        Argument.ThrowIfNull(sink);

        return QueryStatus.Ok;
    }
}
