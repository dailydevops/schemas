namespace NetEvolve.Schemas.Schematron.Seams;

/// <summary>Answers the size of a sequence focus when <c>last()</c> asks for it (design #150 RD-7): the focus carries -1 until then.</summary>
internal interface IFocusSizeSource
{
    /// <summary>Gets the number of items of the sequence the focus iterates.</summary>
    /// <returns>The size.</returns>
    int GetSize();
}
