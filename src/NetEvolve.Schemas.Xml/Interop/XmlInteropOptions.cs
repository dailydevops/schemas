namespace NetEvolve.Schemas.Xml.Interop;

/// <summary>Options of the interop adapters.</summary>
public sealed class XmlInteropOptions
{
    /// <summary>Initializes a new instance of the <see cref="XmlInteropOptions"/> class.</summary>
    /// <param name="allowDocumentType"><see langword="true"/> to accept input that carries a document type declaration.</param>
    public XmlInteropOptions(bool allowDocumentType = false) => AllowDocumentType = allowDocumentType;

    /// <summary>Gets a value indicating whether input with a document type declaration is accepted; by default it is rejected.</summary>
    public bool AllowDocumentType { get; }
}
