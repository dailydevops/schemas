#pragma warning disable NE0002 // Approved namespace NetEvolve.Schemas for format entry points (design #103 AP-2, #146 DP-1)

namespace NetEvolve.Schemas;

using System.Threading;
using NetEvolve.Schemas.Xml.Contracts;
using NetEvolve.Schemas.Xml.Datatypes.Contracts;
using NetEvolve.Schemas.Xsd;

/// <summary>An immutable, thread-safe validator for XML Schema documents. Not intended for implementation by consumers.</summary>
public interface IXsdValidator : ISchemaValidator
{
    /// <summary>Gets the XSD version the validator was built for.</summary>
    XsdVersion Version { get; }

    /// <summary>
    /// Validates a document of the document model and returns the issues with the annotated tree (XSD-79, #361). The model-to-sink pump
    /// feeds the same validator core as every other input kind, so the issues are those of the streaming path for the same content.
    /// </summary>
    /// <param name="document">The tree to validate. It is not modified.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>The issues and a copy of the document whose nodes carry the annotations.</returns>
    /// <remarks>
    /// <para>
    /// Locations take the line and the column that the tree keeps for an element, and <see cref="DocumentLocation.Source"/> is the
    /// original string of the document URI of the tree.
    /// </para>
    /// <para>
    /// The annotated tree is a copy of the validated content that also holds what the schema adds (design #146 DP-19): the defaulted
    /// attributes are attribute nodes after the specified ones, and the default or fixed value of an empty element is its text. A
    /// defaulted attribute reports <see cref="XmlNode.IsSpecified"/> as <see langword="false"/>.
    /// </para>
    /// <para>
    /// Limitations: the model holds no unparsed entities and no document type declaration (#731), so
    /// a case that depends on them can differ from the streaming path. The assertion evaluation does not carry the annotation source yet (#318), so typed values
    /// of the annotated tree are read through the annotations, not through <c>fn:data</c> inside an expression. An issue of an
    /// attribute belongs to the element that owns it.
    /// </para>
    /// </remarks>
    /// <exception cref="System.ArgumentNullException"><paramref name="document"/> is <see langword="null"/>.</exception>
    /// <exception cref="System.OperationCanceledException">The token was canceled.</exception>
    XsdAnnotatedResult ValidateTree(XmlTree document, CancellationToken cancellationToken = default);

    /// <summary>Validates the subtree that starts at an element of a document of the document model and returns the issues with the annotated subtree.</summary>
    /// <param name="document">The tree that holds the element. It is not modified.</param>
    /// <param name="element">The element that roots the subtree; it must belong to <paramref name="document"/>.</param>
    /// <param name="cancellationToken">A token to cancel the run.</param>
    /// <returns>The issues and a copy of the subtree, as a document whose only child is the element.</returns>
    /// <remarks>
    /// The issues equal those of a document that contains only the subtree, with the namespaces in scope of the element declared on its
    /// root. The limitations are those of <see cref="ValidateTree(XmlTree, CancellationToken)"/>.
    /// </remarks>
    /// <exception cref="System.ArgumentNullException"><paramref name="document"/> is <see langword="null"/> or <paramref name="element"/> is <see langword="default"/>.</exception>
    /// <exception cref="System.ArgumentException"><paramref name="element"/> belongs to another tree or is not an element.</exception>
    /// <exception cref="System.OperationCanceledException">The token was canceled.</exception>
    XsdAnnotatedResult ValidateTree(XmlTree document, XmlNode element, CancellationToken cancellationToken = default);
}
