namespace NetEvolve.Schemas.XPath.Functions.Maps;

using System;
using NetEvolve.Schemas.Xml.Datatypes.QNames;
using NetEvolve.Schemas.XPath.Api;
using NetEvolve.Schemas.XPath.Errors;
using NetEvolve.Schemas.XPath.Functions.Binding;
using NetEvolve.Schemas.XPath.Xdm.Maps;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// <c>map:merge</c> (Functions and Operators 3.1, 17.1.5): one map with the entries of all the maps of the first argument. The
/// <c>duplicates</c> option of the second argument says what happens to a key that two maps share: <c>use-first</c> (the default),
/// <c>use-last</c>, <c>use-any</c>, <c>combine</c> (the values are concatenated in order), or <c>reject</c> (FOJS0003).
/// </summary>
/// <remarks>Limitation: <c>use-any</c> behaves like <c>use-first</c>; other options of the option map are ignored.</remarks>
internal sealed class MapMergeFunction : XPathFunction
{
    private static readonly XdmItem DuplicatesKey = XdmItem.FromString("duplicates");

    /// <summary>Initializes a new instance of the <see cref="MapMergeFunction"/> class.</summary>
    public MapMergeFunction()
        : base(
            new XPathQName(MapFunction.MapNamespace, "merge"),
            1,
            2,
            [
                new(XPathItemType.Item, Syntax.Tree.Occurrence.ZeroOrMore),
                new(XPathItemType.Item, Syntax.Tree.Occurrence.ZeroOrMore),
            ]
        ) { }

    /// <inheritdoc />
    public override XPathVersionMask Versions => XPathVersionMask.V31;

    /// <inheritdoc />
    public override XPathEvaluationStatus Invoke(ref XPathFunctionCall invocation)
    {
        var maps = invocation.Arguments[0];
        var duplicates =
            invocation.Arguments.Length > 1 ? ReadDuplicates(invocation.Arguments[1]) : Duplicates.UseFirst;

        var merged = XdmMap.Empty;
        for (var i = 0; i < maps.Count; i++)
        {
            if (!maps[i].TryGetFunction(out var function) || function is not XdmMap map)
            {
                throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
            }

            merged = i == 0 ? map : Merge(merged, map, duplicates);
        }

        invocation.Result = merged.ToSequence();
        return XPathEvaluationStatus.Ok;
    }

    private static XdmMap Merge(XdmMap target, XdmMap source, Duplicates duplicates)
    {
        foreach (var entry in source.Entries())
        {
            if (!target.TryGetEntry(entry.Key, out var existing))
            {
                target = target.Put(entry.Key, entry.Value);
                continue;
            }

            switch (duplicates)
            {
                case Duplicates.Reject:
                    throw XPathErrors.Exception(XPathErrorCodes.FOJS0003);
                case Duplicates.UseLast:
                    target = target.Put(entry.Key, entry.Value);
                    break;
                case Duplicates.Combine:
                    target = target.Put(existing.Key, XdmSequence.Concat(existing.Value, entry.Value));
                    break;
                default:
                    break;
            }
        }

        return target;
    }

    private static Duplicates ReadDuplicates(in XdmSequence options)
    {
        if (options.Count == 0)
        {
            return Duplicates.UseFirst;
        }

        if (!XdmMap.TryRead(options, out var map))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }

        if (!map!.TryGet(DuplicatesKey, out var value))
        {
            return Duplicates.UseFirst;
        }

        if (value.Count != 1 || !(value[0].TryGetString(out var text) || value[0].TryGetUntypedAtomic(out text)))
        {
            throw XPathErrors.Exception(XPathErrorCodes.XPTY0004);
        }

        return text switch
        {
            "use-first" or "use-any" => Duplicates.UseFirst,
            "use-last" => Duplicates.UseLast,
            "combine" => Duplicates.Combine,
            "reject" => Duplicates.Reject,
            _ => throw XPathErrors.Exception(XPathErrorCodes.FOJS0005),
        };
    }

    private enum Duplicates
    {
        UseFirst = 0,
        UseLast = 1,
        Combine = 2,
        Reject = 3,
    }
}
