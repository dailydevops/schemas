namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Api;

/// <summary>
/// A compiled <c>xsl:function</c> (XSLT 2.0 section 10.3): the expanded name, the parameters, the result type, and the body. It is
/// immutable and holds no run state, so one component serves every run (design #150 EP-13). The variable slots of the parameters
/// and local variables start at <see cref="SlotBase"/>, above every slot of the schema, phase, pattern, and rule blocks.
/// </summary>
internal sealed class XsltFunctionComponent
{
    private readonly int[] _referencedSlots;

    internal XsltFunctionComponent(
        XPathQName name,
        string displayName,
        IReadOnlyList<XsltFunctionParameter> parameters,
        XsltSequenceType? resultType,
        XsltExecutable body,
        int slotBase,
        int slotCount,
        int[] referencedSlots,
        bool overridesImported,
        DocumentLocation location
    )
    {
        Argument.ThrowIfNull(displayName);
        Argument.ThrowIfNull(parameters);
        Argument.ThrowIfNull(body);
        Argument.ThrowIfLessThan(slotBase, 0);
        Argument.ThrowIfLessThan(slotCount, parameters.Count);
        Argument.ThrowIfNull(referencedSlots);

        Name = name;
        DisplayName = displayName;
        Parameters = parameters;
        ResultType = resultType;
        Body = body;
        SlotBase = slotBase;
        SlotCount = slotCount;
        _referencedSlots = referencedSlots;
        OverridesImported = overridesImported;
        Location = location;
    }

    /// <summary>Gets the expanded name.</summary>
    internal XPathQName Name { get; }

    /// <summary>Gets the name as written, for messages.</summary>
    internal string DisplayName { get; }

    /// <summary>Gets the number of parameters.</summary>
    internal int Arity => Parameters.Count;

    /// <summary>Gets the parameters in order.</summary>
    internal IReadOnlyList<XsltFunctionParameter> Parameters { get; }

    /// <summary>Gets the <see langword="as"/> type of the result, or <see langword="null"/> when the result is used as it is.</summary>
    internal XsltSequenceType? ResultType { get; }

    /// <summary>Gets the compiled sequence constructor.</summary>
    internal XsltExecutable Body { get; }

    /// <summary>Gets the first variable slot of the function.</summary>
    internal int SlotBase { get; }

    /// <summary>Gets the number of slots of the parameters and local variables.</summary>
    internal int SlotCount { get; }

    /// <summary>Gets the schema-level slots the body reads directly, which the call binds at entry.</summary>
    internal ReadOnlySpan<int> ReferencedSlots => _referencedSlots;

    /// <summary>Gets a value indicating whether the function overrides an imported one (<see langword="override"/> is yes, the default).</summary>
    internal bool OverridesImported { get; }

    /// <summary>Gets the location of the <c>xsl:function</c> element.</summary>
    internal DocumentLocation Location { get; }
}
