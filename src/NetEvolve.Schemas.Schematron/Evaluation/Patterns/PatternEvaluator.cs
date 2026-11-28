namespace NetEvolve.Schemas.Schematron.Evaluation.Patterns;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using NetEvolve.Schemas.Schematron.Evaluation.Annotations;
using NetEvolve.Schemas.Schematron.Evaluation.Assertions;
using NetEvolve.Schemas.Schematron.Evaluation.Dispatch;
using NetEvolve.Schemas.Schematron.Scopes;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Xdm.Nodes;

/// <summary>
/// The core control flow of validation (design #150 EP-1 to EP-3 and EP-16): every active pattern makes one pass over the instance
/// in document order; per node the first matching rule of a pattern fires, every matching rule of a group fires. A fired rule
/// evaluates its assertions through the <see cref="AssertionEvaluator"/>. The run ends at the first error that the issue limit
/// drops, and the cancellation token is looked at at the start of every pass and every 256th visited node.
/// </summary>
/// <remarks>
/// <para>
/// Not thread-safe: one evaluator per run. It delivers <c>ActivePattern</c> and <c>FiredRule</c> events and leaves <c>Begin</c> and
/// <c>Finish</c> to the composition, which also builds the instance tree: the returned summary is what <c>Finish</c> reports.
/// </para>
/// <para>
/// Limitations: the events carry the static <c>role</c> and <c>flag</c> only, a dynamic value is resolved on the findings (see <see cref="AnnotationEvaluator"/>).
/// <c>visit-each</c> and <c>documents</c> are SCH-14. Every rule whose node kinds can match is asked, in rule order; bucketing
/// the rules by node name is the <see cref="DispatchIndex"/>: with two or more patterns one traversal collects the candidate nodes of all
/// of them (SCH-72), and every pattern then visits only its candidates in document order, so the fired rules and their order are those of a pass
/// over the whole instance. A pattern whose rule contexts have no name test is a candidate of every node of its kinds. A variable that only a rule
/// reads which no node can match is never bound, so its failure is not reported in that case. The run-wide evaluation budget (design #150
/// EP-16, P-X5) does not exist yet, so only the issue limit and the token end a run. A context that reads a variable is bound
/// before it is matched only when the compiled pattern reports the slot (<see cref="CompiledPattern.ReferencedSlots"/>).
/// </para>
/// </remarks>
internal sealed class PatternEvaluator
{
    // A context that fails on every node reports this many errors per rule and code and pass, and counts the rest.
    private const int MaxReportedContextErrors = 10;

    // The code of a read of a variable that is not bound.
    private const string UnboundVariable = "XPDY0002";

    // The token is checked when the count of visited nodes is a multiple of this power of two.
    private const int CancellationMask = 255;

    private readonly IPatternMatcher _matcher;
    private readonly IQueryRun _run;
    private readonly SlotBinder _binder;
    private readonly AssertionEvaluator _assertions;
    private readonly IEvaluationEvents _events;
    private readonly EvaluationProblems _problems;
    private readonly CancellationToken _cancellationToken;
    private readonly ContextErrorCounter _contextErrors = new();
    private int _visited;
    private int _traversals;

    /// <summary>Initializes a new instance of the <see cref="PatternEvaluator"/> class.</summary>
    /// <param name="matcher">Matches the rule contexts.</param>
    /// <param name="run">The run that evaluates the expressions; the matcher reads its problems from it.</param>
    /// <param name="binder">Binds the variables; the evaluator announces every pattern pass and every firing to it.</param>
    /// <param name="assertions">Evaluates the assertions of a fired rule.</param>
    /// <param name="events">Delivers the events and reports whether the issue limit dropped an error.</param>
    /// <param name="problems">Receives the problems of the evaluation; the same sink as the other parts of the run use.</param>
    /// <param name="cancellationToken">Ends the run with an <see cref="System.OperationCanceledException"/>.</param>
    internal PatternEvaluator(
        IPatternMatcher matcher,
        IQueryRun run,
        SlotBinder binder,
        AssertionEvaluator assertions,
        IEvaluationEvents events,
        EvaluationProblems problems,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(matcher);
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(binder);
        ArgumentNullException.ThrowIfNull(assertions);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(problems);

        _matcher = matcher;
        _run = run;
        _binder = binder;
        _assertions = assertions;
        _events = events;
        _problems = problems;
        _cancellationToken = cancellationToken;
    }

