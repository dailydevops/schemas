namespace NetEvolve.Schemas.Xsd.Assertions.ComplexTypes;

using System;
using System.Collections.Generic;
using System.Globalization;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Model;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Evaluation.Atomization;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.Validation.Annotations;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>
/// The listener of the assertions of complex types (XSD-23, #318; design #146 DP-10 and DP-14): it buffers the subtree of every element
/// whose type has assertions and evaluates the assertions over it at the end tag of the element (Structures sections 3.13.4.1 and 3.4.4).
/// </summary>
/// <remarks>
/// <para>
/// The subtree is built through the tree builder of the Xml package from the hooks of the validator: the element, its attributes (the
/// defaulted ones included, design #146 DP-19), the value of simple content, and the descendants. It has no document node and no parent, so
/// the element is the context item and the root of the visible tree: <c>..</c>, <c>ancestor::*</c>, and the siblings are empty, and the root
/// step <c>/</c> raises a dynamic error (XPDY0050). An element with assertions inside another one gets a subtree of its own (design #146
/// DP-10, A1), so the work grows with the nesting depth of such types. A type has the assertions of its base types as well
/// (<see cref="ComplexTypeAssertions"/>).
/// </para>
/// <para>
/// A test that is false, a test that raises a dynamic error, and a test that has no effective boolean value are all reported as
/// <c>cvc-assertion</c> (design #146 DP-14: an error makes the element invalid like a false result does; the message names the XPath code), at the start tag of the
/// element, one issue per assertion, so a failing assertion never hides the next. The variable <c>$value</c> holds the typed value of the
/// element when its content is simple and the element is valid so far, and the empty sequence otherwise (Structures 3.13.4.1, clause 2.3).
/// The size of a subtree is bounded by <c>MaxInputSize</c> in characters of attribute values and text, and one character for every start tag of a descendant; a larger subtree is dropped, reported
/// once with the core code NES0001, and not evaluated.
/// </para>
/// <para>
/// Limitations: the hooks carry no namespace declarations, no text of mixed content or between elements, no <c>xsi</c> attributes, and nothing
/// of a lax or skipped descendant, so the subtree misses those nodes until the validator forwards its events to the builder (#948); the in-scope
/// namespaces of a tree node are not materialized. The evaluation carries an annotation source over the subtree (XSD-23, #318): the
/// descendants and the attributes the validator assessed are typed, so a comparison of two integer children is numeric, and a node of a lax or
/// skipped descendant reads as untyped. The root of the tree has the untyped annotation, not <c>xs:anyType</c>, so atomizing it yields its string value. A violation marks the element and its ancestors invalid through the
/// verdict channel of the validator (#605). The subtree is bounded by the input size limit only because the core limits have no node count.
/// </para>
/// <para>Not thread-safe; one instance serves one run at a time and may be reused for the next.</para>
/// </remarks>
internal sealed class AssertionListener : ValidationListener
{
    private static readonly XsdRule LimitRule = new(
        CoreDiagnosticCodes.InputSizeLimitExceeded,
        "Limit: MaxInputSize",
        "The subtree of an element with assertions exceeds MaxInputSize",
        XsdRuleKind.Engine,
        XsdVersionMask.All,
        string.Empty
    );

    private static readonly XsdRule AssertionRule = XsdRuleLookup.Find(XsdRuleCodes.CvcAssertion);

    private readonly ComplexTypeAssertions _assertions;
    private readonly ISimpleValueAssessor _assessor;
    private readonly Stack<XmlTreeBuilder> _spare = new();
    private Subtree[] _open = new Subtree[4];
    private int _count;
    private XmlNameTable? _names;
    private XsdIssueReporter? _reporter;
    private long _limit;
    private XPathEvaluationContext? _context;

    /// <summary>Initializes a new instance of the <see cref="AssertionListener"/> class.</summary>
    /// <param name="assertions">The assertions of the complex types.</param>
    /// <param name="assessor">The datatype contract, which tells the accepting member of a union.</param>
    internal AssertionListener(ComplexTypeAssertions assertions, ISimpleValueAssessor assessor)
    {
        ArgumentNullException.ThrowIfNull(assertions);
        ArgumentNullException.ThrowIfNull(assessor);

        _assertions = assertions;
        _assessor = assessor;
    }

    /// <inheritdoc />
    public override ListenerInterest Interest => _assertions.Count == 0 ? ListenerInterest.None : ListenerInterest.All;

