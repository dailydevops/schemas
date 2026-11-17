namespace NetEvolve.Schemas.Schematron.Seams;

/// <summary>The outcome of a hot-path seam call; the problem is read from the run only on failure.</summary>
internal enum QueryStatus : byte
{
    Ok = 0,
    Problem = 1,
}
