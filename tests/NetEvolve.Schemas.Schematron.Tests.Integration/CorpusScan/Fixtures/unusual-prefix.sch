<?xml version="1.0" encoding="utf-8"?>
<sch:schema xmlns:sch="http://purl.oclc.org/dsdl/schematron" queryBinding="xslt2">
  <sch:pattern>
    <sch:rule context="/">
      <sch:assert test="count(*) &gt; 0 and string-length(name()) &gt; 0">The document must not be empty.</sch:assert>
    </sch:rule>
  </sch:pattern>
  <q:function xmlns:q="http://www.w3.org/1999/XSL/Transform" xmlns:my="urn:example:my" xmlns:xs="http://www.w3.org/2001/XMLSchema" name="my:upper" as="xs:string">
    <q:param name="value" as="xs:string"/>
    <q:value-of select="upper-case($value)"/>
  </q:function>
  <other:function xmlns:other="urn:example:other" name="ignored"/>
  <function xmlns="urn:example:other" name="ignored"/>
</sch:schema>
