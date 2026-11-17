<?xml version="1.0" encoding="utf-8"?>
<schema xmlns="http://purl.oclc.org/dsdl/schematron" queryBinding="xslt2">
  <pattern>
    <rule context="/">
      <assert test="count(*) &gt; 0 and string-length(name()) &gt; 0">The document must not be empty.</assert>
    </rule>
  </pattern>
  <function xmlns="http://www.w3.org/1999/XSL/Transform" xmlns:my="urn:example:my" xmlns:xs="http://www.w3.org/2001/XMLSchema" name="my:upper" as="xs:string">
    <param name="value" as="xs:string"/>
    <value-of select="upper-case($value)"/>
  </function>
</schema>
