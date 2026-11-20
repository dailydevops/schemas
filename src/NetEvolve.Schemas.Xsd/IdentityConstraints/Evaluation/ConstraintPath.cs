namespace NetEvolve.Schemas.Xsd.IdentityConstraints.Evaluation;

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// A selector or field expression of the restricted subset (Structures section 3.11.6), compiled for the streaming matcher: a
/// union of paths, each a chain of name tests over the child axis, optionally led by <c>.//</c> and, for a field, ended by an
/// attribute name test. A path is matched against the names of the open elements, so no tree is needed.
/// </summary>
/// <remarks>
/// The syntax tree does not keep abbreviations apart from the long forms, so <c>child::a</c> and <c>attribute::a</c> match like
/// <c>a</c> and <c>@a</c>. A prefix without a binding, an expression the parser rejects, and a step outside the subset make a path
/// that matches nothing; the definition has rejected them before, so this happens only for a path that bypassed it. An unprefixed
/// name test selects names in no namespace (the <c>xpathDefaultNamespace</c> attribute of XSD 1.1 is XSD-85).
/// </remarks>
internal sealed class ConstraintPath
{
    private const string XmlNamespace = "http://www.w3.org/XML/1998/namespace";

    private readonly Alternative[] _alternatives;

    private ConstraintPath(Alternative[] alternatives) => _alternatives = alternatives;

    /// <summary>Compiles an expression.</summary>
    /// <param name="xpath">The expression.</param>
    /// <param name="bindings">The namespaces of the prefixes of the expression.</param>
    /// <returns>The compiled paths.</returns>
    internal static ConstraintPath Compile(string xpath, IReadOnlyList<KeyValuePair<string, string>> bindings)
    {
        Argument.ThrowIfNull(xpath);
        Argument.ThrowIfNull(bindings);

        var alternatives = new List<Alternative>();
        if (XPathParser.TryParse(xpath.AsSpan(), out var tree, out _))
        {
            AddPaths(tree!, bindings, alternatives);
        }

        return new ConstraintPath(alternatives.ToArray());
    }