    /// <inheritdoc />
    public override void BeginRun(in ValidationRunContext context)
    {
        Release();
        _names = context.Names;
        _reporter = context.Reporter;
        _limit = context.Limits.MaxInputSize;
    }

    /// <inheritdoc />
    public override void StartElement(in ElementStartInfo info)
    {
        if (_reporter is null)
        {
            return;
        }

        // Every start tag costs one character against the limit, so a subtree of empty elements is bounded as well.
        for (var i = 0; i < _count; i++)
        {
            var subtree = _open[i];
            if (subtree.Builder is not null && Charge(subtree, 1))
            {
                subtree.Builder.StartElement(info.Name, default);
            }
        }

        var assertions = _assertions.Of(info.Type);
        if (assertions.Count > 0)
        {
            Open(info, assertions);
        }
    }

    /// <inheritdoc />
    public override void Attribute(in AttributeInfo info)
    {
        for (var i = 0; i < _count; i++)
        {
            var subtree = _open[i];
            if (subtree.Builder is not null && Charge(subtree, info.Value.Length))
            {
                subtree.Builder.Attribute(info.Name, info.Value);
                subtree.Annotator!.Attribute(info);
            }
        }
    }

    /// <inheritdoc />
    public override void SimpleValue(in SimpleValueInfo info)
    {
        for (var i = 0; i < _count; i++)
        {
            var subtree = _open[i];
            if (subtree.Builder is null || !Charge(subtree, info.Value.Length))
            {
                continue;
            }

            subtree.Builder.Text(info.Value, XmlTextKind.Text);
            subtree.Annotator!.SimpleValue(info);
            if (subtree.Ordinal == info.Ordinal && subtree.HasSimpleContent)
            {
                subtree.ValueText = info.Value.ToString();
            }
        }
    }

    /// <inheritdoc />
    public override void EndElement(in ElementEndInfo info)
    {
        if (_count == 0)
        {
            return;
        }

        // Only the innermost subtree can start at the element that ends; its own root is left untyped (see the remarks).
        var last = _open[_count - 1];
        for (var i = 0; i < _count; i++)
        {
            var subtree = _open[i];
            if (subtree.Builder is null)
            {
                continue;
            }

            subtree.Builder.EndElement(default);
            if (subtree.Ordinal != info.Ordinal)
            {
                subtree.Annotator!.EndElement(info);
            }
        }

        if (last.Ordinal != info.Ordinal)
        {
            return;
        }

        _count--;
        if (last.Builder is { } builder)
        {
            Evaluate(last, builder.Complete(), info);
            _spare.Push(builder);
        }

        last.Clear();
    }

    /// <inheritdoc />
    public override void EndRun() => Release();

    private void Open(in ElementStartInfo info, IReadOnlyList<ComplexTypeAssertion> assertions)
    {
        if (_count == _open.Length)
        {
            Array.Resize(ref _open, _count * 2);
        }

        var subtree = _open[_count] ??= new Subtree();
        _count++;
        subtree.Start(_spare.Count > 0 ? _spare.Pop() : new XmlTreeBuilder(_names, null, false), info, assertions);
        subtree.Builder!.StartElement(info.Name, default);
    }

    // The characters of the attribute values and the text are what MaxInputSize counts for char input; a subtree above it is dropped.
    private bool Charge(Subtree subtree, int characters)
    {
        subtree.Characters += characters;
        if (subtree.Characters <= _limit)
        {
            return true;
        }

        _reporter!.Report(
            LimitRule,
            subtree.Location,
            "The subtree of an element with assertions exceeds MaxInputSize of "
                + _limit.ToString(CultureInfo.InvariantCulture)
                + " characters, so its assertions are not evaluated."
        );
        subtree.Builder!.Dispose();
        subtree.Builder = null;
        return false;
    }

    private void Evaluate(Subtree subtree, XmlTree tree, in ElementEndInfo info)
    {
        var node = XdmItem.FromNode(new XPathNode(tree.Document));
        var value = ValueOf(subtree, info);
        var context = _context ??= new XPathEvaluationContext();
        var source = new AnnotationSource(subtree.Annotator!.Table, tree, _assessor);
        var assertions = subtree.Assertions!;
        for (var i = 0; i < assertions.Count; i++)
        {
            context.Reset();
            context.Variables.Set(AssertionStaticContext.ValueVariable, value);
            context.SetContextItem(node);
            context.Annotations = source;
            Check(assertions[i], subtree.Location, context);
        }
    }

