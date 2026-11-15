# NetEvolve.Schemas

Format-agnostic, span-based .NET libraries for reading, validating, and querying XML-based schema formats such as XSD and Schematron.

## Status

In development. No package has been released yet.

## Planned packages (provisional)

Provisional names derived from the planned layer order `Schemas <- Xml <- XPath <- {Schematron, Xsd}` with dependency injection on top. They are not final until the first release.

- `NetEvolve.Schemas`: shared abstractions and diagnostics.
- `NetEvolve.Schemas.Xml`: XML reader and document model.
- `NetEvolve.Schemas.XPath`: XPath evaluation.
- `NetEvolve.Schemas.Schematron`: Schematron validation.
- `NetEvolve.Schemas.Xsd`: XSD validation.
- `NetEvolve.Schemas.DependencyInjection`: dependency injection integration.

## License

[MIT](LICENSE)
