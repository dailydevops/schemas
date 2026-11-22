namespace NetEvolve.Schemas.Xml.Dtd.External.General;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.External.Loader;
using NetEvolve.Schemas.Xml.Reader.Core;

/// <summary>The text of an external general parsed entity: a thin view of the open frame of the loader for the parser.</summary>
internal sealed class ExternalGeneralEntityText : IXmlEntityText
{
    private readonly ExternalEntityFrame _frame;

    /// <summary>Initializes a new instance of the <see cref="ExternalGeneralEntityText"/> class.</summary>
    /// <param name="frame">The open frame; this instance closes it.</param>
    internal ExternalGeneralEntityText(ExternalEntityFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);

        _frame = frame;
    }

    /// <inheritdoc />
    public string? BaseUri => _frame.BaseUri;

    /// <inheritdoc />
    public XmlErrorCode? Error => _frame.Error;

    /// <inheritdoc />
    public bool TryRead(out ReadOnlyMemory<char> chunk) => _frame.TryRead(out chunk);

    /// <inheritdoc />
    public void Dispose() => _frame.Dispose();
}
