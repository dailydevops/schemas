namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// The <see langword="as"/> type of a function, a parameter, or a result: a sequence type of XPath 2.0 with the function conversion rules
/// (XPath 2.0 section 3.1.5) applied to a value before the match. It reuses the sequence type matcher and the conversion of the
/// XPath package; this class only parses the text and chooses the conversion.
/// </summary>
/// <remarks>
/// Limitations: the item types are the built-in ones (schema-aware types await DEF-11, #23); the conversion (atomization, the cast
/// of <c>xs:untypedAtomic</c>, and the type promotion) covers the item types <c>item()</c>, <c>node()</c>, and the built-in atomic
/// types that the XPath conversion names, and any other item type, such as a kind test with a name or a type derived from a built-in
/// one, is only matched without conversion.
/// </remarks>
internal sealed class XsltSequenceType
{
    private const string XmlSchemaNamespace = "http://www.w3.org/2001/XMLSchema";

    private readonly SequenceTypeMatcher _matcher;
    private readonly XPathParameterType? _conversion;

    private XsltSequenceType(string text, SequenceTypeMatcher matcher, XPathParameterType? conversion)
    {
        Text = text;
        _matcher = matcher;
        _conversion = conversion;
    }

    /// <summary>Gets the type as written.</summary>
    internal string Text { get; }

    /// <summary>Parses and binds a sequence type.</summary>
    /// <param name="text">The type as written in the <see langword="as"/> attribute.</param>
    /// <param name="context">The static context that binds the prefixes of the type.</param>
    /// <param name="type">The type when the result is <see langword="true"/>.</param>
    /// <param name="diagnostic">The static error when the result is <see langword="false"/>: XPST0003, XPST0051, XPST0081, or XPST0008.</param>
    /// <returns><see langword="true"/> when the text is a sequence type.</returns>
    internal static bool TryParse(
        string text,
        XPathStaticContext context,
        out XsltSequenceType type,
        out XPathDiagnostic diagnostic
    )
    {
        Argument.ThrowIfNull(text);
        Argument.ThrowIfNull(context);

        type = null!;
        // The type is parsed as the right operand of instance of; anything that is not exactly one sequence type changes the shape of the tree.
        if (!XPathParser.TryParse(("() instance of " + text).AsSpan(), out var tree, out diagnostic))
        {
            return false;
        }

        if (tree is not InstanceOfExpression { Type: { } syntax })
        {
            diagnostic = XPathErrors.Exception(XPathErrorCodes.XPST0003).Diagnostic;
            return false;
        }

        try
        {
            var matcher = SequenceTypeMatcher.Create(syntax, context);
            type = new XsltSequenceType(text, matcher, ConversionOf(syntax, context));
            return true;
        }
        catch (XPathException exception)
        {
            diagnostic = exception.Diagnostic;
            return false;
        }
    }

    /// <summary>Converts a value by the function conversion rules and checks it against the type.</summary>
    /// <param name="value">The value.</param>
    /// <param name="converted">The converted value when the result is <see langword="true"/>.</param>
    /// <param name="diagnostic">The dynamic error when the result is <see langword="false"/>: XPTY0004, or FORG0001 for an <c>xs:untypedAtomic</c> value that cannot be cast.</param>
    /// <returns><see langword="true"/> when the value matches the type after the conversion.</returns>
    internal bool TryConvert(in XdmSequence value, out XdmSequence converted, out XPathDiagnostic diagnostic)
    {
        converted = value;
        diagnostic = default;
        if (_conversion is { } conversion)
        {
            try
            {
                converted = FunctionConversion.Convert(in value, conversion);
            }
            catch (XPathException exception)
            {
                diagnostic = exception.Diagnostic;
                return false;
            }
        }

        if (_matcher.Matches(in converted))
        {
            return true;
        }

        diagnostic = XPathErrors.Exception(XPathErrorCodes.XPTY0004).Diagnostic;
        return false;
    }

    private static XPathParameterType? ConversionOf(SequenceType syntax, XPathStaticContext context)
    {
        if (syntax.ItemType is null)
        {
            return null;
        }

        var item = syntax.ItemType switch
        {
            AnyItemType => XPathItemType.Item,
            KindItemType { Test: AnyKindTest } => XPathItemType.Node,
            AtomicType atomic => AtomicItemType(atomic.Name, context),
            _ => null,
        };
        return item is { } found ? new XPathParameterType(found, syntax.Occurrence) : null;
    }

    private static XPathItemType? AtomicItemType(QualifiedName name, XPathStaticContext context)
    {
        if (
            name.Prefix is null
            || !context.TryResolveNamespace(name.Prefix, out var uri)
            || !string.Equals(uri, XmlSchemaNamespace, StringComparison.Ordinal)
        )
        {
            return null;
        }

        return name.LocalName switch
        {
            "anyAtomicType" => XPathItemType.AnyAtomic,
            "untypedAtomic" => XPathItemType.UntypedAtomic,
            "string" => XPathItemType.String,
            "boolean" => XPathItemType.Boolean,
            "integer" => XPathItemType.Integer,
            "decimal" => XPathItemType.Decimal,
            "float" => XPathItemType.Float,
            "double" => XPathItemType.Double,
            "anyURI" => XPathItemType.AnyUri,
            "dateTime" => XPathItemType.DateTime,
            "date" => XPathItemType.Date,
            "time" => XPathItemType.Time,
            "duration" => XPathItemType.Duration,
            "yearMonthDuration" => XPathItemType.YearMonthDuration,
            "dayTimeDuration" => XPathItemType.DayTimeDuration,
            _ => null,
        };
    }
}
