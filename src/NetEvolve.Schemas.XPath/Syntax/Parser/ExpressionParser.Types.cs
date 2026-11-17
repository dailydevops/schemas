namespace NetEvolve.Schemas.XPath.Syntax.Parser;

using System;
using NetEvolve.Schemas.XPath.Syntax.Lexer;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>Sequence types, single types, and kind tests: productions [49] to [69].</summary>
internal ref partial struct ExpressionParser
{
    // [49] SingleType
    private SingleType ParseSingleType()
    {
        var start = _current.Span.Offset;
        var name = ParseTypeName();
        var allowsEmpty = Accept(XPathTokenKind.Question);
        return new SingleType(Span(start), name, allowsEmpty);
    }

    // [50] SequenceType and [51] OccurrenceIndicator
    private SequenceType ParseSequenceType()
    {
        var start = _current.Span.Offset;
        if (IsName(_current, "empty-sequence") && PeekKind() == XPathTokenKind.LeftParen)
        {
            _ = Advance();
            _ = Advance();
            Expect(XPathTokenKind.RightParen);
            return new SequenceType(Span(start), null, Occurrence.ExactlyOne);
        }

        var item = ParseItemType();
        var occurrence = _current.Kind switch
        {
            XPathTokenKind.Question => Occurrence.ZeroOrOne,
            XPathTokenKind.Star => Occurrence.ZeroOrMore,
            XPathTokenKind.Plus => Occurrence.OneOrMore,
            _ => Occurrence.ExactlyOne,
        };
        if (occurrence != Occurrence.ExactlyOne)
        {
            _ = Advance();
        }

        return new SequenceType(Span(start), item, occurrence);
    }

    // [52] ItemType and [53] AtomicType
    private ItemType ParseItemType()
    {
        var token = _current;
        if (token.Kind is XPathTokenKind.Name or XPathTokenKind.QName)
        {
            if (PeekKind() == XPathTokenKind.LeftParen)
            {
                if (IsName(token, "item"))
                {
                    _ = Advance();
                    _ = Advance();
                    Expect(XPathTokenKind.RightParen);
                    return new AnyItemType(Span(token.Span.Offset));
                }

                if (IsKindTestName(token))
                {
                    var test = ParseKindTest();
                    return new KindItemType(test.Location, test);
                }
            }

            return new AtomicType(token.Span, ParseTypeName());
        }

        throw Unexpected();
    }

    // [54] to [58]: the name of a type; a prefix is resolved later.
    private QualifiedName ParseTypeName()
    {
        var token = _current;
        if (token.Kind is not (XPathTokenKind.Name or XPathTokenKind.QName))
        {
            throw Unexpected();
        }

        _ = Advance();
        return CreateName(token);
    }

    // [54] KindTest: the current token is the name of a kind test and "(" follows.
    private KindTest ParseKindTest()
    {
        var name = Advance();
        var start = name.Span.Offset;
        _ = Advance();
        var word = TextOf(name);
        switch (word)
        {
            case "processing-instruction":
                var target = ParseProcessingInstructionTarget();
                Expect(XPathTokenKind.RightParen);
                return new ProcessingInstructionTest(Span(start), target);
            case "document-node":
                var element = ParseDocumentElementTest();
                Expect(XPathTokenKind.RightParen);
                return new DocumentTest(Span(start), element);
            case "element":
                return ParseElementTest(start);
            case "attribute":
                return ParseAttributeTest(start);
            case "schema-element":
                var elementName = ParseTypeName();
                Expect(XPathTokenKind.RightParen);
                return new SchemaElementTest(Span(start), elementName);
            case "schema-attribute":
                var attributeName = ParseTypeName();
                Expect(XPathTokenKind.RightParen);
                return new SchemaAttributeTest(Span(start), attributeName);
            default:
                Expect(XPathTokenKind.RightParen);
                return word switch
                {
                    "text" => new TextTest(Span(start)),
                    "comment" => new CommentTest(Span(start)),
                    _ => new AnyKindTest(Span(start)),
                };
        }
    }

    // [58] PITest: an NCName, or a string literal whose value is trimmed.
    private string? ParseProcessingInstructionTarget()
    {
        var token = _current;
        switch (token.Kind)
        {
            case XPathTokenKind.Name:
                _ = Advance();
                return TextOf(token).ToString();
            case XPathTokenKind.StringLiteral:
                _ = Advance();
                return UnquoteString(token).Trim(' ', '\t', '\r', '\n');
            default:
                return null;
        }
    }

    // [55] DocumentTest: an optional element test or schema-element test.
    private KindTest? ParseDocumentElementTest()
    {
        if (_current.Kind != XPathTokenKind.Name || PeekKind() != XPathTokenKind.LeftParen)
        {
            return null;
        }

        if (!IsName(_current, "element") && !IsName(_current, "schema-element"))
        {
            throw Unexpected();
        }

        return ParseKindTest();
    }

    // [64] ElementTest, [65] ElementNameOrWildcard
    private ElementTest ParseElementTest(int start)
    {
        QualifiedName? name = null;
        QualifiedName? type = null;
        var nillable = false;
        if (_current.Kind != XPathTokenKind.RightParen)
        {
            name = ParseNameOrWildcard();
            if (Accept(XPathTokenKind.Comma))
            {
                type = ParseTypeName();
                nillable = Accept(XPathTokenKind.Question);
            }
        }

        Expect(XPathTokenKind.RightParen);
        return new ElementTest(Span(start), name, type, nillable);
    }

    // [59] AttributeTest, [60] AttribNameOrWildcard
    private AttributeTest ParseAttributeTest(int start)
    {
        QualifiedName? name = null;
        QualifiedName? type = null;
        if (_current.Kind != XPathTokenKind.RightParen)
        {
            name = ParseNameOrWildcard();
            if (Accept(XPathTokenKind.Comma))
            {
                type = ParseTypeName();
            }
        }

        Expect(XPathTokenKind.RightParen);
        return new AttributeTest(Span(start), name, type);
    }

    // A name, or "*" which is represented as no name.
    private QualifiedName? ParseNameOrWildcard() => Accept(XPathTokenKind.Wildcard) ? null : ParseTypeName();

    private readonly bool IsName(XPathToken token, string name) =>
        token.Kind == XPathTokenKind.Name && TextOf(token).SequenceEqual(name.AsSpan());

    // The names of the kind tests of [54]; a prefixed name is never one.
    private readonly bool IsKindTestName(XPathToken token) =>
        token.Kind == XPathTokenKind.Name
        && TextOf(token)
            is "node"
                or "text"
                or "comment"
                or "processing-instruction"
                or "document-node"
                or "element"
                or "attribute"
                or "schema-element"
                or "schema-attribute";
}
