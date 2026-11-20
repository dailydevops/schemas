namespace NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd.ComplexTypes.Definitions;
using NetEvolve.Schemas.Xsd.Components;
using NetEvolve.Schemas.Xsd.Contracts;
using NetEvolve.Schemas.Xsd.Diagnostics;
using NetEvolve.Schemas.Xsd.IdentityConstraints.Definitions;
using NetEvolve.Schemas.Xsd.SimpleTypes.Definitions;
using NetEvolve.Schemas.Xsd.Validation.Core;

/// <summary>
/// The listener of the identity constraints (XSD-15, #304; design #146 DP-13): <c>unique</c>, <c>key</c>, and <c>keyref</c> are
/// evaluated while the instance streams by (Structures sections 3.11.4 and 3.11.6).
/// </summary>
/// <remarks>
/// <para>
/// Every element whose declaration has identity constraints opens one scope per constraint. The selector of a scope is matched
/// against the names of the open elements at each start tag; a selected element becomes a target, and the field paths are
/// matched at the start tags and attributes below it. A target is complete at its end tag: its field values form a key-sequence
/// that goes into the key table of the scope (<c>unique</c> and <c>key</c>; a duplicate is reported at once, at the second
/// element) or waits for the end of the scope (<c>keyref</c>). Every scope has its own table, so nested scopes of the same
/// constraint never meet. At the end of a scope its key tables are offered to the scopes above it that hold a <c>keyref</c> to
/// that constraint without declaring the constraint themselves, so a keyref finds the key of a descendant element, with the
/// tables of all such descendants merged, except for key-sequences that two descendants hold (Structures 3.11.5). The values of the key-sequences compare in the value space (<see cref="ValueSpace"/>).
/// </para>
/// <para>
/// Decisions of #304: a field that selects a second node is reported once, at that node, and the selected element takes no
/// further part; an element field of a type that has no simple content is reported at that element; a field value that the
/// datatype check has rejected, and a field element whose content was rejected, leave the selected element out without an issue of
/// their own, because the validator has reported them; a keyref value that no key of the scope has is reported when the scope
/// ends, at the selected element, in document order; the constraints of an element declaration come from the lookup that the
/// listener is created with, because the component model does not yet hold them (the wiring is XSD-78).
/// </para>
/// <para>
/// Limitations: one object per selected element and one per scope, one key-sequence per qualified element, so memory grows with
/// the number of matches and is otherwise independent of the size of the document. An element that the validator does not hand
/// to the listeners (a skipped or rejected subtree) takes no part, and a violation found here does not mark the element invalid
/// (#605). A nilled element is not told apart (<c>xsi:nil</c> is XSD-55, #308), so its value counts. Key tables over the date and
/// time types, durations, and binary values compare every pair (see <see cref="ValueSpace"/>).
/// </para>
/// <para>Not thread-safe; one instance serves one run at a time and may be reused for the next.</para>
/// </remarks>
internal sealed class IdentityConstraintListener : ValidationListener
{
    private const int MaxReportedValue = 64;

    private static readonly XsdRule IdentityRule = XsdRuleLookup.Find(XsdRuleCodes.CvcIdentityConstraint);

    private readonly ValueSpace _values;
    private readonly Func<IXsdComponent, IReadOnlyList<IdentityConstraintDefinition>?> _constraintsOf;
    private readonly Dictionary<IdentityConstraintDefinition, Plan> _plans = [];
    private readonly List<Scope> _scopes = [];
    private readonly List<Target> _targets = [];
    private ElementName[] _stack = new ElementName[16];
    private Capture[] _captures = new Capture[16];
    private int _captureCount;
    private int _depth;
    private XmlNameTable? _names;
    private XsdIssueReporter? _reporter;

    /// <summary>Initializes a new instance of the <see cref="IdentityConstraintListener"/> class.</summary>
    /// <param name="assessor">The datatype contract, which tells the accepting member of a union.</param>
    /// <param name="version">The XSD version, which decides how values of the value space compare.</param>
    /// <param name="constraintsOf">Gives the identity constraints of an element declaration, <see langword="null"/> for none.</param>
    internal IdentityConstraintListener(
        ISimpleValueAssessor assessor,
        XsdVersion version,
        Func<IXsdComponent, IReadOnlyList<IdentityConstraintDefinition>?> constraintsOf
    )
    {
        Argument.ThrowIfNull(constraintsOf);

        _values = new ValueSpace(assessor, version);
        _constraintsOf = constraintsOf;
    }

    /// <inheritdoc />
    public override ListenerInterest Interest => ListenerInterest.All;

    /// <inheritdoc />
    public override void BeginRun(in ValidationRunContext context)
    {
        _names = context.Names;
        _reporter = context.Reporter;
        Reset();
    }

