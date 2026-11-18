namespace NetEvolve.Schemas.Schematron.Xslt.Functions;

using NetEvolve.Schemas.Schematron.Seams;
using NetEvolve.Schemas.XPath.Xdm.Values;

/// <summary>
/// What a run offers to the calls of stylesheet functions besides evaluating: bind an evaluated value to a variable slot, read a slot
/// back to restore it, and enter and leave the scope of a function body (design #150 EP-5 and EP-13). The slots are one table
/// shared by every activation, so a call saves the values of the slots it uses and restores them when it ends. The slots of a
/// function start above every slot of the compiled schema (<c>ScopePlan.SlotCount</c>), so the slot table of the run must hold more
/// slots than the plan declares.
/// </summary>
/// <remarks>
/// Limitation: <see cref="IQueryRun"/> binds only the value of an expression, and a stylesheet function receives its arguments as
/// evaluated values, so the run of a binding implements this interface too. The run of the XPath binding does not exist yet (XP-19,
/// #297, and the typed host state P-X4 of design #150); until then only the test run implements it.
/// </remarks>
internal interface IXsltFunctionRun : IQueryRun
{
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
