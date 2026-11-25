<?xml version="1.0" encoding="UTF-8"?>
<!-- Stage 5 golden schema: xsl:for-each-group with group-by inside an xsl:function, using current-group() and current-grouping-key(). -->
<sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:f="urn:f" queryBinding="xslt2">
  <sch:ns prefix="f" uri="urn:f"/>
  <xsl:function name="f:group-sizes" as="xs:integer*">
    <xsl:param name="lines" as="element()*"/>
    <xsl:for-each-group select="$lines" group-by="@category">
      <xsl:sequence select="count(current-group())"/>
    </xsl:for-each-group>
  </xsl:function>
  <xsl:function name="f:categories" as="xs:string*">
    <xsl:param name="lines" as="element()*"/>
    <xsl:for-each-group select="$lines" group-by="@category">
      <xsl:sequence select="string(current-grouping-key())"/>
    </xsl:for-each-group>
  </xsl:function>
  <sch:pattern id="categories">
    <sch:rule id="order-lines" context="order">
      <sch:assert id="category-size" test="max(f:group-sizes(line)) le 2">A category may hold at most two lines.</sch:assert>
    </sch:rule>
  </sch:pattern>
  <sch:pattern id="category-count">
    <sch:rule id="order-categories" context="order[@max-categories]">
      <sch:assert id="category-count" test="count(f:categories(line)) le number(@max-categories)">An order may use at most <sch:value-of select="@max-categories"/> categories.</sch:assert>
    </sch:rule>
  </sch:pattern>
</sch:schema>
