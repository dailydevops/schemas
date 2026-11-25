<?xml version="1.0" encoding="UTF-8"?>
<!-- Stage 5 golden schema: xsl:for-each-group with group-ending-with inside an xsl:function; every total ends a block. -->
<sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:f="urn:f" queryBinding="xslt2">
  <sch:ns prefix="f" uri="urn:f"/>
  <xsl:function name="f:unclosed" as="xs:integer*">
    <xsl:param name="items" as="element()*"/>
    <xsl:for-each-group select="$items" group-ending-with="total">
      <xsl:if test="not(current-group()[last()][self::total])">
        <xsl:sequence select="count(current-group())"/>
      </xsl:if>
    </xsl:for-each-group>
  </xsl:function>
  <sch:pattern id="blocks">
    <sch:rule id="ledger-blocks" context="ledger">
      <sch:assert id="block-closed" test="empty(f:unclosed(*))">Every block of entries must end with a total.</sch:assert>
    </sch:rule>
  </sch:pattern>
</sch:schema>
