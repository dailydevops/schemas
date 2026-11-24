namespace NetEvolve.Schemas.Xml.Dtd.Validation.Identity;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Contracts;
using NetEvolve.Schemas.Xml.Dtd.Model;
using NetEvolve.Schemas.Xml.Text.CharClasses;

/// <summary>
/// The validation step for the identity constraints of XML 1.0, section 3.3.1 (design #122, XML-65): ID (the value is a Name
/// that no other ID attribute of the document has), One ID per Element Type, ID Attribute Default, and IDREF (an IDREF value
/// is a Name and an IDREFS value is Names, each matching the value of an ID attribute).
/// </summary>
/// <remarks>
/// <para>
/// One ID per Element Type and ID Attribute Default are decided from the declarations at <see cref="BeginDocument"/>. An ID
/// value is checked when its start tag arrives, so a duplicate is reported at the second occurrence. A reference to an ID
/// that is already known is resolved at once; a reference to an unknown name may still match a later ID, so it waits and
/// is decided at <see cref="EndDocument"/>, where an unmatched name is reported at the position of its attribute, one issue
/// per occurrence in document order. Because the reader always processes namespaces, a Name must also be free of colons
/// (an NCName, Namespaces in XML 1.0, erratum NE08). A value that is not such a Name is reported at once and is never stored. For IDREFS only the
/// offending token is reported.
/// </para>
/// <para>
/// The IDs live in a <see cref="DtdIdTable"/> and the waiting references in a <see cref="DtdNameStore"/>: names are kept in
/// reused character buffers, so a document allocates only while the buffers grow. The number of IDs is bounded by
/// <see cref="XmlReaderLimits.MaxDistinctNames"/> (<see cref="XmlErrorCodes.XML1111"/>) and the number of waiting references
/// by <see cref="XmlReaderLimits.MaxNodeCount"/> (<see cref="XmlErrorCodes.XML1108"/>), the bounds of design #122, section 9;
/// the composition (XML-68) passes the limits of the reader.
/// </para>
/// <para>
/// Limitations: the value of an attribute arrives normalized (XML 1.0, section 3.3.3), so IDREFS names are separated by
/// exactly one space (production 6) and any other separator, a leading or trailing space, or an empty value is a token that
/// is not a Name. An ID attribute whose declaration carries a default (an ID Attribute Default violation reported once at
/// <see cref="BeginDocument"/>) does not register the defaulted value, so the one mistake does not repeat as a duplicate for
/// every element. Declaration-time reports use <see cref="DtdValidationContext.DocumentTypeLocation"/> until per-declaration
/// locations exist (#608). The total size of the stored names is bounded by the reader's limit on the document size.
/// </para>
/// <para>Not thread-safe; one instance validates one document at a time and may be reused for the next.</para>
/// </remarks>
internal sealed class DtdIdentityStep : IDtdValidationStep
{
    private readonly int _maxIds;
    private readonly int _maxPending;
    private readonly DtdIdTable _ids = new();
    private readonly DtdNameStore _pendingNames = new();
    private Pending[] _pending = new Pending[16];
    private int _pendingCount;
    private DtdValidationContext? _context;

    /// <summary>Initializes a new instance of the <see cref="DtdIdentityStep"/> class.</summary>
    /// <param name="limits">The reader limits that bound the table, <see langword="null"/> for <see cref="XmlReaderLimits.Default"/>.</param>
    internal DtdIdentityStep(XmlReaderLimits? limits = null)
    {
        var effective = limits ?? XmlReaderLimits.Default;
        _maxIds = effective.MaxDistinctNames;
        _maxPending = effective.MaxNodeCount;
    }

    /// <inheritdoc />
    public void BeginDocument(DtdValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        _context = context;
        _ids.Clear();
        _pendingNames.Clear();
        _pendingCount = 0;

        foreach (var pair in context.Declarations.AttributeLists)
        {
            CheckAttributeList(context, pair.Key, pair.Value);
        }
    }

    /// <inheritdoc />
    public void StartElement(in ValidationElement element)
    {
        if (_context is null || _context.IsStopped)
        {
            return;
        }

        var attributes = element.Attributes.Span;
        for (var i = 0; i < attributes.Length && !_context.IsStopped; i++)
        {
            ref readonly var attribute = ref attributes[i];
            switch (attribute.DeclaredType)
            {
                case DtdAttributeType.Id:
                    // A defaulted ID value is the symptom of a declaration that BeginDocument reported already.
                    if (attribute.IsSpecified)
                    {
                        CheckId(in attribute);
                    }

                    break;
                case DtdAttributeType.IdRef:
                    CheckReference(in attribute, attribute.Value.Span);
                    break;
                case DtdAttributeType.IdRefs:
                    CheckReferences(in attribute);
                    break;
                default:
                    break;
            }
        }
    }

    /// <inheritdoc />
    public void EntityReference(in ValidationEntityReference reference) { }

