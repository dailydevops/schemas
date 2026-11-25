namespace NetEvolve.Schemas.Xml.Regex.Contracts;

/// <summary>The regex dialect: the tree is shared, the dialect decides which node kinds are legal and how a match is made.</summary>
public enum RegexDialect
{
    /// <summary>The XSD <c>pattern</c> dialect: the whole input must match, with no back references, and <c>^</c> and <c>$</c> are literal characters.</summary>
    Xsd = 0,

    /// <summary>The XPath dialect of the Functions and Operators regular expressions: a search, with anchors and back references.</summary>
    XPath = 1,
}
