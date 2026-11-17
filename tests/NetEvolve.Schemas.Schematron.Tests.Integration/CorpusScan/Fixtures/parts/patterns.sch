<?xml version="1.0" encoding="utf-8"?>
<pattern xmlns="http://purl.oclc.org/dsdl/schematron" xmlns:xi="http://www.w3.org/2001/XInclude">
  <xi:include href="functions.xml"/>
  <rule context="item">
    <assert test="exists(@id)">An item needs an id.</assert>
  </rule>
</pattern>
