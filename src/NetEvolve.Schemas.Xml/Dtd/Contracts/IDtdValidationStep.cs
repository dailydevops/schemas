namespace NetEvolve.Schemas.Xml.Dtd.Contracts;

using System;
using NetEvolve.Schemas.Xml.Contracts;

/// <summary>
/// One independent validation step over reader events (design #122, D1 and D2). A step shares no state with another step;
/// it reads the declaration model through the context and reports through <see cref="DtdValidationContext.Report"/>.
/// </summary>
/// <remarks>
/// <para>
/// Order: <see cref="Begin"/> fires once after the document type declaration and, when allowed, the external subset are
/// read, and before the first <see cref="StartElement"/>, also for a document without a document type declaration. Events
/// then follow reader order; <see cref="StartElement"/> fires when the start tag is complete and after defaults are applied,
/// <see cref="EntityReference"/> before the replacement events of that reference, comments and processing instructions only
/// inside the root element. Every event reaches the steps in their fixed registration order. <see cref="End"/> fires after
/// the last reader event and not after a fatal well-formedness error. Validity issues never stop the run.
/// </para>
/// <para>
/// Limitation: the composition of the steps into the validating reader is XML-68; the interface is internal like the
/// declaration model it exposes (see the decision on #223).
/// </para>
/// </remarks>
internal interface IDtdValidationStep
{
    /// <summary>Starts a document; the declarations are complete and declaration-time checks run here.</summary>
    /// <param name="context">The context; it stays valid until <see cref="End"/>.</param>
    void Begin(DtdValidationContext context);

    /// <summary>Reports a start tag.</summary>
    /// <param name="element">The element with its attributes.</param>
    void StartElement(in ValidationElement element);

    /// <summary>Reports an entity reference in content, before any replacement events.</summary>
    /// <param name="reference">The reference.</param>
    void EntityReference(in ValidationEntityReference reference);

    /// <summary>Reports text.</summary>
    /// <param name="text">The characters.</param>
    /// <param name="kind">The origin of the characters.</param>
    /// <param name="position">The position of the text.</param>
    void Text(ReadOnlySpan<char> text, XmlTextKind kind, in XmlPosition position);

    /// <summary>Reports a comment inside the root element.</summary>
    /// <param name="position">The position of the comment.</param>
    void Comment(in XmlPosition position);

    /// <summary>Reports a processing instruction inside the root element.</summary>
    /// <param name="position">The position of the processing instruction.</param>
    void ProcessingInstruction(in XmlPosition position);

    /// <summary>Reports an end tag.</summary>
    /// <param name="element">The element, without attributes.</param>
    void EndElement(in ValidationElement element);

    /// <summary>Ends the document; deferred checks such as unresolved IDREF values run here.</summary>
    void End();
}