    /// <inheritdoc />
    public override void StartElement(in ElementStartInfo info)
    {
        if (_reporter is null)
        {
            return;
        }

        if (_depth == _stack.Length)
        {
            Array.Resize(ref _stack, _depth * 2);
        }

        _stack[_depth++] = new ElementName(
            _names!.GetString(info.Name.NamespaceUri),
            _names.GetString(info.Name.Local)
        );
        OpenScopes(info.Declaration);
        SelectTargets(info.Location);
        for (var i = 0; i < _targets.Count; i++)
        {
            MatchElementFields(_targets[i], info);
        }
    }

    /// <inheritdoc />
    public override void Attribute(in AttributeInfo info)
    {
        if (_reporter is null || _targets.Count == 0)
        {
            return;
        }

        var namespaceName = _names!.GetString(info.Name.NamespaceUri);
        var localName = _names.GetString(info.Name.Local);
        for (var i = 0; i < _targets.Count; i++)
        {
            var target = _targets[i];
            var fields = target.Scope.Plan.Fields;
            for (var field = 0; field < fields.Length && !target.Failed; field++)
            {
                if (fields[field].MatchesAttribute(_stack, target.Depth, _depth, namespaceName, localName))
                {
                    CaptureAttribute(target, field, info);
                }
            }
        }
    }

    /// <inheritdoc />
    public override void SimpleValue(in SimpleValueInfo info)
    {
        // The element of a simple value is the innermost open one, and its captures are the last ones.
        for (var i = _captureCount - 1; i >= 0 && _captures[i].Ordinal == info.Ordinal; i--)
        {
            var target = _captures[i].Target;
            if (target.Failed)
            {
                continue;
            }

            if (info.IsValid && info.Type is not null)
            {
                target.Fields[_captures[i].Field].Set(info.Value.ToString(), info.Type);
                _captures[i].Delivered = true;
            }
            else
            {
                target.Failed = true;
            }
        }
    }

    /// <inheritdoc />
    public override void EndElement(in ElementEndInfo info)
    {
        if (_reporter is null || _depth == 0)
        {
            return;
        }

        // A field element whose value never came had its content rejected; the validator has reported that.
        while (_captureCount > 0 && _captures[_captureCount - 1].Ordinal == info.Ordinal)
        {
            ref var capture = ref _captures[--_captureCount];
            if (!capture.Delivered)
            {
                capture.Target.Failed = true;
            }
        }

        while (_targets.Count > 0 && _targets[_targets.Count - 1].Depth == _depth)
        {
            Complete(_targets[_targets.Count - 1]);
            _targets.RemoveAt(_targets.Count - 1);
        }

        CloseScopes();
        _depth--;
    }

    /// <inheritdoc />
    public override void EndRun() => Reset();

    private void Reset()
    {
        _scopes.Clear();
        _targets.Clear();
        _captureCount = 0;
        _depth = 0;
    }

    private void OpenScopes(IXsdComponent? declaration)
    {
        var constraints = declaration is null ? null : _constraintsOf(declaration);
        if (constraints is null)
        {
            return;
        }

        for (var i = 0; i < constraints.Count; i++)
        {
            if (!_plans.TryGetValue(constraints[i], out var plan))
            {
                plan = new Plan(constraints[i]);
                _plans.Add(constraints[i], plan);
            }

            _scopes.Add(new Scope(constraints[i], plan, _depth));
        }
    }

    private void SelectTargets(DocumentLocation location)
    {
        for (var i = 0; i < _scopes.Count; i++)
        {
            var scope = _scopes[i];
            if (scope.Plan.Selector.MatchesElement(_stack, scope.Depth, _depth))
            {
                _targets.Add(new Target(scope, _depth, location));
            }
        }
    }

    private void MatchElementFields(Target target, in ElementStartInfo info)
    {
        var fields = target.Scope.Plan.Fields;
        for (var field = 0; field < fields.Length && !target.Failed; field++)
        {
            if (fields[field].MatchesElement(_stack, target.Depth, _depth))
            {
                CaptureElement(target, field, info);
            }
        }
    }

    private void CaptureElement(Target target, int field, in ElementStartInfo info)
    {
        if (!target.Fields[field].Count())
        {
            ReportMultiple(target, field, info.Location);
            return;
        }

        if (!HasSimpleContent(info.Type))
        {
            Report(
                info.Location,
                "The field '"
                    + target.Scope.Definition.Fields[field]
                    + "' of the identity constraint '"
                    + target.Scope.Definition.Name.LocalName
                    + "' selects an element whose type has no simple content.",
                "cvc-identity-constraint.3"
            );
            target.Failed = true;
            return;
        }

        if (_captureCount == _captures.Length)
        {
            Array.Resize(ref _captures, _captureCount * 2);
        }

        _captures[_captureCount++] = new Capture(target, field, info.Ordinal);
    }

