namespace NetEvolve.Schemas.Schematron.Model;

/// <summary>The edition of ISO/IEC 19757-3 a schema is written for.</summary>
public enum SchematronEdition
{
    /// <summary>The 2016 edition; the value when <c>schematronEdition</c> is absent or <c>2016</c>.</summary>
    Iso2016 = 0,

    /// <summary>The 2025 edition.</summary>
    Iso2025 = 1,
}