    private void Check(ComplexTypeAssertion assertion, DocumentLocation location, XPathEvaluationContext context)
    {
        var result = assertion.Expression.TryEvaluate(context);
        var diagnostic = result.Diagnostic;
        var failed = result.Status != XPathEvaluationStatus.Ok;
        var satisfied = false;
        if (!failed)
        {
            try
            {
                satisfied = XPathAtomization.EffectiveBooleanValue(result.Value);
            }
            catch (XPathException exception)
            {
                // The test has no effective boolean value (FORG0006), which is a dynamic error like the others.
                diagnostic = exception.Diagnostic;
                failed = true;
            }
        }

        if (failed)
        {
            _reporter!.Report(
                AssertionRule,
                location,
                "The assertion '"
                    + assertion.Test
                    + "' raised the dynamic error "
                    + diagnostic.Code
                    + ": "
                    + diagnostic.Message
            );
        }
        else if (!satisfied)
        {
            _reporter!.Report(
                AssertionRule,
                location,
                "The element does not satisfy the assertion '" + assertion.Test + "'."
            );
        }
    }

    // Clause 2.3 of Structures 3.13.4.1: the typed value of a valid element with simple content, otherwise the empty sequence.
    private XdmSequence ValueOf(Subtree subtree, in ElementEndInfo info)
    {
        if (!info.IsValid || subtree.ValueText is null || subtree.Type is not { } type)
        {
            return XdmSequence.Empty;
        }

        var items = new List<XdmItem>(1);
        TypedValues.Append(TypedValues.SimpleTypeOf(type), subtree.ValueText, _assessor, items);
        return XdmSequence.Create(items.ToArray());
    }

    private void Release()
    {
        for (var i = 0; i < _count; i++)
        {
            _open[i].Builder?.Dispose();
            _open[i].Clear();
        }

        _count = 0;
        while (_spare.Count > 0)
        {
            _spare.Pop().Dispose();
        }

        _context?.Dispose();
        _context = null;
        _reporter = null;
    }

    // One open subtree: the builder that receives the events, where it started, and what the assertions need of the element.
    private sealed class Subtree
    {
        internal XmlTreeBuilder? Builder { get; set; }

        internal int Ordinal { get; private set; }

        internal DocumentLocation Location { get; private set; }

        internal IXsdComponent? Type { get; private set; }

        internal IReadOnlyList<ComplexTypeAssertion>? Assertions { get; private set; }

        internal long Characters { get; set; }

        internal SubtreeAnnotator? Annotator { get; private set; }

        internal bool HasSimpleContent { get; private set; }

        internal string? ValueText { get; set; }

        internal void Start(
            XmlTreeBuilder builder,
            in ElementStartInfo info,
            IReadOnlyList<ComplexTypeAssertion> assertions
        )
        {
            ArgumentNullException.ThrowIfNull(builder);
            ArgumentNullException.ThrowIfNull(assertions);

            Builder = builder;
            Ordinal = info.Ordinal;
            Location = info.Location;
            Type = info.Type;
            Assertions = assertions;
            Characters = 0;
            Annotator = new SubtreeAnnotator(info.Ordinal);
            HasSimpleContent = info.Type is ComplexTypeDefinition { ContentKind: ComplexContentKind.Simple };
            ValueText = null;
        }

        internal void Clear()
        {
            Builder = null;
            Type = null;
            Assertions = null;
            Annotator = null;
            ValueText = null;
        }
    }

    // The annotations of one subtree: the listener of the run, fed through a sink that renumbers the element ordinals so that the root of the
    // subtree is the first element, as the numbering of the annotation source counts them.
    private sealed class SubtreeAnnotator : IAnnotationSink
    {
        private readonly int _offset;
        private readonly AnnotationListener _listener;

        internal SubtreeAnnotator(int rootOrdinal)
        {
            _offset = rootOrdinal - 1;
            _listener = new AnnotationListener(this);
        }

        internal AnnotationTable Table { get; } = new();

        internal void Attribute(in AttributeInfo info) => _listener.Attribute(info);

        internal void SimpleValue(in SimpleValueInfo info) => _listener.SimpleValue(info);

        internal void EndElement(in ElementEndInfo info) => _listener.EndElement(info);

        void IAnnotationSink.Element(in AnnotationRecord record) => Table.Element(Shift(record));

        void IAnnotationSink.Attribute(in AnnotationRecord record) => Table.Attribute(Shift(record));

        private AnnotationRecord Shift(in AnnotationRecord record) =>
            new(
                record.Ordinal - _offset,
                record.AttributeIndex,
                record.Type,
                record.Validity,
                record.IsNilled,
                record.DefaultedValue,
                record.DefaultedName
            );
    }
}
