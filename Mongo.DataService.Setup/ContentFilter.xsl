<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:wix="http://schemas.microsoft.com/wix/2006/wi">

  <xsl:output method="xml" encoding="UTF-8" indent="yes" />

  <!-- Identity template -->
  <xsl:template match="@* | node()">
    <xsl:copy>
      <xsl:apply-templates select="@* | node()" />
    </xsl:copy>
  </xsl:template>

  <!-- exclude files -->
  <xsl:template match="wix:Component[wix:File/@Source='$(var.Mongo.DataService.ProjectDir)\Web.Debug.config']" >
    <xsl:value-of select="parent" />
  </xsl:template>
  <xsl:template match="wix:Component[wix:File/@Source='$(var.Mongo.DataService.ProjectDir)\Web.Release.config']" >
    <xsl:value-of select="parent" />
  </xsl:template>
  <xsl:template match="wix:Component[wix:File/@Source='$(var.Mongo.DataService.ProjectDir)\..\ConnectionStrings.config']" >
    <xsl:value-of select="parent" />
  </xsl:template>

</xsl:stylesheet>
