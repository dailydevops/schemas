namespace NetEvolve.Schemas.Schematron.Xslt.Instructions.Grouping;

using System;
using System.Collections.Generic;
using NetEvolve.Arguments;
using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.Schematron.Xslt.Instructions.Flow;
using NetEvolve.Schemas.Schematron.Xslt.Keys;
using NetEvolve.Schemas.XPath.Xdm.Nodes;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// How <c>xsl:for-each-group</c> assigns the items of the population to groups (XSLT 2.0 section 14.3): by the grouping keys of the
/// items, by the keys of adjacent items, or by a pattern that starts or ends a group. A grouping appends the groups to a list in the
/// order of first appearance.
/// </summary>
/// <remarks>
/// Limitation: grouping keys are compared as <c>xsl:key</c> values are (<see cref="XsltKeyValue"/>): the codepoint collation for strings,
/// the value of <c>eq</c> for numbers and booleans with NaN equal to itself, and a string never equals a number. A key of another type
/// (date, time, duration, QName) is reported as not supported (#778). Only the codepoint collation is available (DEF-43, #55).
/// </remarks>
internal abstract class XsltGrouping
{
    // The key text of NaN, which grouping treats as equal to itself; the texts of XsltKeyValue start with S, N, or B.
    private const string NaNKey = "X";

    /// <summary>Initializes a new instance of the <see cref="XsltGrouping"/> class.</summary>
    /// <param name="location">The location of the instruction, for a problem.</param>
    protected XsltGrouping(DocumentLocation location) => Location = location;

    /// <summary>Gets the location of the instruction, for a problem.</summary>
    protected DocumentLocation Location { get; }

    /// <summary>Gets the variable slots that the grouping reads.</summary>
    internal abstract ReadOnlySpan<int> ReferencedSlots { get; }

    /// <summary>Appends the groups of the population in the order of first appearance.</summary>
    /// <param name="frame">The frame; it evaluates with its run and records the problem the grouping found.</param>
    /// <param name="items">The population; it answers <c>last()</c> for the key expressions.</param>
    /// <param name="groups">The list that receives the groups.</param>
    /// <returns><see cref="QueryStatus.Problem"/> when an evaluation failed or the population breaks a rule of the instruction.</returns>
    /// <exception cref="OperationCanceledException">The token of the frame is canceled.</exception>
    internal abstract QueryStatus TryGroup(XsltFrame frame, ItemBuffer items, List<XsltGroup> groups);

    /// <summary>Records a problem of the grouping on the frame.</summary>
    /// <param name="frame">The frame.</param>
    /// <param name="code">The code.</param>
    /// <param name="message">The message.</param>
    /// <param name="constraint">The code of the XSLT rule, or <see langword="null"/>.</param>
    /// <returns><see cref="QueryStatus.Problem"/>.</returns>
    protected QueryStatus Fail(XsltFrame frame, DiagnosticCode code, string message, string? constraint = null) =>
        frame.Fail(new SchematronProblem(code, message, Location, constraint));

    /// <summary>
    /// Atomizes a key value, casts an untyped value to a string (XSLT 2.0 section 14.3), and turns it into the text under which keys are
    /// equal when they are equal by <c>eq</c>.
    /// </summary>
    /// <param name="frame">The frame, which receives the problem.</param>
    /// <param name="item">The value that the key expression gave.</param>
    /// <param name="atom">The atomic key.</param>
    /// <param name="text">The key text.</param>
    /// <returns><see cref="QueryStatus.Problem"/> when the type of the key is not supported.</returns>
    protected QueryStatus TryKey(XsltFrame frame, in XdmItem item, out XdmItem atom, out string text)
    {
        atom = item.TryGetNode(out var node) ? node.TypedValue() : item;
        if (atom.TryGetUntypedAtomic(out var untyped))
        {
            atom = XdmItem.FromString(untyped!);
        }

        var status = XsltKeyValue.TryGetKey(in atom, out text);
        if (status == XsltKeyValueStatus.Unsupported)
        {
            return Fail(
                frame,
                SchematronDiagnosticCodes.EvaluationFailed,
                "The type of a grouping key is not supported as a key value (#778)."
            );
        }

        if (status == XsltKeyValueStatus.NeverEqual)
        {
            text = NaNKey;
        }

        return QueryStatus.Ok;
    }
}
