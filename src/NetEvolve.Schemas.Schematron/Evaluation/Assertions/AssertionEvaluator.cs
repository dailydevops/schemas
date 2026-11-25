namespace NetEvolve.Schemas.Schematron.Evaluation.Assertions;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Evaluation.Annotations;
using NetEvolve.Schemas.Schematron.Results.Mapping;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// Evaluates compiled assertions for the context nodes of one run and delivers their findings (design #150 EP-7). The test is
/// evaluated first; only when the assertion fires are the subject, the <c>role</c>, <c>flag</c>, and <c>severity</c> values (see
/// <see cref="AnnotationEvaluator"/>), the message, the referenced diagnostics, and the referenced properties evaluated, in that
/// order. A part that fails is reported once and leaves out only itself: the finding is always delivered. An assertion that does
/// not fire allocates nothing.
/// </summary>
/// <remarks>
/// Not thread-safe: one evaluator per run, which owns the pooled buffers. Limitations: a message that exceeds the maximum length is
/// cut and reported, and ending the run through the run budget is left to the run loop (design #150 EP-16).
/// </remarks>
internal sealed class AssertionEvaluator : IDisposable
{
    private readonly IQueryRun _run;
    private readonly ISlotBinder _binder;
    private readonly IssueMapper _events;
    private readonly IProblemSink _problems;
    private readonly string? _source;
    private readonly MessageBuilder _builder;
    private readonly NodeSink _subject = new();
    private readonly TextSink _message;
    private readonly TextSink _reference;
    private readonly TextSink _path;
    private readonly AnnotationEvaluator _annotations;
    private string[] _diagnosticIds = [];
    private string[] _diagnosticTexts = [];
    private string[] _diagnosticLanguages = [];
    private string[] _propertyIds = [];
    private string[] _propertyTexts = [];
    private string[] _propertyLanguages = [];
    private IFindingContent?[] _propertyContents = [];

