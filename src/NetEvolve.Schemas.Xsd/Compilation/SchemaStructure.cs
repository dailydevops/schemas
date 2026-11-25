namespace NetEvolve.Schemas.Xsd.Compilation;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Xsd.SchemaDocuments;

/// <summary>
/// The order and number of the children of every schema element (the content models of the schema for schemas, Structures
/// appendix A), which the document grammar leaves out: it checks only that a child may appear at all and that an annotation
/// comes first.
/// </summary>
/// <remarks>
/// <para>
/// A content model is written as a list of child names with <c>?</c>, <c>*</c>, <c>+</c>, <c>|</c>, and parentheses, and is matched by
/// following the set of reachable positions in the child list, so no model can fail by backtracking. Only the children in the schema
/// namespace count; an element of another namespace is not a child of the model. The models of XSD 1.1 are the union of both
/// versions: a 1.1 element in a 1.0 schema is rejected by the strict 1.0 gate, not here.
/// </para>
/// <para>Limitation: the content of <c>annotation</c>, <c>appinfo</c>, and <c>documentation</c> is not checked.</para>
/// </remarks>
internal static class SchemaStructure
{
    private const string Facets =
        "(minExclusive|minInclusive|maxExclusive|maxInclusive|totalDigits|fractionDigits|length|minLength|maxLength|enumeration|whiteSpace|pattern|assertion|explicitTimezone)";
    private const string Attributes = "(attribute|attributeGroup)* anyAttribute? assert*";
    private const string Particles = "(group|all|choice|sequence)?";

    // The elements whose only child is an annotation.
    private static readonly string[] LeafElements =
    [
        "include",
        "import",
        "attributeGroup@reference",
        "group@reference",
        "anyAttribute",
        "any",
        "selector",
        "field",
        "notation",
        "assert",
        "minExclusive",
        "minInclusive",
        "maxExclusive",
        "maxInclusive",
        "totalDigits",
        "fractionDigits",
        "length",
        "minLength",
        "maxLength",
        "enumeration",
        "whiteSpace",
        "pattern",
        "assertion",
        "explicitTimezone",
    ];

    private static readonly Dictionary<string, Node> Models = Build();

    /// <summary>Tells whether the children of an element are in an order and number that its content model allows.</summary>
    /// <param name="node">The element, an element of the schema namespace.</param>
    /// <returns><see langword="true"/> when the content is valid or the element has no content model.</returns>
    internal static bool IsValid(SchemaSyntaxNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (!Models.TryGetValue(KeyOf(node), out var model))
        {
            return true;
        }

        var names = new List<string>(node.ChildCount);
        for (var i = 0; i < node.ChildCount; i++)
        {
            var child = node.GetChildNode(i);
            if (string.Equals(child.Name.NamespaceName, SchemaGrammar.Namespace, StringComparison.Ordinal))
            {
                names.Add(child.Name.LocalName);
            }
        }

        var reached = model.Step(Positions.Start, names);
        return reached.Contains(names.Count);
    }

    /// <summary>Gets the content model of an element in a readable form.</summary>
    /// <param name="node">The element.</param>
    /// <returns>The model, <see langword="null"/> when the element has none.</returns>
    internal static string? DescribeModel(SchemaSyntaxNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        return Models.TryGetValue(KeyOf(node), out var model) ? model.Text : null;
    }

    // restriction, extension, group, and attributeGroup have one model per context.
    private static string KeyOf(SchemaSyntaxNode node)
    {
        var parent = node.Parent?.Name.LocalName ?? string.Empty;
        var name = node.Name.LocalName;
        return name switch
        {
            "restriction" => name + "@" + (parent is "simpleType" or "simpleContent" ? parent : "complexContent"),
            "extension" => name
                + "@"
                + (string.Equals(parent, "simpleContent", StringComparison.Ordinal) ? parent : "complexContent"),
            "group" or "attributeGroup" => name
                + (parent is "schema" or "redefine" or "override" ? "@definition" : "@reference"),
            _ => name,
        };
    }

    private static Dictionary<string, Node> Build()
    {
        var models = new Dictionary<string, Node>(StringComparer.Ordinal);
        AddDocumentModels(models);
        AddTypeModels(models);
        AddParticleModels(models);
        foreach (var name in LeafElements)
        {
            models.Add(name, Parse("annotation?"));
        }

        return models;
    }

