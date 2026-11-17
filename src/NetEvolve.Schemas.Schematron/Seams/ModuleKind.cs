namespace NetEvolve.Schemas.Schematron.Seams;

/// <summary>What a module reference points at.</summary>
internal enum ModuleKind : byte
{
    SchematronInclude = 0,
    SchematronExtends = 1,
    XsltInclude = 2,
    XsltImport = 3,
}
