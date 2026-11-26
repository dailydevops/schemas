namespace NetEvolve.Schemas.Xsd.Validation.Core;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Text.NameTable;
using NetEvolve.Schemas.Xsd.ContentModels.AllGroups;
using NetEvolve.Schemas.Xsd.ContentModels.AllGroups11;
using NetEvolve.Schemas.Xsd.ContentModels.Automaton;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;
using NetEvolve.Schemas.Xsd.Validation.SchemaLocation;
using NetEvolve.Schemas.Xsd.Validation.Xsi;

/// <summary>
/// The streaming instance validator (design #146 DP-11): an <see cref="IXmlSink"/> that assesses an instance in one pass over
/// the reader events and reports located issues. One instance serves one run; it holds a stack of element frames, each with
/// the state of its content model, and no tree, so memory use does not grow with the size of the document.
/// </summary>
/// <remarks>
/// <para>
/// Behavior decided for this package (see the decisions of #273): the assessment is strict and declaration driven, so an
/// undeclared root is <c>cvc-assess-elt</c> and its subtree is assessed lax; after a child that its content model does not
/// accept, the rest of that content is not matched, so an element reports its content once; a skipped or rejected subtree
/// gets no listener hook; an attribute is located at the start tag of its element, and incomplete content, character data in
/// element-only content, and a fixed value mismatch are located at the start tag of the element, because the reader events
/// for end tags and text carry no position.
/// </para>
/// <para>
/// Limitations: ID, IDREF, and NOTATION (XSD-14, #303), identity
/// constraints (XSD-15, #304), default values of mixed content, the value space comparison of fixed QName and NOTATION values, which needs the namespace context,
/// and forwarding of events to an inner sink (#361). A violation that a listener finds does not mark the element invalid,
/// because the shared reporter has no verdict channel yet. The nesting depth is bounded by the reader
/// (<see cref="XmlReaderLimits.MaxElementDepth"/>, which the assembly of #335 sets from <c>ValidationLimits.MaxDepth</c>), not here.
/// </para>
/// <para>
/// <c>xsi:type</c> and <c>xsi:nil</c> (XSD-55, #308) are decided when the attributes are complete, before the content of the
/// element is set up, so the type that is assessed is the one of <c>xsi:type</c> when it is usable and the declared one
/// otherwise (the actual type definition of Structures 3.3.4). <c>xsi:nil</c> is a property of an element declaration, so an
/// element without one is never nilled and its <c>xsi:nil</c> is not checked; white space is character data, so a nilled
/// element has none; the content of a nilled element is reported once. The other attributes of the schema instance namespace are
/// not assessed here.
/// </para>
/// </remarks>
internal sealed partial class StreamingValidator : IXmlSink
{
    private const string XsiNamespace = "http://www.w3.org/2001/XMLSchema-instance";
    private const int MaxExpectedNames = 8;
    private const int MaxReportedValue = 64;

    private readonly IDerivationCheck? _derivation;
    private readonly SchemaLocationHints? _hints;
    private readonly ISimpleValueAssessor _assessor;
    private ValueSpace? _valueSpace;
    private readonly XsdIssueReporter _reporter;
    private readonly ValidationLimits _limits;
    private readonly string? _documentUri;
    private readonly XsiNamespaceScope _scope;
    private readonly XsiNil _nil;
    private readonly ValidationListener[] _listeners;
    private readonly ValidationListener[] _onStart;
    private readonly ValidationListener[] _onAttribute;
    private readonly ValidationListener[] _onXsiAttribute;
    private readonly ValidationListener[] _onValue;
    private readonly ValidationListener[] _onEnd;

    private ValidationModel _model;
    private NameBinding _binding;
    private XsiTypeResolver _xsi;
    private Stack<CountingRun>?[] _countingPool;
    private Stack<AllGroupRun>?[]? _allPool; // allocated on the first XSD 1.0 all group, so an XSD 1.1 session pays nothing
    private Stack<AllGroup11Run>?[]? _all11Pool; // allocated on the first XSD 1.1 all group, so a session without one pays nothing
    private ElementFrame[] _frames = new ElementFrame[8];
    private int _depth;
    private int _skip;
    private int _ordinal;
    private char[] _text = [];
    private int _textLength;
    private AttributeEntry[] _attributes = new AttributeEntry[8];
    private char[] _attributeChars = [];
    private int _attributeCount;
    private int _attributeLength;
    private bool[] _seen = [];