    /// <summary>Gets the number of walks over the whole instance: one per pattern for a single pattern, otherwise one for all patterns.</summary>
    internal int Traversals => _traversals;

    private bool IssueLimitReached => _events.HasDroppedError || _problems.HasDroppedError;

    /// <summary>Evaluates every pattern in order, each in one pass over the instance.</summary>
    /// <param name="sets">The active patterns and groups in document order.</param>
    /// <param name="document">The document node of the instance.</param>
    /// <param name="table">The rule index of <paramref name="sets"/>, or <see langword="null"/> to build it for this call; a caller that evaluates the same patterns repeatedly passes the same table.</param>
    /// <returns>The summary for <c>Finish</c>: truncated with <see cref="SchematronDiagnosticCodes.IssueLimitStop"/> when the issue limit ended the run.</returns>
    /// <exception cref="System.OperationCanceledException">The token was canceled; no event is delivered after that.</exception>
    internal SchematronRunSummary Evaluate(
        IReadOnlyList<CompiledRuleSet> sets,
        in XPathNode document,
        DispatchTable? table = null
    )
    {
        ArgumentNullException.ThrowIfNull(sets);
        ArgumentNullException.ThrowIfNull(document.Tree);

        _cancellationToken.ThrowIfCancellationRequested();
        if (sets.Count < 2)
        {
            return Run(sets, document.Node, null);
        }

        using var index = DispatchIndex.Build(table ?? new DispatchTable(sets), document.Node, _cancellationToken);
        _traversals++;
        return Run(sets, document.Node, index);
    }

    private SchematronRunSummary Run(IReadOnlyList<CompiledRuleSet> sets, XmlNode root, DispatchIndex? index)
    {
        for (var i = 0; i < sets.Count; i++)
        {
            _cancellationToken.ThrowIfCancellationRequested();

            var set = sets[i];
            var pattern = new SchematronPatternEvent(
                set.Id.AsSpan(),
                set.Documents.AsSpan(),
                set.Name.AsSpan(),
                set.Annotations.StaticRole.AsSpan(),
                set.IsGroup
            );
            _events.ActivePattern(in pattern);
            _binder.BeginPattern(set.Scope);
            _assertions.Annotations.BeginPattern(set.Annotations);
            _contextErrors.Clear();

            var stopped = IssueLimitReached || (index is null ? Pass(set, root) : Pass(set, i, index));
            ReportSuppressedContextErrors(set);
            if (stopped || IssueLimitReached)
            {
                return new SchematronRunSummary(true, SchematronDiagnosticCodes.IssueLimitStop);
            }
        }

        return new SchematronRunSummary(false, default);
    }

    // One pass: the document node, then per element the element, its attributes, and its children. Iterative, so the depth
    // of the instance is not limited by the stack.
    private bool Pass(CompiledRuleSet set, XmlNode root)
    {
        _traversals++;
        for (var node = root; !node.IsEmpty; node = Next(node))
        {
            if (Visit(set, node))
            {
                return true;
            }
        }

        return false;
    }

    // The candidates of one pattern from the shared traversal, in document order.
    private bool Pass(CompiledRuleSet set, int position, DispatchIndex index)
    {
        for (var entry = 0; entry < index.Count; entry++)
        {
            if (index.Includes(entry, position) && Visit(set, index.NodeAt(entry)))
            {
                return true;
            }
        }

        return false;
    }

