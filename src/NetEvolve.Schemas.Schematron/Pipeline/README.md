# Pipeline

The composition of the stages that turn a Schematron schema into a validator (SCH-55, #311; design #137 AP-26 and #150 EP-14). This is the
first point where Schematron validation runs end to end on the Xml and XPath engines.

```text
source ──> Parse ──> Resolve ──> Expand ──> Compile ──> CompiledProgram        (Build, BuildAsync)
           tree        include     abstract   binding      │
           xi:include  extends     rules and  phase        ▼
           model       library     patterns   scopes     ValidationRun          (Validate, ValidateAsync)
           edition                            rules       tree ─> evaluate ─> events ─> issues and caller sink
```

| File                          | Role                                                                                                         |
| ----------------------------- | ------------------------------------------------------------------------------------------------------------ |
| `SchematronPipeline.cs`       | `ISchematronPipeline`: runs the four stages in order; a stage with problems stops the later ones.            |
| `SchematronModelParser.cs`    | Stage Parse: Xml tree, `xi:include`, object model, edition.                                                  |
| `ModelExpander.cs`            | Stage Expand: abstract rules, then abstract patterns.                                                        |
| `SchematronSchemaCompiler.cs` | Stage Compile: binding, parameters, phase, scopes, rule sets.                                                |
| `PipelineBindings.cs`         | The registry of a build: the bindings with the XSLT 2.0 functions for the XSLT family.                       |
| `CompiledProgram.cs`          | The immutable result of the build; read by every run.                                                        |
| `CompilingBinding.cs`         | The binding of a build: forwards, counts every compiled expression, and collects the names they mention.   |
| `ValidationContextPool.cs`    | Pooled per-validation contexts: a name table that already holds the names of the schema (`ValidationContext.cs`). |
| `ValidationRun.cs`            | One validation call: instance tree, run of the binding, evaluation, events.                                  |
| `InstanceReader.cs`           | Reads an instance through the loader of the Xml package, for every input kind.                               |
| `XmlIssues.cs`                | Reader options from `ValidationLimits`, and the issue of a failed read.                                      |
| `BuildProblems.cs`            | The problems of a build as the issues of one `SchemaCompilationException`.                                   |

A schema is compiled once, at `Build`: the compiled program is never written afterwards, so any number of threads validate with one validator.
No expression is compiled during a validation (a test counts the compile calls of the binding). What a validation changes lives in a
context from `ValidationContextPool`: the name table the instance is read into, seeded with the names the expressions of the schema mention, so
those names are the same string instances in every tree read through it. A context whose table grew by more than 4096 names is dropped instead of
pooled.

The instance is read only through the readers of the Xml package (an architecture test pins it), so there is no second parse path; a
`Stream` is read into a pooled buffer by the base of the validator.

## Limitations

- **XSLT declarations are compiled but not run.** `xsl:include` and `xsl:import` in a schema of the XSLT family are linked in the resolve
  stage, and `xsl:function`, `xsl:key`, `xsl:variable`, `xsl:param`, and `xsl:decimal-format` are compiled; the compiled program holds the
  tables, and stylesheet functions are callable from the XPath of a rule, a variable, and another function body (#628), and the function `key` reads the `xsl:key` declarations (#776). Any other `xsl:` element is reported as `NES3204` at `Build`. The
  functions `current`, `system-property`, `function-available`, `format-number`, and `generate-id` are available; `regex-group` is not (#287). A call of a stylesheet function compiles through one signature-only host function per name and arity, resolved when the call runs. A run starts with an empty set of decimal formats, so a named format is unknown.
- **Locations of expression problems are approximate.** The binding seam hands the binding the text of an expression but not the location of
  the construct that holds it, so a problem is located by the base URI of the schema and by the line and column within the expression text;
  a dynamic problem has no line and column.
- **The schema is bounded by `MaxResourceSize`** (provisional, design #137 OQ-6; the alternative is `MaxInputSize`), and the Xml reader
  keeps its default limits for the schema; the nesting limit applies after inclusion.
- **The names of a schema are read lexically.** The names pre-interned in a context come from a scan of the expression text, not from the parsed
  expression, so a function or axis name is interned too; this costs a few names and nothing else.
- **`last()` in a predicate without a known size** equals `position()`.
