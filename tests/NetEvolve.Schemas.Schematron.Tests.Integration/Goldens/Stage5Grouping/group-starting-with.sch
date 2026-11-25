<?xml version="1.0" encoding="UTF-8"?>
<!-- Stage 5 golden schema: xsl:for-each-group with group-starting-with inside an xsl:function; every heading starts a new chunk. -->
<sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:f="urn:f" queryBinding="xslt2">
  <sch:ns prefix="f" uri="urn:f"/>
  <xsl:function name="f:chunk-sizes" as="xs:integer*">
    <xsl:param name="items" as="element()*"/>
    <xsl:for-each-group select="$items" group-starting-with="heading">
      <xsl:sequence select="count(current-group())"/>
    </xsl:for-each-group>
  </xsl:function>
  <sch:pattern id="chunks">
    <sch:rule id="section-chunks" context="section">
      <sch:assert id="chunk-not-empty" test="min(f:chunk-sizes(*)) ge 2">Every heading must be followed by at least one paragraph.</sch:assert>
    </sch:rule>
  </sch:pattern>
</sch:schema>