    internal static XmlNode Next(XmlNode node)
    {
        var kind = node.Kind;
        if (kind == XmlNodeKind.Attribute)
        {
            var attribute = node.NextAttribute;
            if (!attribute.IsEmpty)
            {
                return attribute;
            }

            // The attributes of the element are done: continue with its children, else with what follows the element.
            node = node.Parent;
            var first = node.FirstChild;
            if (!first.IsEmpty)
            {
                return first;
            }
        }
        else
        {
            if (kind == XmlNodeKind.Element)
            {
                var attribute = node.FirstAttribute;
                if (!attribute.IsEmpty)
                {
                    return attribute;
                }
            }

            var child = node.FirstChild;
            if (!child.IsEmpty)
            {
                return child;
            }
        }

        while (!node.IsEmpty)
        {
            var sibling = node.NextSibling;
            if (!sibling.IsEmpty)
            {
                return sibling;
            }

            node = node.Parent;
        }

        return default;
    }

    // Returns true when the run must end.
    private bool Visit(CompiledRuleSet set, XmlNode node)
    {
        if ((++_visited & CancellationMask) == 0)
        {
            _cancellationToken.ThrowIfCancellationRequested();
        }

        var kind = KindOf(node.Kind);
        var candidate = new XPathNode(node);
        var rules = set.Rules;
        var named = node.Kind is XmlNodeKind.Element or XmlNodeKind.Attribute;
        var nameKnown = false;
        XPathQName name = default;
        for (var i = 0; i < rules.Length; i++)
        {
            var rule = rules[i];
            if ((rule.Context.NodeKinds & kind) == QueryNodeKinds.None)
            {
                continue;
            }

            // A context whose last steps list their names cannot match a node of another name: skip the XPath match.
            var names = rule.Context.CandidateNames;
            if (named && names.Length > 0)
            {
                if (!nameKnown)
                {
                    name = NameOf(node);
                    nameKnown = true;
                }

                var listed = false;
                foreach (var candidateName in names)
                {
                    if (candidateName.Equals(name))
                    {
                        listed = true;
                        break;
                    }
                }

                if (!listed)
                {
                    continue;
                }
            }
            // A variable is an error only where it is read: the context runs even when a slot failed, and the problem of the slot is
            // reported when the context read it (XPDY0002) or when a later expression reads it. Another failure of the context (not
            // XPDY0002) is reported in addition to the problem of a failed slot that the context names. A failed match is reported here, with the flooding cap.
            var deferred = _binder.EnsureBoundDeferred(rule.Context.ReferencedSlots) != QueryStatus.Ok;
            var status = _matcher.Matches(rule.Context, _run, in candidate, out var matched);
            if (status != QueryStatus.Ok)
            {
                if (deferred)
                {
                    _binder.FlushDeferred(rule.Context.ReferencedSlots);
                }

                if (!deferred || _run.GetProblem().Constraint != UnboundVariable)
                {
                    ReportContextError(i);
                }
            }

            if (status != QueryStatus.Ok)
            {
                // In a pattern no later rule may fire in place of the rule whose verdict is unknown; in a group they are independent.
                if (set.IsGroup)
                {
                    continue;
                }

                return IssueLimitReached;
            }

            if (!matched)
            {
                continue;
            }

            if (Fire(rule, in candidate) || !set.IsGroup)
            {
                return IssueLimitReached;
            }
        }

        return IssueLimitReached;
    }

    // Delivers the event before any variable of the rule is read, then evaluates the assertions in document order.
    private bool Fire(CompiledRule rule, in XPathNode node)
    {
        var ruleEvent = new SchematronRuleEvent(
            rule.Id.AsSpan(),
            rule.ContextText.AsSpan(),
            rule.Annotations.StaticRole.AsSpan(),
            rule.Annotations.StaticFlags.AsSpan(),
            default
        );
        _events.FiredRule(in ruleEvent);

        var focus = new QueryFocus(node, 1, 1);
        _binder.BeginFiring(rule.Scope, in focus);
        _assertions.Annotations.BeginRule(rule.Annotations);

        var assertions = rule.Assertions;
        for (var i = 0; i < assertions.Length && !IssueLimitReached; i++)
        {
            _ = _assertions.Evaluate(assertions[i], in focus);
        }

        return IssueLimitReached;
    }

