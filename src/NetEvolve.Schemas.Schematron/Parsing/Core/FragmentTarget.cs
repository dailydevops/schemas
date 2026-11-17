namespace NetEvolve.Schemas.Schematron.Parsing.Core;

/// <summary>The place an included document replaces; it decides which root elements the document may have.</summary>
internal enum FragmentTarget
{
    /// <summary>An <c>include</c> in <c>schema</c> or <c>library</c>: a <c>library</c>, <c>pattern</c>, or <see langword="group"/> root.</summary>
    Container = 0,

    /// <summary>An <c>include</c> in <c>pattern</c> or <see langword="group"/>: a <c>rule</c> root.</summary>
    Pattern,

    /// <summary>An <c>include</c> in <c>rule</c>: an <c>assert</c>, <c>report</c>, or <c>p</c> root.</summary>
    Rule,

    /// <summary>An <c>extends</c> with <c>href</c>: a <c>library</c> root.</summary>
    Library,
}
