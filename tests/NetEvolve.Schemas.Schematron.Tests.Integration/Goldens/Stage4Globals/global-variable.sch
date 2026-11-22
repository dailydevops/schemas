<?xml version="1.0" encoding="UTF-8"?>
<!-- Stage 4 golden schema: global xsl:variable declarations used in rule contexts and in assertion tests. -->
<sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" queryBinding="xslt2">
  <xsl:variable name="limit" select="100"/>
  <xsl:variable name="threshold" select="$limit * 2"/>
  <xsl:variable name="currency" select="'EUR'"/>
  <sch:pattern id="limit">
    <sch:rule id="large-line" context="line[@amount &gt; $limit]">
      <sch:assert id="large-line-approved" test="@approved = 'yes'">A line above <sch:value-of select="$limit"/> must be approved.</sch:assert>
    </sch:rule>
  </sch:pattern>
  <sch:pattern id="threshold">
    <sch:rule id="very-large-line" context="line[@amount &gt; $threshold]">
      <sch:report id="very-large-line-reported" test="true()">A line above <sch:value-of select="$threshold"/> needs a second review.</sch:report>
    </sch:rule>
  </sch:pattern>
  <sch:pattern id="currency">
    <sch:rule id="foreign-currency" context="line[@currency != $currency]">
      <sch:assert id="foreign-currency-rejected" test="false()">A line must use <sch:value-of select="$currency"/>.</sch:assert>
    </sch:rule>
  </sch:pattern>
</sch:schema>