    /// <summary>Initializes a new instance of the <see cref="AssertionEvaluator"/> class.</summary>
    /// <param name="run">The run that evaluates the expressions.</param>
    /// <param name="binder">Binds the variables an expression reads.</param>
    /// <param name="events">Maps a fired assertion to an issue and forwards the finding.</param>
    /// <param name="problems">Receives the problem of a part that fails.</param>
    /// <param name="instanceSystemId">The system identifier of the instance, or <see langword="null"/>.</param>
    /// <param name="maxTextLength">The most characters of one message, diagnostic, or property.</param>
    /// <param name="embedded">Builds the structured content of a property with <c>xsl:copy-of</c>, or <see langword="null"/> when the run has none.</param>
    internal AssertionEvaluator(
        IQueryRun run,
        ISlotBinder binder,
        IssueMapper events,
        IProblemSink problems,
        string? instanceSystemId,
        int maxTextLength,
        IEmbeddedContent? embedded = null
    )
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(binder);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(problems);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxTextLength, 0);

        _run = run;
        _binder = binder;
        _events = events;
        _problems = problems;
        _source = instanceSystemId;
        _builder = new MessageBuilder(run, binder, problems, embedded);
        _message = new TextSink(maxTextLength);
        _reference = new TextSink(maxTextLength);
        _path = new TextSink(int.MaxValue);
        _annotations = new AnnotationEvaluator(run, binder, problems, maxTextLength);
    }

    /// <summary>Gets the resolver of the <c>role</c>, <c>flag</c>, and <c>severity</c> values; the run loop tells it where a pattern pass and a firing begin.</summary>
    internal AnnotationEvaluator Annotations => _annotations;

    /// <summary>Evaluates one assertion for one context node; the finding is delivered when the assertion fires.</summary>
    /// <param name="assertion">The compiled assertion.</param>
    /// <param name="focus">The context node of the fired rule.</param>
    /// <returns>What the evaluation produced.</returns>
    internal AssertionOutcome Evaluate(CompiledAssertion assertion, in QueryFocus focus)
    {
        ArgumentNullException.ThrowIfNull(assertion);

        var test = assertion.TestExpression;
        if (_binder.EnsureBound(test.ReferencedSlots) != QueryStatus.Ok)
        {
            return AssertionOutcome.Problem;
        }

        if (_run.TryEvaluateBoolean(test, in focus, out var value) != QueryStatus.Ok)
        {
            ReportRun();
            return AssertionOutcome.Problem;
        }

        // An assert fires when its test is false, a report when its test is true.
        if (value != assertion.IsReport)
        {
            return AssertionOutcome.Silent;
        }

        Deliver(assertion, in focus);
        return AssertionOutcome.Fired;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _message.Dispose();
        _reference.Dispose();
        _path.Dispose();
        _annotations.Dispose();
    }

    private void Deliver(CompiledAssertion assertion, in QueryFocus focus)
    {
        var node = ResolveSubject(assertion, in focus);
        var annotations = _annotations.Resolve(assertion.Annotations, assertion.Constraint, in focus);

        _message.Clear();
        _ = _builder.Build(assertion.Message, in focus, _message);
        var diagnostics = BuildDiagnostics(assertion.Diagnostics, in focus);
        var properties = BuildReferences(
            assertion.Properties,
            in focus,
            ref _propertyIds,
            ref _propertyTexts,
            ref _propertyContents,
            ref _propertyLanguages
        );

        ReadOnlySpan<char> path = default;
        if (_events.WantsLocationPath)
        {
            _path.Clear();
            NodeLocation.AppendPath(in node, _path);
            path = _path.Written;
        }

        var issue = _events.Map(
            assertion.IsReport,
            assertion.Constraint,
            _message.ToString(),
            NodeLocation.Of(in node, _source)
        );
        var finding = new SchematronFinding(
            assertion.IsReport,
            assertion.Id.AsSpan(),
            assertion.RuleId.AsSpan(),
            assertion.Constraint.AsSpan(),
            assertion.Test.AsSpan(),
            path,
            annotations.Role.AsSpan(),
            annotations.Flags.AsSpan(),
            annotations.Severity.AsSpan(),
            _message.Written,
            new SchematronReferenceList(
                _diagnosticIds.AsSpan(0, diagnostics),
                _diagnosticTexts.AsSpan(0, diagnostics),
                languages: _diagnosticLanguages.AsSpan(0, diagnostics)
            ),
            new SchematronReferenceList(
                _propertyIds.AsSpan(0, properties),
                _propertyTexts.AsSpan(0, properties),
                _propertyContents.AsSpan(0, properties),
                _propertyLanguages.AsSpan(0, properties)
            ),
            issue,
            assertion.Message.Language.AsSpan()
        );
        _events.Finding(in finding);
    }

    // The reported node: the first node of the subject, else the context node; a failing subject never hides the finding.
    private XPathNode ResolveSubject(CompiledAssertion assertion, in QueryFocus focus)
    {
        var subject = assertion.Subject;
        if (subject is null || _binder.EnsureBound(subject.ReferencedSlots) != QueryStatus.Ok)
        {
            return focus.Node;
        }

        _subject.Reset();
        if (_run.TryEvaluate(subject, in focus, _subject) != QueryStatus.Ok)
        {
            ReportRun();
            return focus.Node;
        }

        if (_subject.Count == 0)
        {
            return focus.Node;
        }

        if (!_subject.FirstIsNode)
        {
            var problem = new SchematronProblem(
                SchematronDiagnosticCodes.EvaluationFailed,
                "The subject selected a value that is not a node; the finding is reported at the context node.",
                assertion.Location
            );
            _problems.Add(in problem);
            return focus.Node;
        }

        return _subject.First;
    }

    // Renders each diagnostic as text; one that fails is dropped from both lists, so ids and texts stay aligned.
    private int BuildDiagnostics(CompiledMessage[] references, in QueryFocus focus)
    {
        if (_diagnosticIds.Length < references.Length)
        {
            _diagnosticIds = new string[references.Length];
            _diagnosticTexts = new string[references.Length];
            _diagnosticLanguages = new string[references.Length];
        }

        var count = 0;
        for (var i = 0; i < references.Length; i++)
        {
            _reference.Clear();
            if (_builder.Build(references[i], in focus, _reference) == QueryStatus.Ok)
            {
                _diagnosticIds[count] = references[i].Id;
                _diagnosticTexts[count] = _reference.ToString();
                _diagnosticLanguages[count] = references[i].Language;
                count++;
            }
        }

        return count;
    }

    // Renders each property, with its content when it holds xsl:copy-of; one that fails is dropped from all three lists.
    private int BuildReferences(
        CompiledMessage[] references,
        in QueryFocus focus,
        ref string[] ids,
        ref string[] texts,
        ref IFindingContent?[] contents,
        ref string[] languages
    )
    {
        if (ids.Length < references.Length)
        {
            ids = new string[references.Length];
            texts = new string[references.Length];
            contents = new IFindingContent?[references.Length];
            languages = new string[references.Length];
        }

        var count = 0;
        for (var i = 0; i < references.Length; i++)
        {
            _reference.Clear();
            if (_builder.Build(references[i], in focus, _reference, out var content) == QueryStatus.Ok)
            {
                ids[count] = references[i].Id;
                texts[count] = _reference.ToString();
                contents[count] = content;
                languages[count] = references[i].Language;
                count++;
            }
        }

        return count;
    }

    private void ReportRun()
    {
        var problem = _run.GetProblem();
        _problems.Add(in problem);
    }
}