    private void CaptureAttribute(Target target, int field, in AttributeInfo info)
    {
        if (!target.Fields[field].Count())
        {
            ReportMultiple(target, field, info.Location);
        }
        else if (info.IsValid && info.Type is not null)
        {
            target.Fields[field].Set(info.Value.ToString(), info.Type);
        }
        else
        {
            target.Failed = true;
        }
    }

    private void ReportMultiple(Target target, int field, DocumentLocation location)
    {
        Report(
            location,
            "The field '"
                + target.Scope.Definition.Fields[field]
                + "' of the identity constraint '"
                + target.Scope.Definition.Name.LocalName
                + "' selects more than one node.",
            "cvc-identity-constraint.3"
        );
        target.Failed = true;
    }

    // A target is complete at its end tag, when every field has seen all the nodes below it.
    private void Complete(Target target)
    {
        if (target.Failed)
        {
            return;
        }

        var definition = target.Scope.Definition;
        var fields = target.Fields;
        for (var i = 0; i < fields.Length; i++)
        {
            if (fields[i].IsEmpty)
            {
                if (definition.ConstraintKind == IdentityConstraintKind.Key)
                {
                    Report(
                        target.Location,
                        "The key '"
                            + definition.Name.LocalName
                            + "' has no value for the field '"
                            + definition.Fields[i]
                            + "'.",
                        "cvc-identity-constraint.4.2.1"
                    );
                }

                return;
            }
        }

        var sequence = Sequence(fields);
        if (definition.ConstraintKind == IdentityConstraintKind.Keyref)
        {
            (target.Scope.Pending ??= []).Add(new PendingReference(sequence, target.Location));
            return;
        }

        var table = target.Scope.Table ??= new KeyTable();
        if (!table.Contains(sequence, _values))
        {
            table.Add(sequence);
            return;
        }

        var isKey = definition.ConstraintKind == IdentityConstraintKind.Key;
        Report(
            target.Location,
            "The value "
                + Display(sequence)
                + " is already the "
                + (isKey ? "key" : "unique value")
                + " of another element for the identity constraint '"
                + definition.Name.LocalName
                + "'.",
            isKey ? "cvc-identity-constraint.4.2.2" : "cvc-identity-constraint.4.1"
        );
    }

    private KeySequence Sequence(FieldValue[] fields)
    {
        var lexicals = new string[fields.Length];
        var types = new IXsdComponent[fields.Length];
        var hash = 17;
        for (var i = 0; i < fields.Length; i++)
        {
            lexicals[i] = fields[i].Lexical!;
            types[i] = fields[i].Type!;
            hash = unchecked((hash * 31) + _values.Hash(types[i], lexicals[i]));
        }

        return new KeySequence(lexicals, types, hash);
    }

    // The scopes of the ending element are the last ones. Keyrefs resolve first, because a key of the same element is complete
    // by now; then the key tables go up to the scopes that wait for them.
    private void CloseScopes()
    {
        var first = _scopes.Count;
        while (first > 0 && _scopes[first - 1].Depth == _depth)
        {
            first--;
        }

        for (var i = first; i < _scopes.Count; i++)
        {
            if (_scopes[i].Definition.ConstraintKind == IdentityConstraintKind.Keyref)
            {
                ResolveKeyref(_scopes[i], first);
            }
        }

        for (var i = first; i < _scopes.Count; i++)
        {
            Publish(_scopes[i], first);
        }

        _scopes.RemoveRange(first, _scopes.Count - first);
    }

    private void ResolveKeyref(Scope scope, int first)
    {
        var key = scope.Definition.ReferencedConstraint;
        if (key is null || scope.Pending is null)
        {
            return;
        }

        // A key of the same element hides the tables of its descendants.
        KeyTable? own = null;
        var declared = false;
        for (var i = first; i < _scopes.Count; i++)
        {
            if (ReferenceEquals(_scopes[i].Definition, key))
            {
                own = _scopes[i].Table;
                declared = true;
            }
        }

        foreach (var pending in scope.Pending)
        {
            var found = declared
                ? own is not null && own.Contains(pending.Sequence, _values)
                : IsBubbled(scope, pending);
            if (!found)
            {
                Report(
                    pending.Location,
                    "The key reference '"
                        + scope.Definition.Name.LocalName
                        + "' has the value "
                        + Display(pending.Sequence)
                        + ", which no key or unique value of '"
                        + key.Name.LocalName
                        + "' has.",
                    "cvc-identity-constraint.4.3"
                );
            }
        }
    }

    private bool IsBubbled(Scope scope, PendingReference pending)
    {
        if (scope.Bubbled is null)
        {
            return false;
        }

        // A key-sequence that two descendant tables hold has two nodes; such entries are not included (Structures 3.11.5).
        var holders = 0;
        foreach (var table in scope.Bubbled)
        {
            if (table.Contains(pending.Sequence, _values))
            {
                holders++;
            }
        }

        return holders == 1;
    }

