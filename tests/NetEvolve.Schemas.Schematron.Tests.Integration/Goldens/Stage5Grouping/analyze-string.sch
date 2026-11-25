<?xml version="1.0" encoding="UTF-8"?>
<!-- Stage 5 golden schema: xsl:analyze-string with regex-group inside an xsl:function; the month of every ISO date in a text must be 1 to 12. -->
<sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:f="urn:f" queryBinding="xslt2">
  <sch:ns prefix="f" uri="urn:f"/>
  <xsl:function name="f:months" as="xs:integer*">
    <xsl:param name="text" as="xs:string"/>
    <xsl:analyze-string select="$text" regex="([0-9]+)-([0-9]+)-([0-9]+)">
      <xsl:matching-substring>
        <xsl:sequence select="xs:integer(regex-group(2))"/>
      </xsl:matching-substring>
    </xsl:analyze-string>
  </xsl:function>
  <sch:pattern id="dates">
    <sch:rule id="note-dates" context="note">
      <sch:assert id="month-range" test="every $m in f:months(string(.)) satisfies $m ge 1 and $m le 12">Every date must use a month from 1 to 12.</sch:assert>
    </sch:rule>
  </sch:pattern>
</sch:schema>
