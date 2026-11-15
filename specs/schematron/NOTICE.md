# Vendored Schematron and SVRL schemas

The files `schematron.rnc`, `schematron.sch`, `svrl.rnc`, and `svrl.sch` are
copied unchanged from the official Schematron project repository.

- Source: https://github.com/schematron/schema
- Branch: `2025-edition`
- Revision (commit SHA): `f12483d056f97feac7630dea1949449d72741271`
- Revision date: 2025-04-01 (commit message: "[FIX] changes to reflect DIS comments")
- The repository has no release tags, so the commit is pinned.

This is the state of the upstream `2025-edition` branch as of April 2025
(changes to reflect DIS comments). That state does not by itself prove that it
equals the final ISO/IEC 19757-3:2025 text.

`checksums.txt` lists the SHA-256 checksum of every file in this folder except
itself (`sha256sum` format). Unit tests verify the checksums and that no
unlisted file exists.

The text of ISO/IEC 19757-3 itself is not part of this repository.

## License

The upstream `LICENSE` file, reproduced verbatim:

```text
Copyright © ISO/IEC 2017
The following permission notice and disclaimer shall be included in all 
copies of this XML schema ("the Schema"), and derivations of the Schema: 

Permission is hereby granted, free of charge in perpetuity, to any 
person obtaining a copy of the Schema, to use, copy, modify, merge and 
distribute free of charge, copies of the Schema for the purposes of 
developing, implementing, installing and using software based on the 
Schema, and to permit persons to whom the Schema is furnished to do so, 
subject to the following conditions: 

THE SCHEMA IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL 
THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR 
OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
ARISING FROM, OUT OF OR IN CONNECTION WITH THE SCHEMA OR THE USE OR 
OTHER DEALINGS IN THE SCHEMA. 

In addition, any modified copy of the Schema shall include the following 
notice: 

"THIS SCHEMA HAS BEEN MODIFIED FROM THE SCHEMA DEFINED IN ISO/IEC 19757 3, 
AND SHOULD NOT BE INTERPRETED AS COMPLYING WITH THAT STANDARD". 
```