    private static void AddDocumentModels(Dictionary<string, Node> models)
    {
        models["schema"] = Parse(
            "(include|import|redefine|override|annotation)* (defaultOpenContent annotation*)? ((simpleType|complexType|group|attributeGroup|element|attribute|notation) annotation*)*"
        );
        models["redefine"] = Parse("(annotation|simpleType|complexType|group|attributeGroup)*");
        models["override"] = Parse(
            "(annotation|simpleType|complexType|group|attributeGroup|element|attribute|notation)*"
        );
        models["openContent"] = Parse("annotation? any?");
        models["defaultOpenContent"] = Parse("annotation? any?");
    }

    private static void AddTypeModels(Dictionary<string, Node> models)
    {
        models["simpleType"] = Parse("annotation? (restriction|list|union)");
        models["restriction@simpleType"] = Parse("annotation? simpleType? " + Facets + "*");
        models["list"] = Parse("annotation? simpleType?");
        models["union"] = Parse("annotation? simpleType*");
        models["complexType"] = Parse(
            "annotation? (simpleContent|complexContent|(openContent? " + Particles + " " + Attributes + "))"
        );
        models["simpleContent"] = Parse("annotation? (restriction|extension)");
        models["complexContent"] = Parse("annotation? (restriction|extension)");
        models["restriction@simpleContent"] = Parse("annotation? simpleType? " + Facets + "* " + Attributes);
        models["extension@simpleContent"] = Parse("annotation? " + Attributes);
        models["restriction@complexContent"] = Parse("annotation? openContent? " + Particles + " " + Attributes);
        models["extension@complexContent"] = Parse("annotation? openContent? " + Particles + " " + Attributes);
        models["attribute"] = Parse("annotation? simpleType?");
        models["attributeGroup@definition"] = Parse("annotation? (attribute|attributeGroup)* anyAttribute?");
    }

    private static void AddParticleModels(Dictionary<string, Node> models)
    {
        models["element"] = Parse("annotation? (simpleType|complexType)? alternative* (unique|key|keyref)*");
        models["alternative"] = Parse("annotation? (simpleType|complexType)?");
        models["group@definition"] = Parse("annotation? (all|choice|sequence)");
        models["all"] = Parse("annotation? (element|any|group)*");
        models["choice"] = Parse("annotation? (element|group|choice|sequence|any)*");
        models["sequence"] = Parse("annotation? (element|group|choice|sequence|any)*");
        models["unique"] = Parse("annotation? selector field+");
        models["key"] = Parse("annotation? selector field+");
        models["keyref"] = Parse("annotation? selector field+");
    }

    private static Node Parse(string text) => new Parser(text).ParseAll();

