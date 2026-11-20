namespace NetEvolve.Schemas.Xsd.Validation.Entities;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Declarations;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>
/// The listener of the ENTITY and ENTITIES checks (XSD-83, #309; design #146 DP-11 and DP-21). The skeleton fixes the
/// constructor seam only; it reports nothing yet.
/// </summary>
internal sealed class EntityListener : ValidationListener
{
    private readonly ISimpleValueAssessor _assessor;
    private readonly XmlReaderLimits _limits;
    private DtdDeclarations? _declarations;

    /// <summary>Initializes a new instance of the <see cref="EntityListener"/> class.</summary>
    /// <param name="assessor">The datatype contract, which tells the accepting member of a union.</param>
    /// <param name="limits">The limits that bound the parse of the document type declaration, <see langword="null"/> for the defaults.</param>
    internal EntityListener(ISimpleValueAssessor assessor, XmlReaderLimits? limits = null)
    {
        Argument.ThrowIfNull(assessor);

        _assessor = assessor;
        _limits = limits ?? XmlReaderLimits.Default;
    }

    /// <inheritdoc />
    public override ListenerInterest Interest =>
        ListenerInterest.StartElement | ListenerInterest.Attribute | ListenerInterest.SimpleValue;

    /// <summary>Receives the document type declaration of the run; it must follow <see cref="ValidationListener.BeginRun"/> and precede the root element.</summary>
    /// <param name="doctype">The declaration, valid only during the call.</param>
    internal void ReceiveDoctype(in XmlDoctypeInfo doctype) =>
        _ = DtdSubsetParser.Parse(in doctype, _limits, out _declarations);

    /// <summary>Gets the declarations of the last document type declaration, <see langword="null"/> when there is none.</summary>
    internal DtdDeclarations? Declarations => _declarations;
}