    /// <summary>Determines whether an element is selected by one of the paths that end in an element.</summary>
    /// <param name="stack">The names of the open elements, the root first.</param>
    /// <param name="contextDepth">The depth of the context element, the root being 1.</param>
    /// <param name="depth">The depth of the element to test, which is the innermost open element.</param>
    /// <returns><see langword="true"/> when the element is selected.</returns>
    internal bool MatchesElement(ElementName[] stack, int contextDepth, int depth)
    {
        Argument.ThrowIfNull(stack);

        for (var i = 0; i < _alternatives.Length; i++)
        {
            if (!_alternatives[i].HasAttribute && _alternatives[i].MatchesElement(stack, contextDepth, depth))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Determines whether an attribute of the innermost open element is selected by one of the attribute paths.</summary>
    /// <param name="stack">The names of the open elements, the root first.</param>
    /// <param name="contextDepth">The depth of the context element, the root being 1.</param>
    /// <param name="depth">The depth of the element that owns the attribute, which is the innermost open element.</param>
    /// <param name="namespaceName">The namespace name of the attribute, empty for none.</param>
    /// <param name="localName">The local name of the attribute.</param>
    /// <returns><see langword="true"/> when the attribute is selected.</returns>
    internal bool MatchesAttribute(
        ElementName[] stack,
        int contextDepth,
        int depth,
        string namespaceName,
        string localName
    )
    {
        Argument.ThrowIfNull(stack);
        Argument.ThrowIfNull(namespaceName);
        Argument.ThrowIfNull(localName);

        for (var i = 0; i < _alternatives.Length; i++)
        {
            if (
                _alternatives[i].HasAttribute
                && _alternatives[i].Attribute.Matches(namespaceName, localName)
                && _alternatives[i].MatchesElement(stack, contextDepth, depth)
            )
            {
                return true;
            }
        }

        return false;
    }

    private static void AddPaths(
        Expression expression,
        IReadOnlyList<KeyValuePair<string, string>> bindings,
        List<Alternative> alternatives
    )
    {
        switch (expression)
        {
            case SetExpression set:
                AddPaths(set.Left, bindings, alternatives);
                AddPaths(set.Right, bindings, alternatives);
                break;
            case PathExpression path:
                AddPath(path.Steps, bindings, alternatives);
                break;
            default:
                AddPath([expression], bindings, alternatives);
                break;
        }
    }

    private static void AddPath(
        IReadOnlyList<Expression> steps,
        IReadOnlyList<KeyValuePair<string, string>> bindings,
        List<Alternative> alternatives
    )
    {
        var descendant = false;
        var names = new List<Step>();
        var attribute = default(Step);
        var hasAttribute = false;
        foreach (var step in steps)
        {
            switch (step)
            {
                case ContextItemExpression:
                    break;
                case AxisStep { Axis: Axis.DescendantOrSelf }:
                    descendant = true;
                    break;
                case AxisStep { Axis: Axis.Child, Test: NameTest test }:
                    names.Add(Step.Of(test, bindings));
                    break;
                case AxisStep { Axis: Axis.Attribute, Test: NameTest test }:
                    attribute = Step.Of(test, bindings);
                    hasAttribute = true;
                    break;
                default:
                    return;
            }
        }

        alternatives.Add(new Alternative(descendant, names.ToArray(), attribute, hasAttribute));
    }

    private enum StepKind
    {
        Never = 0,
        Name = 1,
        AnyInNamespace = 2,
        Any = 3,
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct Step
    {
        private readonly StepKind _kind;
        private readonly string _namespaceName;
        private readonly string _localName;

        private Step(StepKind kind, string namespaceName, string localName)
        {
            _kind = kind;
            _namespaceName = namespaceName;
            _localName = localName;
        }

        internal static Step Of(NameTest test, IReadOnlyList<KeyValuePair<string, string>> bindings)
        {
            switch (test.Kind)
            {
                case NameTestKind.AnyName:
                    return new Step(StepKind.Any, string.Empty, string.Empty);
                case NameTestKind.QualifiedName:
                    return Resolve(test.Prefix, bindings, out var qualified)
                        ? new Step(StepKind.Name, qualified, test.LocalName ?? string.Empty)
                        : default;
                case NameTestKind.AnyLocalName:
                    return Resolve(test.Prefix, bindings, out var space)
                        ? new Step(StepKind.AnyInNamespace, space, string.Empty)
                        : default;
                default:
                    return default;
            }
        }

        internal bool Matches(string namespaceName, string localName) =>
            _kind switch
            {
                StepKind.Any => true,
                StepKind.AnyInNamespace => string.Equals(_namespaceName, namespaceName, StringComparison.Ordinal),
                StepKind.Name => string.Equals(_namespaceName, namespaceName, StringComparison.Ordinal)
                    && string.Equals(_localName, localName, StringComparison.Ordinal),
                _ => false,
            };

        private static bool Resolve(
            string? prefix,
            IReadOnlyList<KeyValuePair<string, string>> bindings,
            out string namespaceName
        )
        {
            namespaceName = string.Empty;
            if (prefix is null)
            {
                return true;
            }

            if (string.Equals(prefix, "xml", StringComparison.Ordinal))
            {
                namespaceName = XmlNamespace;
                return true;
            }

            for (var i = 0; i < bindings.Count; i++)
            {
                if (string.Equals(bindings[i].Key, prefix, StringComparison.Ordinal))
                {
                    namespaceName = bindings[i].Value;
                    return true;
                }
            }

            return false;
        }
    }

    [StructLayout(LayoutKind.Auto)]
    private readonly struct Alternative
    {
        private readonly bool _descendant;
        private readonly Step[] _steps;

        internal Alternative(bool descendant, Step[] steps, Step attribute, bool hasAttribute)
        {
            _descendant = descendant;
            _steps = steps;
            Attribute = attribute;
            HasAttribute = hasAttribute;
        }

        internal Step Attribute { get; }

        internal bool HasAttribute { get; }

        // The last steps are matched against the last open elements; the elements between the context and them are the
        // descendants that a leading './/' allows.
        internal bool MatchesElement(ElementName[] stack, int contextDepth, int depth)
        {
            var count = _steps.Length;
            var distance = depth - contextDepth;
            if (distance < count || (!_descendant && distance != count))
            {
                return false;
            }

            for (var i = 0; i < count; i++)
            {
                var name = stack[depth - count + i];
                if (!_steps[i].Matches(name.NamespaceName, name.LocalName))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
