namespace NetEvolve.Schemas.XPath.Formatting.Numbers;

using System;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Casting.Core;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Functions.Strings;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>format-number</c> in two arities (Functions and Operators 3.1, 4.7.2; XSLT 2.0, 16.4): the number formatted with a picture string
/// and a decimal format. There are two instances in the default catalog with disjoint version masks: the XPath 3.0 and 3.1 function (the 3.1
/// rules, FODF codes, no host flag) and the XSLT 2.0 function (the XSLT 2.0 rules, XTDE codes, visible in a 2.0 static context only under
/// <see cref="XPathHostRequirements.Xslt20Host"/>). The empty sequence as the number is NaN.
/// </summary>
/// <remarks>
/// <para>
/// The decimal formats come from <see cref="XPathHostSlots.DecimalFormats"/>. The two-argument form, and the three-argument form with an
/// empty name in XPath 3, use the unnamed format: the one the provider returns for the default name (<see langword="default"/> of
/// <see cref="XPathQName"/>), and the default format of <see cref="XPathDecimalFormat"/> when the slot is empty or has none. A named format needs
/// the slot, as every host slot does (XPDY0002 when it is empty), and the name has to be known to it (FODF1280, XTDE1280).
/// </para>
/// <para>
/// Limitations: XPath 3.0 uses the rules of 3.1 (the exponent separator exists since 3.0; there is no third rule set). A prefixed name is
/// resolved by the namespace context the function was built with, and by none in the default catalog, until a call reaches the static
/// namespaces (#752). The characters of a decimal format are UTF-16 code units.
/// </para>
/// </remarks>
internal sealed class FormatNumberFunction : XPathFunction
{
    private const int InitialLength = 256;

    private readonly bool _xslt20;
    private readonly INamespaceContext _namespaces;

    private FormatNumberFunction(bool xslt20, INamespaceContext? namespaces)
        : base(
            new XPathQName(XPathStaticContext.Default.DefaultFunctionNamespace, "format-number"),
            2,
            3,
            [
                new XPathParameterType(XPathItemType.Numeric, Occurrence.ZeroOrOne),
                new XPathParameterType(XPathItemType.String, Occurrence.ExactlyOne),
                new XPathParameterType(XPathItemType.String, xslt20 ? Occurrence.ExactlyOne : Occurrence.ZeroOrOne),
            ]
        )
    {
        _xslt20 = xslt20;
        _namespaces = namespaces ?? NoNamespaceContext.Instance;
    }

    /// <summary>Gets the language versions in which the function is visible: XPath 2.0 for the XSLT 2.0 function, XPath 3.0 and 3.1 for the other.</summary>
    public override XPathVersionMask Versions =>
        _xslt20 ? XPathVersionMask.V20 : XPathVersionMask.V30 | XPathVersionMask.V31;

    /// <summary>Gets the host flag: the XSLT 2.0 function is visible only when the host sets <see cref="XPathHostRequirements.Xslt20Host"/>.</summary>
    public override XPathHostRequirements HostRequirements =>
        _xslt20 ? XPathHostRequirements.Xslt20Host : XPathHostRequirements.None;

    /// <summary>Creates the function of XPath 3.0 and 3.1: the 3.1 rules and the FODF error codes.</summary>
    /// <param name="namespaces">The namespaces that resolve a prefix of the decimal format name, <see langword="null"/> for none.</param>
    /// <returns>The function.</returns>
    public static FormatNumberFunction ForXPath3(INamespaceContext? namespaces = null) => new(false, namespaces);

    /// <summary>Creates the function of the XSLT 2.0 host: the XSLT 2.0 rules and the XTDE error codes.</summary>
    /// <param name="namespaces">The namespaces that resolve a prefix of the decimal format name, <see langword="null"/> for none.</param>
    /// <returns>The function.</returns>
    public static FormatNumberFunction ForXslt20(INamespaceContext? namespaces = null) => new(true, namespaces);

    /// <inheritdoc />
    /// <exception cref="XPathException">
    /// The decimal format name is invalid or unknown (FODF1280, XTDE1280), the slot is empty for a named format (XPDY0002), or the picture
    /// string or the decimal format breaks a rule (FODF1310, XTDE1310).
    /// </exception>
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var arguments = invocation.Arguments;
        var rules = _xslt20 ? NumberPictureRules.Xslt20 : NumberPictureRules.Xpath31;

        var format = ResolveFormat(invocation.Context, arguments, rules);
        _ = arguments[1][0].TryGetString(out var text);
        if (!NumberPictureParser.TryParse(text!, format, rules, out var picture, out var problem))
        {
            throw FormatNumberErrors.InvalidPicture(_xslt20, problem);
        }

        var number = arguments[0].IsEmpty ? XdmItem.FromDouble(double.NaN) : arguments[0][0];
        Span<char> initial = stackalloc char[InitialLength];
        var builder = new TextBuilder(initial, invocation.Context.Limits);
        try
        {
            NumberFormatter.Format(in number, picture!, format, ref builder);
            invocation.Result = XdmSequence.Create(XdmItem.FromString(builder.ToString()));
        }
        finally
        {
            builder.Dispose();
        }

        return XPathEvaluationStatus.Ok;
    }

    private DecimalFormatModel ResolveFormat(
        XPathEvaluationContext context,
        ReadOnlySpan<XdmSequence> arguments,
        NumberPictureRules rules
    )
    {
        var provider = context.Host.DecimalFormats;
        XPathDecimalFormat? declared;
        string? name = null;
        if (arguments.Length > 2 && !arguments[2].IsEmpty)
        {
            _ = arguments[2][0].TryGetString(out name);
        }

        // XPath 3: an empty name is the unnamed format. XSLT 2.0 has no empty name: the empty string is not a QName.
        if (name is not null && (_xslt20 || name.Trim(' ', '\t', '\r', '\n').Length > 0))
        {
            if (!DecimalFormatName.TryParse(name, _xslt20, _namespaces, out var qname))
            {
                throw FormatNumberErrors.UnknownFormat(_xslt20, name);
            }

            // A named format lives in the slot, as every host slot is a requirement of the function that reads it (#123, AP11).
            if (provider is null)
            {
                throw XPathErrors.Exception(XPathErrorCodes.XPDY0002);
            }

            if (!provider.TryGetDecimalFormat(qname, out declared) || declared is null)
            {
                throw FormatNumberErrors.UnknownFormat(_xslt20, name);
            }
        }
        else if (provider is null || !provider.TryGetDecimalFormat(default, out declared) || declared is null)
        {
            return DecimalFormatModel.Default;
        }

        return DecimalFormatModel.TryCreate(declared, rules, out var model, out var problem)
            ? model!
            : throw FormatNumberErrors.InvalidPicture(_xslt20, problem);
    }
}