    /// <inheritdoc />
    public void Text(ReadOnlySpan<char> text, XmlTextKind kind, in XmlPosition position)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan((int)kind, (int)XmlTextKind.Text);
        ArgumentOutOfRangeException.ThrowIfGreaterThan((int)kind, (int)XmlTextKind.Reference);
    }

    /// <inheritdoc />
    public void Comment(in XmlPosition position) { }

    /// <inheritdoc />
    public void ProcessingInstruction(in XmlPosition position) { }

    /// <inheritdoc />
    public void EndElement(in ValidationElement element) { }

    /// <inheritdoc />
    public void EndDocument()
    {
        if (_context is null)
        {
            return;
        }

        var count = _pendingCount;
        _pendingCount = 0;
        for (var i = 0; i < count && !_context.IsStopped; i++)
        {
            var pending = _pending[i];
            var token = _pendingNames.Get(pending.Start, pending.Length);
            if (!_ids.Contains(token))
            {
                _context.Report(
                    DtdValidityCodes.XML5012,
                    IssueSeverity.Error,
                    _context.Locate(pending.Position),
                    "no ID attribute has the value '" + token.ToString() + "'"
                );
            }
        }
    }

    // One ID per Element Type and ID Attribute Default: both are properties of the attribute list of an element type.
    private static void CheckAttributeList(
        DtdValidationContext context,
        string element,
        IReadOnlyList<DtdAttributeDefinition> definitions
    )
    {
        string? first = null;
        foreach (var definition in definitions)
        {
            if (definition.Type != DtdAttributeType.Id)
            {
                continue;
            }

            if (definition.DefaultKind is DtdDefaultKind.Fixed or DtdDefaultKind.Default)
            {
                context.Report(
                    DtdValidityCodes.XML5011,
                    IssueSeverity.Error,
                    default,
                    "attribute '" + definition.Name + "' of element type '" + element + "' is declared with a default"
                );
            }

            if (first is null)
            {
                first = definition.Name;
            }
            else
            {
                context.Report(
                    DtdValidityCodes.XML5010,
                    IssueSeverity.Error,
                    default,
                    "element type '" + element + "' has the ID attributes '" + first + "' and '" + definition.Name + "'"
                );
            }
        }
    }

    private void CheckId(in ValidationAttributeInfo attribute)
    {
        var value = attribute.Value.Span;
        if (!IsName(value))
        {
            ReportNotAName(DtdValidityCodes.XML5009, in attribute, value);

            return;
        }

        switch (_ids.TryAdd(value, _maxIds))
        {
            case DtdIdAddResult.Duplicate:
                _context!.Report(
                    DtdValidityCodes.XML5009,
                    IssueSeverity.Error,
                    _context.Locate(attribute.Position),
                    "attribute '"
                        + attribute.Name.ToString()
                        + "' has the value '"
                        + value.ToString()
                        + "' of another ID attribute"
                );
                break;
            case DtdIdAddResult.LimitExceeded:
                _context!.Stop(XmlErrorCodes.XML1111, _context.Locate(attribute.Position));
                break;
            default:
                break;
        }
    }

    // The names of an IDREFS value are separated by one space (production 6); an empty token is not a Name.
    private void CheckReferences(in ValidationAttributeInfo attribute)
    {
        var rest = attribute.Value.Span;
        while (!_context!.IsStopped)
        {
            var space = rest.IndexOf(' ');
            CheckReference(in attribute, space < 0 ? rest : rest.Slice(0, space));
            if (space < 0)
            {
                break;
            }

            rest = rest.Slice(space + 1);
        }
    }

    private void CheckReference(in ValidationAttributeInfo attribute, ReadOnlySpan<char> token)
    {
        if (!IsName(token))
        {
            ReportNotAName(DtdValidityCodes.XML5012, in attribute, token);
        }
        else if (!_ids.Contains(token))
        {
            Defer(in attribute, token);
        }
    }

    // A name that no ID has yet may still match an ID further on; it waits for the end of the document.
    private void Defer(in ValidationAttributeInfo attribute, ReadOnlySpan<char> token)
    {
        if (_pendingCount >= _maxPending)
        {
            _context!.Stop(XmlErrorCodes.XML1108, _context.Locate(attribute.Position));

            return;
        }

        if (_pendingCount == _pending.Length)
        {
            Array.Resize(ref _pending, _pending.Length * 2);
        }

        _pending[_pendingCount++] = new Pending(_pendingNames.Append(token), token.Length, attribute.Position);
    }

    private void ReportNotAName(DtdValidityCode code, in ValidationAttributeInfo attribute, ReadOnlySpan<char> value) =>
        _context!.Report(
            code,
            IssueSeverity.Error,
            _context.Locate(attribute.Position),
            "attribute '"
                + attribute.Name.ToString()
                + "' has the value '"
                + value.ToString()
                + "', which is not a name without a colon"
        );

    // The reader always processes namespaces, so the value must be an NCName: a Name without a colon (Namespaces in XML 1.0,
    // erratum NE08, "Colon in ID attribute name").
    private static bool IsName(ReadOnlySpan<char> value) =>
        value.Length > 0 && XmlCharClass.NameLength(value) == value.Length && value.IndexOf(':') < 0;

    [StructLayout(LayoutKind.Auto)]
    private readonly struct Pending
    {
        internal Pending(int start, int length, XmlPosition position)
        {
            Start = start;
            Length = length;
            Position = position;
        }

        internal int Start { get; }

        internal int Length { get; }

        internal XmlPosition Position { get; }
    }
}
