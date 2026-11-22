<?xml version="1.0" encoding="UTF-8"?>
<!-- Stage 4 golden schema: temporary trees built by a global xsl:variable and a global xsl:param with element content, navigated by path. -->
<sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" queryBinding="xslt2">
  <xsl:variable name="catalog">
    <entry code="A"/>
    <entry code="B"/>
    <entry code="C"/>
  </xsl:variable>
  <xsl:param name="retired">
    <entry code="C"/>
  </xsl:param>
  <sch:pattern id="catalog">
    <sch:rule id="item-code" context="item">
      <sch:assert id="item-code-known" test="@code = $catalog/entry/@code">The code <sch:value-of select="@code"/> is not in the catalog.</sch:assert>
    </sch:rule>
  </sch:pattern>
  <sch:pattern id="retirement">
    <sch:rule id="retired-item" context="item[@code = $retired/entry/@code]">
      <sch:report id="retired-item-reported" test="true()">The code <sch:value-of select="@code"/> is retired.</sch:report>
    </sch:rule>
  </sch:pattern>
</sch:schema>