    // Offers the table to the scopes above that refer to its constraint, down to the nearest scope that declares it itself.
    private void Publish(Scope scope, int first)
    {
        if (scope.Table is null)
        {
            return;
        }

        var blocker = 0;
        for (var i = first - 1; i >= 0; i--)
        {
            if (ReferenceEquals(_scopes[i].Definition, scope.Definition))
            {
                blocker = _scopes[i].Depth;
                break;
            }
        }

        for (var i = first - 1; i >= 0 && _scopes[i].Depth > blocker; i--)
        {
            var above = _scopes[i];
            if (
                above.Definition.ConstraintKind == IdentityConstraintKind.Keyref
                && ReferenceEquals(above.Definition.ReferencedConstraint, scope.Definition)
            )
            {
                (above.Bubbled ??= []).Add(scope.Table);
            }
        }
    }

    private void Report(DocumentLocation location, string message, string clause) =>
        _reporter!.Report(IdentityRule, location, message, clause);

    private static bool HasSimpleContent(IXsdComponent? type) =>
        type
            is SimpleTypeDefinition
                or XsdBuiltInType { Kind: XsdComponentKind.SimpleType }
                or ComplexTypeDefinition { ContentKind: ComplexContentKind.Simple };

    private static string Display(KeySequence sequence)
    {
        var text = string.Empty;
        for (var i = 0; i < sequence.Lexicals.Length; i++)
        {
            var value = sequence.Lexicals[i];
            text +=
                (i == 0 ? "'" : ", '")
                + (value.Length <= MaxReportedValue ? value : value.AsSpan(0, MaxReportedValue).ToString() + "...")
                + "'";
        }

        return "[" + text + "]";
    }

    // The compiled paths of a constraint, shared by all its scopes.
    private sealed class Plan
    {
        internal Plan(IdentityConstraintDefinition definition)
        {
            Selector = ConstraintPath.Compile(definition.Selector, definition.SelectorBindings);
            Fields = new ConstraintPath[definition.Fields.Count];
            for (var i = 0; i < Fields.Length; i++)
            {
                Fields[i] = ConstraintPath.Compile(definition.Fields[i], definition.FieldBindings[i]);
            }
        }

        internal ConstraintPath Selector { get; }

        internal ConstraintPath[] Fields { get; }
    }

    // One constraint in the scope of one element.
    private sealed class Scope
    {
        internal Scope(IdentityConstraintDefinition definition, Plan plan, int depth)
        {
            Definition = definition;
            Plan = plan;
            Depth = depth;
        }

        internal IdentityConstraintDefinition Definition { get; }

        internal Plan Plan { get; }

        internal int Depth { get; }

        // For a unique or key.
        internal KeyTable? Table { get; set; }

        // For a keyref: the references that wait for the end of the scope, and the key tables of the descendants.
        internal List<PendingReference>? Pending { get; set; }

        internal List<KeyTable>? Bubbled { get; set; }
    }

    // A selected element with the values of its fields.
    private sealed class Target
    {
        internal Target(Scope scope, int depth, DocumentLocation location)
        {
            Scope = scope;
            Depth = depth;
            Location = location;
            Fields = new FieldValue[scope.Plan.Fields.Length];
        }

        internal Scope Scope { get; }

        internal int Depth { get; }

        internal DocumentLocation Location { get; }

        internal FieldValue[] Fields { get; }

        // An error was found, or a value is not usable: the element forms no key-sequence.
        internal bool Failed { get; set; }
    }

    // What one field has found so far: the number of nodes and the value of the first.
    [StructLayout(LayoutKind.Auto)]
    private struct FieldValue
    {
        private int _nodes;

        internal string? Lexical { get; private set; }

        internal IXsdComponent? Type { get; private set; }

        internal readonly bool IsEmpty => _nodes == 0;

        // Counts a node; false when it is the second one.
        internal bool Count() => ++_nodes == 1;

        internal void Set(string lexical, IXsdComponent type)
        {
            Lexical = lexical;
            Type = type;
        }
    }

    // A field element whose value arrives with the end of its simple content.
    [StructLayout(LayoutKind.Auto)]
    private struct Capture
    {
        internal Capture(Target target, int field, int ordinal)
        {
            Target = target;
            Field = field;
            Ordinal = ordinal;
            Delivered = false;
        }

        internal Target Target { get; }

        internal int Field { get; }

        internal int Ordinal { get; }

        internal bool Delivered { get; set; }
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct PendingReference
    {
        internal PendingReference(KeySequence sequence, DocumentLocation location)
        {
            Sequence = sequence;
            Location = location;
        }

        internal KeySequence Sequence { get; }

        internal DocumentLocation Location { get; }
    }
}
