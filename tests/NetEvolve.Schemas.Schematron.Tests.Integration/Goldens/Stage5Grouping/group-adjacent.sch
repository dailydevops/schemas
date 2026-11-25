<?xml version="1.0" encoding="UTF-8"?>
<!-- Stage 5 golden schema: xsl:for-each-group with group-adjacent inside an xsl:function; only adjacent items form a group. -->
<sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:f="urn:f" queryBinding="xslt2">
  <sch:ns prefix="f" uri="urn:f"/>
  <xsl:function name="f:run-lengths" as="xs:integer*">
    <xsl:param name="steps" as="element()*"/>
    <xsl:for-each-group select="$steps" group-adjacent="@status">
      <xsl:sequence select="count(current-group())"/>
    </xsl:for-each-group>
  </xsl:function>
  <sch:pattern id="runs">
    <sch:rule id="workflow-runs" context="workflow">
      <sch:assert id="run-length" test="max(f:run-lengths(step)) le 2">No status may repeat more than twice in a row.</sch:assert>
    </sch:rule>
  </sch:pattern>
</sch:schema>