    /// <summary>Initializes a new instance of the <see cref="StreamingValidator"/> class.</summary>
    /// <param name="model">The immutable model of the schema.</param>
    /// <param name="assessor">The datatype contract.</param>
    /// <param name="reporter">The reporter that collects the issues.</param>
    /// <param name="limits">The limits of the run, handed to the listeners.</param>
    /// <param name="documentUri">The source reported in the locations, <see langword="null"/> for none.</param>
    /// <param name="listeners">The listeners in their fixed order.</param>
    /// <param name="derivation">The derivation check that <c>xsi:type</c> is judged by, <see langword="null"/> for the type derivation check of the schema version.</param>
    /// <param name="readerLimits">The limits of the reader that feeds the validator, <see langword="null"/> for the defaults.</param>
    /// <param name="hints">The schema location hints of the run, <see langword="null"/> to ignore them.</param>
    internal StreamingValidator(
        ValidationModel model,
        ISimpleValueAssessor assessor,
        XsdIssueReporter reporter,
        ValidationLimits limits,
        string? documentUri,
        IReadOnlyList<ValidationListener> listeners,
        IDerivationCheck? derivation = null,
        XmlReaderLimits? readerLimits = null,
        SchemaLocationHints? hints = null
    )
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(assessor);
        ArgumentNullException.ThrowIfNull(reporter);
        ArgumentNullException.ThrowIfNull(limits);
        ArgumentNullException.ThrowIfNull(listeners);

        _model = model;
        _hints = hints;
        _derivation = derivation;
        _assessor = assessor;
        _reporter = reporter;
        _limits = limits;
        _documentUri = documentUri;
        Names = new SpanInterningNameTable((readerLimits ?? XmlReaderLimits.Default).MaxDistinctNames);
        _binding = new NameBinding(model, Names);
        _scope = new XsiNamespaceScope(Names);
        _xsi = new XsiTypeResolver(model.Schema, _scope, derivation);
        _nil = new XsiNil(model.Schema.Version);
        _countingPool = new Stack<CountingRun>?[model.ProgramCount];

        var all = new List<ValidationListener>(listeners.Count);
        for (var i = 0; i < listeners.Count; i++)
        {
            ArgumentNullException.ThrowIfNull(listeners[i]);

            all.Add(listeners[i]);
        }

        _listeners = all.ToArray();
        _onStart = Interested(all, ListenerInterest.StartElement);
        _onAttribute = Interested(all, ListenerInterest.Attribute);
        _onXsiAttribute = Interested(all, ListenerInterest.XsiAttribute);
        _onValue = Interested(all, ListenerInterest.SimpleValue);
        _onEnd = Interested(all, ListenerInterest.EndElement);
    }

    /// <inheritdoc />
    public XmlNameTable Names { get; }

    /// <inheritdoc />
    public void StartDocument(in XmlDeclaration declaration)
    {
        var context = new ValidationRunContext(_limits, Names, _reporter);
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].BeginRun(context);
        }
    }

    /// <inheritdoc />
    public void EndDocument()
    {
        for (var i = 0; i < _listeners.Length; i++)
        {
            _listeners[i].EndRun();
        }
    }

    /// <inheritdoc />
    public void NamespaceDeclaration(XmlSymbol prefix, XmlSymbol uri)
    {
        // A skipped element has no frame that would cut its declarations back when it ends.
        if (_skip == 0)
        {
            _scope.Declare(prefix, uri);
        }
    }

    /// <inheritdoc />
    public void Comment(ReadOnlySpan<char> text) { }

    /// <inheritdoc />
    public void ProcessingInstruction(XmlSymbol target, ReadOnlySpan<char> data) { }

    /// <inheritdoc />
    public void Doctype(in XmlDoctypeInfo doctype) { }

    private static ValidationListener[] Interested(List<ValidationListener> listeners, ListenerInterest hook)
    {
        var interested = new List<ValidationListener>();
        for (var i = 0; i < listeners.Count; i++)
        {
            if ((listeners[i].Interest & hook) != ListenerInterest.None)
            {
                interested.Add(listeners[i]);
            }
        }

        return interested.ToArray();
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct AttributeEntry
    {
        internal AttributeEntry(XmlName name, int start, int length)
        {
            Name = name;
            Start = start;
            Length = length;
        }

        internal XmlName Name { get; }

        internal int Start { get; }

        internal int Length { get; }
    }
}