    private static XPathQName NameOf(XmlNode node)
    {
        var symbols = node.Name;
        var table = node.Tree!.Names;

        return new XPathQName(Text(table, symbols.NamespaceUri), Text(table, symbols.Local));
    }

    private static string Text(XmlNameTable table, XmlSymbol symbol) =>
        table.TryGetString(symbol, out var text) && text is not null ? text : string.Empty;

    private void ReportContextError(int ruleIndex)
    {
        var problem = _run.GetProblem();
        if (_contextErrors.Increment(ruleIndex, problem.Constraint) <= MaxReportedContextErrors)
        {
            _problems.Add(in problem);
        }
    }

    // One issue per rule and code names how many errors of the pass were only counted.
    private void ReportSuppressedContextErrors(CompiledRuleSet set)
    {
        for (var i = 0; i < _contextErrors.Count; i++)
        {
            var total = _contextErrors.At(i, out var rule, out var code);
            if (total > MaxReportedContextErrors)
            {
                var problem = new SchematronProblem(
                    SchematronDiagnosticCodes.EvaluationFailed,
                    string.Concat(
                        (total - MaxReportedContextErrors).ToString(CultureInfo.InvariantCulture),
                        " further evaluation errors in the context of the rule '",
                        set.Rules[rule].ContextText,
                        "' were counted and not reported."
                    ),
                    set.Rules[rule].Scope.Rule.Location,
                    code
                );
                _problems.Add(in problem);
            }
        }
    }

    internal static QueryNodeKinds KindOf(XmlNodeKind kind)
    {
        Debug.Assert(kind != XmlNodeKind.Namespace, "A namespace node is never visited.");

        return kind switch
        {
            XmlNodeKind.Document => QueryNodeKinds.Document,
            XmlNodeKind.Element => QueryNodeKinds.Element,
            XmlNodeKind.Attribute => QueryNodeKinds.Attribute,
            XmlNodeKind.Text => QueryNodeKinds.Text,
            XmlNodeKind.Comment => QueryNodeKinds.Comment,
            XmlNodeKind.ProcessingInstruction => QueryNodeKinds.ProcessingInstruction,
            _ => QueryNodeKinds.None,
        };
    }

    /// <summary>
    /// Counts the context errors of one pattern pass per rule and error code, so that a context that fails on every node reports its
    /// first errors and is then only counted (design #150 EP-2, error flooding). Allocates only when an error occurs.
    /// </summary>
    private sealed class ContextErrorCounter
    {
        private readonly List<Entry> _entries = [];

        /// <summary>Gets the number of distinct (rule, code) pairs counted since the last <see cref="Clear"/>.</summary>
        public int Count => _entries.Count;

        /// <summary>Counts one error.</summary>
        /// <param name="rule">The index of the rule in its pattern.</param>
        /// <param name="code">The XPath error code, or <see langword="null"/>.</param>
        /// <returns>The number of errors of this rule and code in the pass, this one included.</returns>
        public int Increment(int rule, string? code)
        {
            for (var i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (entry.Rule == rule && string.Equals(entry.Code, code, StringComparison.Ordinal))
                {
                    _entries[i] = new Entry(rule, code, entry.Total + 1);
                    return entry.Total + 1;
                }
            }

            _entries.Add(new Entry(rule, code, 1));
            return 1;
        }

        /// <summary>Gets one counted pair.</summary>
        /// <param name="index">The index, below <see cref="Count"/>.</param>
        /// <param name="rule">The index of the rule in its pattern.</param>
        /// <param name="code">The XPath error code, or <see langword="null"/>.</param>
        /// <returns>The number of errors counted.</returns>
        public int At(int index, out int rule, out string? code)
        {
            var entry = _entries[index];
            rule = entry.Rule;
            code = entry.Code;
            return entry.Total;
        }

        /// <summary>Forgets every count; called at the start of a pattern pass.</summary>
        public void Clear() => _entries.Clear();

        private readonly struct Entry
        {
            internal Entry(int rule, string? code, int total)
            {
                Rule = rule;
                Code = code;
                Total = total;
            }

            internal int Rule { get; }

            internal string? Code { get; }

            internal int Total { get; }
        }
    }
}