    // A set of positions in a child list, as a bit set. The first 64 positions are held inline, so a list of up to 64 children (all
    // but a few elements of a schema document) matches without allocating; a longer list adds an array for the positions above.
    private readonly struct Positions
    {
        private readonly ulong _low;
        private readonly ulong[]? _high;

        private Positions(ulong low, ulong[]? high)
        {
            _low = low;
            _high = high;
        }

        internal static Positions Empty => default;

        internal static Positions Start => new(1, null);

        internal bool IsEmpty => _low == 0 && IsHighEmpty();

        internal bool Contains(int position) => (Word(position >> 6) & (1UL << (position & 63))) != 0;

        internal Positions Add(int position)
        {
            var index = position >> 6;
            if (index == 0)
            {
                return new Positions(_low | (1UL << position), _high);
            }

            var high = new ulong[Math.Max(index, _high?.Length ?? 0)];
            _high?.CopyTo(high, 0);
            high[index - 1] |= 1UL << (position & 63);
            return new Positions(_low, high);
        }

        internal Positions Union(Positions other)
        {
            if (_high is null && other._high is null)
            {
                return new Positions(_low | other._low, null);
            }

            var high = new ulong[Math.Max(_high?.Length ?? 0, other._high?.Length ?? 0)];
            for (var i = 0; i < high.Length; i++)
            {
                high[i] = Word(i + 1) | other.Word(i + 1);
            }

            return new Positions(_low | other._low, high);
        }

        internal bool IsSupersetOf(Positions other)
        {
            if ((other._low & ~_low) != 0)
            {
                return false;
            }

            for (var i = 0; i < (other._high?.Length ?? 0); i++)
            {
                if ((other._high![i] & ~Word(i + 1)) != 0)
                {
                    return false;
                }
            }

            return true;
        }

        // The first position from the given one on, or -1.
        internal int Next(int from)
        {
            var words = 1 + (_high?.Length ?? 0);
            var position = from;
            while ((position >> 6) < words)
            {
                var bits = Word(position >> 6) >> (position & 63);
                if (bits != 0)
                {
                    while ((bits & 1) == 0)
                    {
                        bits >>= 1;
                        position++;
                    }

                    return position;
                }

                position = ((position >> 6) + 1) << 6;
            }

            return -1;
        }

        private ulong Word(int index) =>
            index == 0 ? _low
            : _high is not null && index <= _high.Length ? _high[index - 1]
            : 0;

        private bool IsHighEmpty()
        {
            if (_high is not null)
            {
                foreach (var word in _high)
                {
                    if (word != 0)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    private abstract class Node
    {
        internal string Text { get; set; } = string.Empty;

        // The positions in the child list that follow from the given positions by matching this model.
        internal abstract Positions Step(Positions from, List<string> names);
    }

    private sealed class Name(string name) : Node
    {
        internal override Positions Step(Positions from, List<string> names)
        {
            var to = Positions.Empty;
            for (var position = from.Next(0); position >= 0; position = from.Next(position + 1))
            {
                if (position < names.Count && string.Equals(names[position], name, StringComparison.Ordinal))
                {
                    to = to.Add(position + 1);
                }
            }

            return to;
        }
    }

    private sealed class Sequence(List<Node> items) : Node
    {
        internal override Positions Step(Positions from, List<string> names)
        {
            var current = from;
            foreach (var item in items)
            {
                current = item.Step(current, names);
            }

            return current;
        }
    }

    private sealed class Choice(List<Node> alternatives) : Node
    {
        internal override Positions Step(Positions from, List<string> names)
        {
            var to = Positions.Empty;
            foreach (var alternative in alternatives)
            {
                to = to.Union(alternative.Step(from, names));
            }

            return to;
        }
    }

    private sealed class Repeat(Node item, bool allowsNone, bool allowsMany) : Node
    {
        internal override Positions Step(Positions from, List<string> names)
        {
            var to = allowsNone ? from : Positions.Empty;
            var current = item.Step(from, names);
            while (!current.IsEmpty && !to.IsSupersetOf(current))
            {
                to = to.Union(current);
                if (!allowsMany)
                {
                    break;
                }

                current = item.Step(current, names);
            }

            return to;
        }
    }

    private sealed class Parser(string text)
    {
        private int _position;

        internal Node ParseAll()
        {
            var node = ParseChoice();
            node.Text = text;
            return node;
        }

        private Node ParseChoice()
        {
            var alternatives = new List<Node> { ParseSequence() };
            while (Peek() == '|')
            {
                _position++;
                alternatives.Add(ParseSequence());
            }

            return alternatives.Count == 1 ? alternatives[0] : new Choice(alternatives);
        }

        private Node ParseSequence()
        {
            var items = new List<Node>();
            while (Peek() is not ('|' or ')' or '\0'))
            {
                items.Add(ParseItem());
            }

            return items.Count == 1 ? items[0] : new Sequence(items);
        }

        private Node ParseItem()
        {
            Node atom;
            if (Peek() == '(')
            {
                _position++;
                atom = ParseChoice();
                _position++;
            }
            else
            {
                var start = _position;
                while (char.IsLetter(Current()))
                {
                    _position++;
                }

                atom = new Name(text.Substring(start, _position - start));
            }

            var suffix = Current();
            if (suffix is '?' or '*' or '+')
            {
                _position++;
                atom = new Repeat(atom, suffix != '+', suffix != '?');
            }

            return atom;
        }

        private char Current() => _position < text.Length ? text[_position] : '\0';

        private char Peek()
        {
            while (_position < text.Length && text[_position] == ' ')
            {
                _position++;
            }

            return Current();
        }
    }
}
