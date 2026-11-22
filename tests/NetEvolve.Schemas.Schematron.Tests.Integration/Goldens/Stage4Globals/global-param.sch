<?xml version="1.0" encoding="UTF-8"?>
<!-- Stage 4 golden schema: global xsl:param declarations with default values, used in rule contexts and in assertion messages. -->
<sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" queryBinding="xslt2">
  <xsl:param name="minimum" select="10"/>
  <xsl:param name="label" select="'item'"/>
  <sch:pattern id="quantities">
    <sch:rule id="small-quantity" context="*[local-name() = $label][@quantity &lt; number($minimum)]">
      <sch:assert id="small-quantity-rejected" test="false()">The quantity must reach <sch:value-of select="$minimum"/>.</sch:assert>
    </sch:rule>
    <sch:rule id="enough-quantity" context="*[local-name() = $label][@quantity &gt;= number($minimum)]">
      <sch:assert id="enough-quantity-priced" test="@price">An item of <sch:value-of select="$minimum"/> or more needs a price.</sch:assert>
    </sch:rule>
  </sch:pattern>
</sch:schema>
