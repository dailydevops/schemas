namespace NetEvolve.Schemas.Schematron.Scopes;

using System;
using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Syntax.Parser;
using NetEvolve.Schemas.XPath.Syntax.Tree;

/// <summary>
/// The lexical constants of variable values and the <see langword="as"/> types of release 1 (design #150 EP-5, #106 Q2). A value is a
/// constant only when it is one string literal, one numeric literal, or one <c>xs:</c> constructor applied to one literal; nothing
/// else is ever folded, because a compiled schema is shared by every run. The check is Schematron's own: it reads the syntax tree
/// of the XPath parser and does not depend on the XPath package folding anything.
/// </summary>
internal static class ConstantValue
{
    /// <summary>Determines whether the <see langword="as"/> type is one of <c>xs:string</c>, <c>xs:boolean</c>, <c>xs:double</c>, and <c>xs:integer</c>.</summary>
    /// <param name="type">The <see langword="as"/> attribute as written.</param>
    /// <returns><see langword="true"/> when the type is supported.</returns>
    internal static bool IsSupportedType(string type)
    {
        Argument.ThrowIfNull(type);

        return type is "xs:string" or "xs:boolean" or "xs:double" or "xs:integer";
    }

    /// <summary>Determines the type of a constant.</summary>
    /// <param name="text">The value expression.</param>
    /// <param name="type">The type name, such as <c>xs:integer</c>, when the text is a constant; otherwise <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the text is a constant.</returns>
    internal static bool TryGetType(ReadOnlySpan<char> text, out string? type)
    {
        type = null;
        if (!XPathParser.TryParse(text, out var tree, out _))
        {
            return false;
        }

        type = LiteralType(tree);
        if (type is null && tree is FunctionCall { Name.Prefix: "xs", Arguments.Count: 1 } constructor)
        {
            type = LiteralType(constructor.Arguments[0]) is null ? null : "xs:" + constructor.Name.LocalName;
        }

        return type is not null;
    }

    /// <summary>Determines whether a constant of a type is accepted where the declared type is required, by the function conversion rules (type promotion included).</summary>
    /// <param name="constantType">The type of the constant.</param>
    /// <param name="declaredType">The supported <see langword="as"/> type.</param>
    /// <returns><see langword="true"/> when the value matches.</returns>
    internal static bool Satisfies(string constantType, string declaredType)
    {
        Argument.ThrowIfNull(constantType);
        Argument.ThrowIfNull(declaredType);

        if (string.Equals(constantType, declaredType, StringComparison.Ordinal))
        {
            return true;
        }

        return declaredType switch
        {
            "xs:double" => constantType is "xs:integer" or "xs:decimal" or "xs:float",
            "xs:string" => constantType is "xs:anyURI",
            _ => false,
        };
    }

    private static string? LiteralType(Expression? expression) =>
        expression switch
        {
            StringLiteral => "xs:string",
            IntegerLiteral => "xs:integer",
            DecimalLiteral => "xs:decimal",
            DoubleLiteral => "xs:double",
            _ => null,
        };
}
