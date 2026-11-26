namespace NetEvolve.Schemas.Schematron.Seams;

using NetEvolve.Schemas.XPath.Abstractions;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// What a run offers to the calls of stylesheet functions besides evaluating: bind an evaluated value to a variable slot, read a slot
/// back to restore it, and enter and leave the scope of a function body (design #150 EP-5 and EP-13). The slots are one table
/// shared by every activation, so a call saves the values of the slots it uses and restores them when it ends. The slots of a
/// function start above every slot of the compiled schema (<c>ScopePlan.SlotCount</c>), so the slot table of the run must hold more
/// slots than the plan declares.
/// </summary>
/// <remarks>
/// <see cref="IQueryRun"/> binds only the value of an expression, and a stylesheet function receives its arguments as evaluated
/// values, so the run of a binding implements this interface too; the run of the XPath binding reaches the calls through the host
/// state slot (P-X4 of design #150).
/// <para>
/// Contract of the host-function adapter: a stylesheet function is called from inside an evaluation, through
/// <c>XsltFunctionCaller.TryCall</c>. When the call fails, the problem of the callee (<c>XsltFunctionCaller.GetProblem</c>) must
/// become the problem of the run, so the <see cref="IQueryRun.GetProblem"/> of the outermost evaluation reports the nested failure
/// with its own code and location and not a generic one; the host function of a stylesheet function hands it to
/// <see cref="FailWith"/>.
/// </para>
/// </remarks>
internal interface IXsltFunctionRun : IQueryRun
{
    /// <summary>
    /// Gets or sets the calls of the stylesheet functions of this run, or <see langword="null"/> when the schema declares no function.
    /// The run hands the value to the host functions of the expressions it evaluates (the host state slot of the XPath package); the
    /// type of the value is the concern of the XSLT library, which the layering rule keeps out of reach of a binding.
    /// </summary>
    object? FunctionCalls { get; set; }

    /// <summary>
    /// Gets or sets the key provider of this run, or <see langword="null"/> when the schema declares no <c>xsl:key</c>. The run hands
    /// it to the keys host slot of the expressions it evaluates, which the XPath function <c>key</c> reads (#776).
    /// </summary>
    IXPathKeyProvider? Keys { get; set; }

    /// <summary>
    /// Makes the problem of a failed call the problem of the run: the <see cref="IQueryRun.GetProblem"/> of the evaluation that made
    /// the call reports it, and not a generic one.
    /// </summary>
    /// <param name="problem">The problem of the callee.</param>
    void FailWith(in SchematronProblem problem);

    /// <summary>Binds an evaluated value to a slot.</summary>
    /// <param name="slot">The slot.</param>
    /// <param name="value">The value.</param>
    void SetValue(int slot, in XdmSequence value);

    /// <summary>Reads the value of a slot.</summary>
    /// <param name="slot">The slot.</param>
    /// <param name="value">The value when the slot is bound.</param>
    /// <returns><see langword="true"/> when the slot is bound.</returns>
    bool TryGetValue(int slot, out XdmSequence value);

    /// <summary>Makes a slot unbound again.</summary>
    /// <param name="slot">The slot.</param>
    void ClearValue(int slot);

    /// <summary>
    /// Enters the body of a function: the run saves and clears the focus, the current item, the current group, the grouping key, and
    /// the regex groups of the caller, because the focus is undefined in a function body (XSLT 2.0 section 10.3).
    /// </summary>
    void EnterFunction();

    /// <summary>Leaves the body of a function and restores what <see cref="EnterFunction"/> saved.</summary>
    void ExitFunction();
}
