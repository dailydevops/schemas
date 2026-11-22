namespace NetEvolve.Schemas.XPath.Tests.Integration.Qt3.Selections;

/// <summary>What an exclusion entry matches.</summary>
internal enum Qt3ExclusionKind
{
    /// <summary>A dependency of the catalog; the key is <c>&lt;type&gt; &lt;value&gt;</c>, for example <c>feature schemaImport</c>.</summary>
    Dependency,

    /// <summary>A whole test set; the key is the test set name.</summary>
    TestSet,

    /// <summary>A case whose query or expected result mentions a function or a collation; the key is a regular expression.</summary>
    Pattern,
}
