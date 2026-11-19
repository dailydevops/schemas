namespace NetEvolve.Schemas.XPath.Casting.Core;

using NetEvolve.Arguments;
using NetEvolve.Schemas.XPath.Context;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Syntax.Tree;
using NetEvolve.Schemas.XPath.Types;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>Binds the target type of <c>cast as</c> and <c>castable as</c> while lowering, so that every static error is raised at compile time.</summary>
/// <remarks>
/// Limitation: a built-in atomic type that the casting table has no row for, which is <c>xs:dateTimeStamp</c> of XPath 3.1 alone since the derived
/// types have their rows (XP-23), is rejected with XPST0051 and a message that names it. <c>xs:dateTimeStamp</c> is not a type of XPath 2.0. A user-defined
/// type is XPST0051 until schema-aware processing exists (DEF-40, #52).
/// </remarks>
internal static class CastTargetResolver
{
    /// <summary>Resolves the target type.</summary>
    /// <param name="type">The single type of the expression.</param>
    /// <param name="context">The static context that resolves prefixes and the default namespace.</param>
    /// <param name="table">The casting table.</param>
    /// <returns>The type, a target of a row of the table.</returns>
    /// <exception cref="System.ArgumentNullException">An argument is <see langword="null"/>.</exception>
    /// <exception cref="XPathException">A prefix is not bound (XPST0081), the name is not an atomic built-in type or has no row yet (XPST0051), or it is <c>xs:anyAtomicType</c> or <c>xs:NOTATION</c> (XPST0080).</exception>
    public static XdmTypeId Resolve(SingleType type, XPathStaticContext context, CastingTable table)
    {
        Argument.ThrowIfNull(type);
        Argument.ThrowIfNull(context);
        Argument.ThrowIfNull(table);

        var name = type.Name;

        // The binder of the item types reports an unbound prefix, a name that is not an atomic built-in type, and a version-gated type.
        var id = ItemTypeMatcher.Bind(new AtomicType(name.Location, name), context).Type;
        if (
            id == XdmTypes.Get("anyAtomicType")
            || (
                XdmTypes.TryGet(XdmTypes.SchemaNamespace, "NOTATION", out var notation)
                && XdmTypes.IsDerivedFrom(id, notation)
            )
        )
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPST0080, name.Location.Offset, name.Location.Length);
        }

        if (!table.IsTarget(id))
        {
            var written = name.Prefix is null ? name.LocalName : name.Prefix + ":" + name.LocalName;
            var diagnostic = XPathDiagnostic.Create(
                XPathErrorCodes.XPST0051,
                name.Location.Offset,
                name.Location.Length
            );
            throw new XPathException(
                new XPathDiagnostic(
                    diagnostic.QName,
                    diagnostic.Kind,
                    $"The built-in type '{written}' is not supported yet as the target of a cast.",
                    diagnostic.Offset,
                    diagnostic.Length
                )
            );
        }

        return id;
    }
}
