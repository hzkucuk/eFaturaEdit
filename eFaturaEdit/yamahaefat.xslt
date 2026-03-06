<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="2.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
	xmlns:cac="urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2"
	xmlns:cbc="urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2"
	xmlns:ccts="urn:un:unece:uncefact:documentation:2"
	xmlns:clm54217="urn:un:unece:uncefact:codelist:specification:54217:2001"
	xmlns:clm5639="urn:un:unece:uncefact:codelist:specification:5639:1988"
	xmlns:clm66411="urn:un:unece:uncefact:codelist:specification:66411:2001"
	xmlns:clmIANAMIMEMediaType="urn:un:unece:uncefact:codelist:specification:IANAMIMEMediaType:2003"
	xmlns:fn="http://www.w3.org/2005/xpath-functions" xmlns:link="http://www.xbrl.org/2003/linkbase"
	xmlns:n1="urn:oasis:names:specification:ubl:schema:xsd:Invoice-2"
	xmlns:qdt="urn:oasis:names:specification:ubl:schema:xsd:QualifiedDatatypes-2"
	xmlns:udt="urn:un:unece:uncefact:data:specification:UnqualifiedDataTypesSchemaModule:2"
	xmlns:xbrldi="http://xbrl.org/2006/xbrldi" xmlns:xbrli="http://www.xbrl.org/2003/instance"
	xmlns:xdt="http://www.w3.org/2005/xpath-datatypes" xmlns:xlink="http://www.w3.org/1999/xlink"
	xmlns:xs="http://www.w3.org/2001/XMLSchema" xmlns:xsd="http://www.w3.org/2001/XMLSchema"
	xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
	exclude-result-prefixes="cac cbc ccts clm54217 clm5639 clm66411 clmIANAMIMEMediaType fn link n1 qdt udt xbrldi xbrli xdt xlink xs xsd xsi">
  <xsl:character-map name="a">
    <xsl:output-character character="&#133;" string=""/>
    <xsl:output-character character="&#158;" string=""/>
		<xsl:output-character character="&#129;" string=""/>
		<xsl:output-character character="&#130;" string=""/>
		<xsl:output-character character="&#131;" string=""/>
		<xsl:output-character character="&#132;" string=""/>
		<xsl:output-character character="&#133;" string=""/>
		<xsl:output-character character="&#134;" string=""/>
		<xsl:output-character character="&#135;" string=""/>
		<xsl:output-character character="&#136;" string=""/>
		<xsl:output-character character="&#137;" string=""/>
		<xsl:output-character character="&#138;" string=""/>
		<xsl:output-character character="&#139;" string=""/>
		<xsl:output-character character="&#140;" string=""/>
		<xsl:output-character character="&#141;" string=""/>
		<xsl:output-character character="&#142;" string=""/>
		<xsl:output-character character="&#143;" string=""/>
		<xsl:output-character character="&#144;" string=""/>
		<xsl:output-character character="&#145;" string=""/>
		<xsl:output-character character="&#146;" string=""/>
		<xsl:output-character character="&#147;" string=""/>
		<xsl:output-character character="&#148;" string=""/>
		<xsl:output-character character="&#149;" string=""/>
		<xsl:output-character character="&#150;" string=""/>
		<xsl:output-character character="&#151;" string=""/>
		<xsl:output-character character="&#152;" string=""/>
		<xsl:output-character character="&#153;" string=""/>
		<xsl:output-character character="&#154;" string=""/>
		<xsl:output-character character="&#155;" string=""/>
		<xsl:output-character character="&#156;" string=""/>
		<xsl:output-character character="&#157;" string=""/>
		<xsl:output-character character="&#158;" string=""/>
		<xsl:output-character character="&#159;" string=""/>
  </xsl:character-map>
  <xsl:decimal-format name="european" decimal-separator="," grouping-separator="." NaN=""/>
  <xsl:output version="4.0" method="html" indent="no" encoding="UTF-8"
		doctype-public="-//W3C//DTD HTML 4.01 Transitional//EN"
		doctype-system="http://www.w3.org/TR/html4/loose.dtd" use-character-maps="a"/>
  <xsl:param name="SV_OutputFormat" select="'HTML'"/>
  <xsl:variable name="XML" select="/"/>


  <xsl:template match="/">
    <html>
      <head><title/>
        <style type="text/css">
          body {
          background-color: #FFFFFF;
          font-family: 'Tahoma', "Times New Roman", Times, serif;
          font-size: 11px;
          color: #666666;
          }
          h1, h2 {
          padding-bottom: 3px;
          padding-top: 3px;
          margin-bottom: 5px;
          text-transform: uppercase;
          font-family: Arial, Helvetica, sans-serif;
          }
          h1 {
          font-size: 1.4em;
          text-transform:none;
          }
          h2 {
          font-size: 1em;
          color: brown;
          }
          h3 {
          font-size: 1em;
          color: #333333;
          text-align: justify;
          margin: 0;
          padding: 0;
          }
          h4 {
          font-size: 1.1em;
          font-style: bold;
          font-family: Arial, Helvetica, sans-serif;
          color: #000000;
          margin: 0;
          padding: 0;
          }
          hr {
          height:2px;
          color: #000000;
          background-color: #000000;
          border-bottom: 1px solid #000000;
          }
          p, ul, ol {
          margin-top: 1.5em;
          }
          ul, ol {
          margin-left: 3em;
          }
          blockquote {
          margin-left: 3em;
          margin-right: 3em;
          font-style: italic;
          }
          a {
          text-decoration: none;
          color: #70A300;
          }
          a:hover {
          border: none;
          color: #70A300;
          }
          #despatchTable {
          border-collapse:collapse;
          font-size:11px;
          float:right;
          border-color:gray;
          }
          #ettnTable {
          border-collapse:collapse;
          font-size:11px;
          border-color:gray;
          }
          #customerPartyTable {
          border-width: 0px;
          border-spacing:;
          border-style: inset;
          border-color: gray;
          border-collapse: collapse;
          background-color:
          }
          #customerIDTable {
          border-width: 2px;
          border-spacing:;
          border-style: inset;
          border-color: gray;
          border-collapse: collapse;
          background-color:
          }
          #customerIDTableTd {
          border-width: 2px;
          border-spacing:;
          border-style: inset;
          border-color: gray;
          border-collapse: collapse;
          background-color:
          }
          #lineTable {
          border-width:2px;
          border-spacing:;
          border-style: inset;
          border-color: black;
          border-collapse: collapse;
          background-color:;
          }
          #lineTableTd {
          border-width: 1px;
          padding: 1px;
          border-style: inset;
          border-color: black;
          background-color: white;
          }
          #lineTableTr {
          border-width: 1px;
          padding: 0px;
          border-style: inset;
          border-color: black;
          background-color: white;
          -moz-border-radius:;
          }
          #lineTableDummyTd {
          border-width: 1px;
          border-color:white;
          padding: 1px;
          border-style: inset;
          border-color: black;
          background-color: white;
          }
          #lineTableBudgetTd {
          border-width: 2px;
          border-spacing:0px;
          padding: 1px;
          border-style: inset;
          border-color: black;
          background-color: white;
          -moz-border-radius:;
          }
          #notesTable {
          border-width: 2px;
          border-spacing:;
          border-style: inset;
          border-color: black;
          border-collapse: collapse;
          background-color:
          }
          #notesTableTd {
          border-width: 0px;
          border-spacing:;
          border-style: inset;
          border-color: black;
          border-collapse: collapse;
          background-color:
          }
          table {
          border-spacing:0px;
          }
          #budgetContainerTable {
          border-width: 0px;
          border-spacing: 0px;
          border-style: inset;
          border-color: black;
          border-collapse: collapse;
          background-color:;
          }
          td {
          border-color:gray;
          }
        </style>
        <title>e-Fatura</title>
      </head>
      <body
				style="margin-left=0.6in; margin-right=0.6in; margin-top=0.79in; margin-bottom=0.79in">
        <xsl:for-each select="$XML">
          <table style="border-color:blue; " border="0" cellspacing="0px" width="800"
						cellpadding="0px">
            <tbody>
              <tr valign="top">
                <td width="40%">
                  <br/>
                  <table align="center" border="0" width="100%">
                    <tbody>
                      <hr/>
                      <tr align="left">
                        <xsl:for-each select="n1:Invoice/cac:AccountingSupplierParty/cac:Party">
                          <td align="left">
                            <br/>
                        
                            <br/>
                            
                            <xsl:if test="cac:PartyName">
                              <xsl:value-of select="cac:PartyName/cbc:Name"/>
                              <br/>
                              
                            </xsl:if>
                            <xsl:for-each
                              select="cac:Person">
                              <xsl:for-each select="cbc:Title">
                                <xsl:apply-templates/>
                                <xsl:text>&#160;</xsl:text>
                              </xsl:for-each>
                              <xsl:for-each select="cbc:FirstName">
                                <xsl:apply-templates/>
                                <xsl:text>&#160;</xsl:text>
                              </xsl:for-each>
                              <xsl:for-each select="cbc:MiddleName">
                                <xsl:apply-templates/>
                                <xsl:text>&#160;</xsl:text>
                              </xsl:for-each>
                              <xsl:for-each select="cbc:FamilyName">
                                <xsl:apply-templates/>
                                <xsl:text>&#160;</xsl:text>
                              </xsl:for-each>
                              <xsl:for-each select="cbc:NameSuffix">
                                <xsl:apply-templates/>
                              </xsl:for-each>
                            </xsl:for-each>
                          </td>
                        </xsl:for-each>
                      </tr>
                      <tr align="left">
                        <xsl:for-each select="n1:Invoice/cac:AccountingSupplierParty/cac:Party">
                          <td align="left">
                            <xsl:for-each select="cac:PostalAddress">
                              <xsl:for-each select="cbc:StreetName">
                                <xsl:apply-templates/>
                                <xsl:text>&#160;</xsl:text>
                              </xsl:for-each>
                              <xsl:for-each select="cbc:BuildingName">
                                <xsl:apply-templates/>
                              </xsl:for-each>
                              <xsl:if test="cbc:BuildingNumber">
                                <xsl:text> No:</xsl:text>
                                <xsl:for-each select="cbc:BuildingNumber">
                                  <xsl:apply-templates/>
                                </xsl:for-each>
                                <xsl:text>&#160;</xsl:text>
                              </xsl:if>
                              <br/>
                              <xsl:for-each select="cbc:PostalZone">
                                <xsl:apply-templates/>
                                <xsl:text>&#160;</xsl:text>
                              </xsl:for-each>
                              <xsl:for-each select="cbc:CitySubdivisionName">
                                <xsl:apply-templates/>
                              </xsl:for-each>
                              <xsl:text>/ </xsl:text>
                              <xsl:for-each select="cbc:CityName">
                                <xsl:apply-templates/>
                                <xsl:text>&#160;</xsl:text>
                              </xsl:for-each>
                            </xsl:for-each>
                          </td>
                        </xsl:for-each>
                      </tr>
                      <xsl:if test="//n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:Contact/cbc:Telephone or //n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:Contact/cbc:Telefax">
                        <tr align="left">
                          <xsl:for-each select="n1:Invoice/cac:AccountingSupplierParty/cac:Party">
                            <td align="left">
                              <xsl:for-each select="cac:Contact">
                                <xsl:if test="cbc:Telephone">
                                  <xsl:text>Tel: </xsl:text>
                                  <xsl:for-each select="cbc:Telephone">
                                    <xsl:apply-templates/>
                                  </xsl:for-each>
                                </xsl:if>
                                <xsl:if test="cbc:Telefax">
                                  <xsl:text> Fax: </xsl:text>
                                  <xsl:for-each select="cbc:Telefax">
                                    <xsl:apply-templates/>
                                  </xsl:for-each>
                                </xsl:if>
                                <xsl:text>&#160;</xsl:text>
                              </xsl:for-each>
                            </td>
                          </xsl:for-each>
                        </tr>
                      </xsl:if>
                      <xsl:for-each select="//n1:Invoice/cac:AccountingSupplierParty/cac:Party/cbc:WebsiteURI">
                        <tr align="left">
                          <td>
                            <xsl:text>Web Sitesi: </xsl:text>
                            <xsl:value-of select="."/>
                          </td>
                        </tr>
                      </xsl:for-each>
                      <xsl:for-each select="//n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:Contact/cbc:ElectronicMail">
                        <tr align="left">
                          <td>
                            <xsl:text>E-Posta: </xsl:text>
                            <xsl:value-of select="."/>
                          </td>
                        </tr>
                      </xsl:for-each>
                      <tr align="left">
                        <xsl:for-each select="n1:Invoice/cac:AccountingSupplierParty/cac:Party">
                          <td align="left">
                            <xsl:text>Vergi Dairesi: </xsl:text>
                            <xsl:for-each select="cac:PartyTaxScheme">
                              <xsl:for-each select="cac:TaxScheme">
                                <xsl:for-each select="cbc:Name">
                                  <xsl:apply-templates/>
                                </xsl:for-each>
                              </xsl:for-each>
                              <xsl:text>&#160; </xsl:text>
                            </xsl:for-each>
                          </td>
                        </xsl:for-each>
                      </tr>
                      <xsl:for-each select="//n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyIdentification">
                        <tr align="left">
                          <td>
                            <xsl:value-of select="cbc:ID/@schemeID"/>
                            <xsl:text>: </xsl:text>
                            <xsl:value-of select="cbc:ID"/>
                          </td>
                        </tr>
                      </xsl:for-each>
                    </tbody>
                  </table>
                  <hr/>
                </td>
                <td width="20%" align="center" valign="middle">
                  <br/>
                  <br/>
                  <img style="width:91px;" align="middle" alt="E-Fatura Logo"
										src="data:image/jpeg;base64,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"/>

                  <h1 align="center">
                    <span style="font-weight:bold; ">
                      <xsl:text>e-FATURA</xsl:text>
                    </span>
                  </h1>
               
                </td>
                <td width="40%" style="text-align:center; vertical-align:middle">

              
                  <!-- FİRMA LOGONUZU BURAYA KOYABİLİRSİNİZ -->
                <div id="invoiceqrcode" class="qrkod">
                        <div id="qrcode" style="display: flex; justify-content: flex-end;"/>
                      <div id="qrvalue" style="visibility: hidden; display:none" >
                     		{"vkntckn":"<xsl:value-of select="n1:Invoice/cac:AccountingSupplierParty/cac:Party/cac:PartyIdentification/cbc:ID[@schemeID='TCKN' or @schemeID='VKN']"/>",
"avkntckn":"<xsl:value-of select="n1:Invoice/cac:AccountingCustomerParty/cac:Party/cac:PartyIdentification/cbc:ID[@schemeID='TCKN' or @schemeID='VKN']"/>",
"senaryo":"<xsl:value-of select="n1:Invoice/cbc:ProfileID"/>",
"tip":"<xsl:value-of select="n1:Invoice/cbc:InvoiceTypeCode"/>",
"tarih":"<xsl:value-of select="n1:Invoice/cbc:IssueDate"/>",
"no":"<xsl:value-of select="n1:Invoice/cbc:ID"/>",
"ettn":"<xsl:value-of select="n1:Invoice/cbc:UUID"/>",
"parabirimi":"<xsl:value-of select="n1:Invoice/cbc:DocumentCurrencyCode"/>",
"malhizmettoplam":"<xsl:value-of select="n1:Invoice/cac:LegalMonetaryTotal/cbc:LineExtensionAmount"/>",
<xsl:for-each select="n1:Invoice/cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:TaxTypeCode='0015']">
<xsl:text>"kdvmatrah</xsl:text>(<xsl:value-of select="cbc:Percent"/>)":"<xsl:value-of select="cbc:TaxableAmount"/>",</xsl:for-each>
<xsl:for-each select="n1:Invoice/cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:TaxTypeCode='0015']">
<xsl:text>"hesaplanankdv</xsl:text>(<xsl:value-of select="cbc:Percent"/>)":"<xsl:value-of select="cbc:TaxAmount"/>",
</xsl:for-each>"vergidahil":"<xsl:value-of select="n1:Invoice/cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount"/>",
"odenecek":"<xsl:value-of select="n1:Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount"/>"}
                        </div>
                      	<script type="text/javascript">
												<![CDATA[
													var QRCode;!function(){function a(a){this.mode=c.MODE_8BIT_BYTE,this.data=a,this.parsedData=[];for(var b=[],d=0,e=this.data.length;e>d;d++){var f=this.data.charCodeAt(d);f>65536?(b[0]=240|(1835008&f)>>>18,b[1]=128|(258048&f)>>>12,b[2]=128|(4032&f)>>>6,b[3]=128|63&f):f>2048?(b[0]=224|(61440&f)>>>12,b[1]=128|(4032&f)>>>6,b[2]=128|63&f):f>128?(b[0]=192|(1984&f)>>>6,b[1]=128|63&f):b[0]=f,this.parsedData=this.parsedData.concat(b)}this.parsedData.length!=this.data.length&&(this.parsedData.unshift(191),this.parsedData.unshift(187),this.parsedData.unshift(239))}function b(a,b){this.typeNumber=a,this.errorCorrectLevel=b,this.modules=null,this.moduleCount=0,this.dataCache=null,this.dataList=[]}function i(a,b){if(void 0==a.length)throw new Error(a.length+"/"+b);for(var c=0;c<a.length&&0==a[c];)c++;this.num=new Array(a.length-c+b);for(var d=0;d<a.length-c;d++)this.num[d]=a[d+c]}function j(a,b){this.totalCount=a,this.dataCount=b}function k(){this.buffer=[],this.length=0}function m(){return"undefined"!=typeof CanvasRenderingContext2D}function n(){var a=!1,b=navigator.userAgent;return/android/i.test(b)&&(a=!0,aMat=b.toString().match(/android ([0-9]\.[0-9])/i),aMat&&aMat[1]&&(a=parseFloat(aMat[1]))),a}function r(a,b){for(var c=1,e=s(a),f=0,g=l.length;g>=f;f++){var h=0;switch(b){case d.L:h=l[f][0];break;case d.M:h=l[f][1];break;case d.Q:h=l[f][2];break;case d.H:h=l[f][3]}if(h>=e)break;c++}if(c>l.length)throw new Error("Too long data");return c}function s(a){var b=encodeURI(a).toString().replace(/\%[0-9a-fA-F]{2}/g,"a");return b.length+(b.length!=a?3:0)}a.prototype={getLength:function(){return this.parsedData.length},write:function(a){for(var b=0,c=this.parsedData.length;c>b;b++)a.put(this.parsedData[b],8)}},b.prototype={addData:function(b){var c=new a(b);this.dataList.push(c),this.dataCache=null},isDark:function(a,b){if(0>a||this.moduleCount<=a||0>b||this.moduleCount<=b)throw new Error(a+","+b);return this.modules[a][b]},getModuleCount:function(){return this.moduleCount},make:function(){this.makeImpl(!1,this.getBestMaskPattern())},makeImpl:function(a,c){this.moduleCount=4*this.typeNumber+17,this.modules=new Array(this.moduleCount);for(var d=0;d<this.moduleCount;d++){this.modules[d]=new Array(this.moduleCount);for(var e=0;e<this.moduleCount;e++)this.modules[d][e]=null}this.setupPositionProbePattern(0,0),this.setupPositionProbePattern(this.moduleCount-7,0),this.setupPositionProbePattern(0,this.moduleCount-7),this.setupPositionAdjustPattern(),this.setupTimingPattern(),this.setupTypeInfo(a,c),this.typeNumber>=7&&this.setupTypeNumber(a),null==this.dataCache&&(this.dataCache=b.createData(this.typeNumber,this.errorCorrectLevel,this.dataList)),this.mapData(this.dataCache,c)},setupPositionProbePattern:function(a,b){for(var c=-1;7>=c;c++)if(!(-1>=a+c||this.moduleCount<=a+c))for(var d=-1;7>=d;d++)-1>=b+d||this.moduleCount<=b+d||(this.modules[a+c][b+d]=c>=0&&6>=c&&(0==d||6==d)||d>=0&&6>=d&&(0==c||6==c)||c>=2&&4>=c&&d>=2&&4>=d?!0:!1)},getBestMaskPattern:function(){for(var a=0,b=0,c=0;8>c;c++){this.makeImpl(!0,c);var d=f.getLostPoint(this);(0==c||a>d)&&(a=d,b=c)}return b},createMovieClip:function(a,b,c){var d=a.createEmptyMovieClip(b,c),e=1;this.make();for(var f=0;f<this.modules.length;f++)for(var g=f*e,h=0;h<this.modules[f].length;h++){var i=h*e,j=this.modules[f][h];j&&(d.beginFill(0,100),d.moveTo(i,g),d.lineTo(i+e,g),d.lineTo(i+e,g+e),d.lineTo(i,g+e),d.endFill())}return d},setupTimingPattern:function(){for(var a=8;a<this.moduleCount-8;a++)null==this.modules[a][6]&&(this.modules[a][6]=0==a%2);for(var b=8;b<this.moduleCount-8;b++)null==this.modules[6][b]&&(this.modules[6][b]=0==b%2)},setupPositionAdjustPattern:function(){for(var a=f.getPatternPosition(this.typeNumber),b=0;b<a.length;b++)for(var c=0;c<a.length;c++){var d=a[b],e=a[c];if(null==this.modules[d][e])for(var g=-2;2>=g;g++)for(var h=-2;2>=h;h++)this.modules[d+g][e+h]=-2==g||2==g||-2==h||2==h||0==g&&0==h?!0:!1}},setupTypeNumber:function(a){for(var b=f.getBCHTypeNumber(this.typeNumber),c=0;18>c;c++){var d=!a&&1==(1&b>>c);this.modules[Math.floor(c/3)][c%3+this.moduleCount-8-3]=d}for(var c=0;18>c;c++){var d=!a&&1==(1&b>>c);this.modules[c%3+this.moduleCount-8-3][Math.floor(c/3)]=d}},setupTypeInfo:function(a,b){for(var c=this.errorCorrectLevel<<3|b,d=f.getBCHTypeInfo(c),e=0;15>e;e++){var g=!a&&1==(1&d>>e);6>e?this.modules[e][8]=g:8>e?this.modules[e+1][8]=g:this.modules[this.moduleCount-15+e][8]=g}for(var e=0;15>e;e++){var g=!a&&1==(1&d>>e);8>e?this.modules[8][this.moduleCount-e-1]=g:9>e?this.modules[8][15-e-1+1]=g:this.modules[8][15-e-1]=g}this.modules[this.moduleCount-8][8]=!a},mapData:function(a,b){for(var c=-1,d=this.moduleCount-1,e=7,g=0,h=this.moduleCount-1;h>0;h-=2)for(6==h&&h--;;){for(var i=0;2>i;i++)if(null==this.modules[d][h-i]){var j=!1;g<a.length&&(j=1==(1&a[g]>>>e));var k=f.getMask(b,d,h-i);k&&(j=!j),this.modules[d][h-i]=j,e--,-1==e&&(g++,e=7)}if(d+=c,0>d||this.moduleCount<=d){d-=c,c=-c;break}}}},b.PAD0=236,b.PAD1=17,b.createData=function(a,c,d){for(var e=j.getRSBlocks(a,c),g=new k,h=0;h<d.length;h++){var i=d[h];g.put(i.mode,4),g.put(i.getLength(),f.getLengthInBits(i.mode,a)),i.write(g)}for(var l=0,h=0;h<e.length;h++)l+=e[h].dataCount;if(g.getLengthInBits()>8*l)throw new Error("code length overflow. ("+g.getLengthInBits()+">"+8*l+")");for(g.getLengthInBits()+4<=8*l&&g.put(0,4);0!=g.getLengthInBits()%8;)g.putBit(!1);for(;;){if(g.getLengthInBits()>=8*l)break;if(g.put(b.PAD0,8),g.getLengthInBits()>=8*l)break;g.put(b.PAD1,8)}return b.createBytes(g,e)},b.createBytes=function(a,b){for(var c=0,d=0,e=0,g=new Array(b.length),h=new Array(b.length),j=0;j<b.length;j++){var k=b[j].dataCount,l=b[j].totalCount-k;d=Math.max(d,k),e=Math.max(e,l),g[j]=new Array(k);for(var m=0;m<g[j].length;m++)g[j][m]=255&a.buffer[m+c];c+=k;var n=f.getErrorCorrectPolynomial(l),o=new i(g[j],n.getLength()-1),p=o.mod(n);h[j]=new Array(n.getLength()-1);for(var m=0;m<h[j].length;m++){var q=m+p.getLength()-h[j].length;h[j][m]=q>=0?p.get(q):0}}for(var r=0,m=0;m<b.length;m++)r+=b[m].totalCount;for(var s=new Array(r),t=0,m=0;d>m;m++)for(var j=0;j<b.length;j++)m<g[j].length&&(s[t++]=g[j][m]);for(var m=0;e>m;m++)for(var j=0;j<b.length;j++)m<h[j].length&&(s[t++]=h[j][m]);return s};for(var c={MODE_NUMBER:1,MODE_ALPHA_NUM:2,MODE_8BIT_BYTE:4,MODE_KANJI:8},d={L:1,M:0,Q:3,H:2},e={PATTERN000:0,PATTERN001:1,PATTERN010:2,PATTERN011:3,PATTERN100:4,PATTERN101:5,PATTERN110:6,PATTERN111:7},f={PATTERN_POSITION_TABLE:[[],[6,18],[6,22],[6,26],[6,30],[6,34],[6,22,38],[6,24,42],[6,26,46],[6,28,50],[6,30,54],[6,32,58],[6,34,62],[6,26,46,66],[6,26,48,70],[6,26,50,74],[6,30,54,78],[6,30,56,82],[6,30,58,86],[6,34,62,90],[6,28,50,72,94],[6,26,50,74,98],[6,30,54,78,102],[6,28,54,80,106],[6,32,58,84,110],[6,30,58,86,114],[6,34,62,90,118],[6,26,50,74,98,122],[6,30,54,78,102,126],[6,26,52,78,104,130],[6,30,56,82,108,134],[6,34,60,86,112,138],[6,30,58,86,114,142],[6,34,62,90,118,146],[6,30,54,78,102,126,150],[6,24,50,76,102,128,154],[6,28,54,80,106,132,158],[6,32,58,84,110,136,162],[6,26,54,82,110,138,166],[6,30,58,86,114,142,170]],G15:1335,G18:7973,G15_MASK:21522,getBCHTypeInfo:function(a){for(var b=a<<10;f.getBCHDigit(b)-f.getBCHDigit(f.G15)>=0;)b^=f.G15<<f.getBCHDigit(b)-f.getBCHDigit(f.G15);return(a<<10|b)^f.G15_MASK},getBCHTypeNumber:function(a){for(var b=a<<12;f.getBCHDigit(b)-f.getBCHDigit(f.G18)>=0;)b^=f.G18<<f.getBCHDigit(b)-f.getBCHDigit(f.G18);return a<<12|b},getBCHDigit:function(a){for(var b=0;0!=a;)b++,a>>>=1;return b},getPatternPosition:function(a){return f.PATTERN_POSITION_TABLE[a-1]},getMask:function(a,b,c){switch(a){case e.PATTERN000:return 0==(b+c)%2;case e.PATTERN001:return 0==b%2;case e.PATTERN010:return 0==c%3;case e.PATTERN011:return 0==(b+c)%3;case e.PATTERN100:return 0==(Math.floor(b/2)+Math.floor(c/3))%2;case e.PATTERN101:return 0==b*c%2+b*c%3;case e.PATTERN110:return 0==(b*c%2+b*c%3)%2;case e.PATTERN111:return 0==(b*c%3+(b+c)%2)%2;default:throw new Error("bad maskPattern:"+a)}},getErrorCorrectPolynomial:function(a){for(var b=new i([1],0),c=0;a>c;c++)b=b.multiply(new i([1,g.gexp(c)],0));return b},getLengthInBits:function(a,b){if(b>=1&&10>b)switch(a){case c.MODE_NUMBER:return 10;case c.MODE_ALPHA_NUM:return 9;case c.MODE_8BIT_BYTE:return 8;case c.MODE_KANJI:return 8;default:throw new Error("mode:"+a)}else if(27>b)switch(a){case c.MODE_NUMBER:return 12;case c.MODE_ALPHA_NUM:return 11;case c.MODE_8BIT_BYTE:return 16;case c.MODE_KANJI:return 10;default:throw new Error("mode:"+a)}else{if(!(41>b))throw new Error("type:"+b);switch(a){case c.MODE_NUMBER:return 14;case c.MODE_ALPHA_NUM:return 13;case c.MODE_8BIT_BYTE:return 16;case c.MODE_KANJI:return 12;default:throw new Error("mode:"+a)}}},getLostPoint:function(a){for(var b=a.getModuleCount(),c=0,d=0;b>d;d++)for(var e=0;b>e;e++){for(var f=0,g=a.isDark(d,e),h=-1;1>=h;h++)if(!(0>d+h||d+h>=b))for(var i=-1;1>=i;i++)0>e+i||e+i>=b||(0!=h||0!=i)&&g==a.isDark(d+h,e+i)&&f++;f>5&&(c+=3+f-5)}for(var d=0;b-1>d;d++)for(var e=0;b-1>e;e++){var j=0;a.isDark(d,e)&&j++,a.isDark(d+1,e)&&j++,a.isDark(d,e+1)&&j++,a.isDark(d+1,e+1)&&j++,(0==j||4==j)&&(c+=3)}for(var d=0;b>d;d++)for(var e=0;b-6>e;e++)a.isDark(d,e)&&!a.isDark(d,e+1)&&a.isDark(d,e+2)&&a.isDark(d,e+3)&&a.isDark(d,e+4)&&!a.isDark(d,e+5)&&a.isDark(d,e+6)&&(c+=40);for(var e=0;b>e;e++)for(var d=0;b-6>d;d++)a.isDark(d,e)&&!a.isDark(d+1,e)&&a.isDark(d+2,e)&&a.isDark(d+3,e)&&a.isDark(d+4,e)&&!a.isDark(d+5,e)&&a.isDark(d+6,e)&&(c+=40);for(var k=0,e=0;b>e;e++)for(var d=0;b>d;d++)a.isDark(d,e)&&k++;var l=Math.abs(100*k/b/b-50)/5;return c+=10*l}},g={glog:function(a){if(1>a)throw new Error("glog("+a+")");return g.LOG_TABLE[a]},gexp:function(a){for(;0>a;)a+=255;for(;a>=256;)a-=255;return g.EXP_TABLE[a]},EXP_TABLE:new Array(256),LOG_TABLE:new Array(256)},h=0;8>h;h++)g.EXP_TABLE[h]=1<<h;for(var h=8;256>h;h++)g.EXP_TABLE[h]=g.EXP_TABLE[h-4]^g.EXP_TABLE[h-5]^g.EXP_TABLE[h-6]^g.EXP_TABLE[h-8];for(var h=0;255>h;h++)g.LOG_TABLE[g.EXP_TABLE[h]]=h;i.prototype={get:function(a){return this.num[a]},getLength:function(){return this.num.length},multiply:function(a){for(var b=new Array(this.getLength()+a.getLength()-1),c=0;c<this.getLength();c++)for(var d=0;d<a.getLength();d++)b[c+d]^=g.gexp(g.glog(this.get(c))+g.glog(a.get(d)));return new i(b,0)},mod:function(a){if(this.getLength()-a.getLength()<0)return this;for(var b=g.glog(this.get(0))-g.glog(a.get(0)),c=new Array(this.getLength()),d=0;d<this.getLength();d++)c[d]=this.get(d);for(var d=0;d<a.getLength();d++)c[d]^=g.gexp(g.glog(a.get(d))+b);return new i(c,0).mod(a)}},j.RS_BLOCK_TABLE=[[1,26,19],[1,26,16],[1,26,13],[1,26,9],[1,44,34],[1,44,28],[1,44,22],[1,44,16],[1,70,55],[1,70,44],[2,35,17],[2,35,13],[1,100,80],[2,50,32],[2,50,24],[4,25,9],[1,134,108],[2,67,43],[2,33,15,2,34,16],[2,33,11,2,34,12],[2,86,68],[4,43,27],[4,43,19],[4,43,15],[2,98,78],[4,49,31],[2,32,14,4,33,15],[4,39,13,1,40,14],[2,121,97],[2,60,38,2,61,39],[4,40,18,2,41,19],[4,40,14,2,41,15],[2,146,116],[3,58,36,2,59,37],[4,36,16,4,37,17],[4,36,12,4,37,13],[2,86,68,2,87,69],[4,69,43,1,70,44],[6,43,19,2,44,20],[6,43,15,2,44,16],[4,101,81],[1,80,50,4,81,51],[4,50,22,4,51,23],[3,36,12,8,37,13],[2,116,92,2,117,93],[6,58,36,2,59,37],[4,46,20,6,47,21],[7,42,14,4,43,15],[4,133,107],[8,59,37,1,60,38],[8,44,20,4,45,21],[12,33,11,4,34,12],[3,145,115,1,146,116],[4,64,40,5,65,41],[11,36,16,5,37,17],[11,36,12,5,37,13],[5,109,87,1,110,88],[5,65,41,5,66,42],[5,54,24,7,55,25],[11,36,12],[5,122,98,1,123,99],[7,73,45,3,74,46],[15,43,19,2,44,20],[3,45,15,13,46,16],[1,135,107,5,136,108],[10,74,46,1,75,47],[1,50,22,15,51,23],[2,42,14,17,43,15],[5,150,120,1,151,121],[9,69,43,4,70,44],[17,50,22,1,51,23],[2,42,14,19,43,15],[3,141,113,4,142,114],[3,70,44,11,71,45],[17,47,21,4,48,22],[9,39,13,16,40,14],[3,135,107,5,136,108],[3,67,41,13,68,42],[15,54,24,5,55,25],[15,43,15,10,44,16],[4,144,116,4,145,117],[17,68,42],[17,50,22,6,51,23],[19,46,16,6,47,17],[2,139,111,7,140,112],[17,74,46],[7,54,24,16,55,25],[34,37,13],[4,151,121,5,152,122],[4,75,47,14,76,48],[11,54,24,14,55,25],[16,45,15,14,46,16],[6,147,117,4,148,118],[6,73,45,14,74,46],[11,54,24,16,55,25],[30,46,16,2,47,17],[8,132,106,4,133,107],[8,75,47,13,76,48],[7,54,24,22,55,25],[22,45,15,13,46,16],[10,142,114,2,143,115],[19,74,46,4,75,47],[28,50,22,6,51,23],[33,46,16,4,47,17],[8,152,122,4,153,123],[22,73,45,3,74,46],[8,53,23,26,54,24],[12,45,15,28,46,16],[3,147,117,10,148,118],[3,73,45,23,74,46],[4,54,24,31,55,25],[11,45,15,31,46,16],[7,146,116,7,147,117],[21,73,45,7,74,46],[1,53,23,37,54,24],[19,45,15,26,46,16],[5,145,115,10,146,116],[19,75,47,10,76,48],[15,54,24,25,55,25],[23,45,15,25,46,16],[13,145,115,3,146,116],[2,74,46,29,75,47],[42,54,24,1,55,25],[23,45,15,28,46,16],[17,145,115],[10,74,46,23,75,47],[10,54,24,35,55,25],[19,45,15,35,46,16],[17,145,115,1,146,116],[14,74,46,21,75,47],[29,54,24,19,55,25],[11,45,15,46,46,16],[13,145,115,6,146,116],[14,74,46,23,75,47],[44,54,24,7,55,25],[59,46,16,1,47,17],[12,151,121,7,152,122],[12,75,47,26,76,48],[39,54,24,14,55,25],[22,45,15,41,46,16],[6,151,121,14,152,122],[6,75,47,34,76,48],[46,54,24,10,55,25],[2,45,15,64,46,16],[17,152,122,4,153,123],[29,74,46,14,75,47],[49,54,24,10,55,25],[24,45,15,46,46,16],[4,152,122,18,153,123],[13,74,46,32,75,47],[48,54,24,14,55,25],[42,45,15,32,46,16],[20,147,117,4,148,118],[40,75,47,7,76,48],[43,54,24,22,55,25],[10,45,15,67,46,16],[19,148,118,6,149,119],[18,75,47,31,76,48],[34,54,24,34,55,25],[20,45,15,61,46,16]],j.getRSBlocks=function(a,b){var c=j.getRsBlockTable(a,b);if(void 0==c)throw new Error("bad rs block @ typeNumber:"+a+"/errorCorrectLevel:"+b);for(var d=c.length/3,e=[],f=0;d>f;f++)for(var g=c[3*f+0],h=c[3*f+1],i=c[3*f+2],k=0;g>k;k++)e.push(new j(h,i));return e},j.getRsBlockTable=function(a,b){switch(b){case d.L:return j.RS_BLOCK_TABLE[4*(a-1)+0];case d.M:return j.RS_BLOCK_TABLE[4*(a-1)+1];case d.Q:return j.RS_BLOCK_TABLE[4*(a-1)+2];case d.H:return j.RS_BLOCK_TABLE[4*(a-1)+3];default:return void 0}},k.prototype={get:function(a){var b=Math.floor(a/8);return 1==(1&this.buffer[b]>>>7-a%8)},put:function(a,b){for(var c=0;b>c;c++)this.putBit(1==(1&a>>>b-c-1))},getLengthInBits:function(){return this.length},putBit:function(a){var b=Math.floor(this.length/8);this.buffer.length<=b&&this.buffer.push(0),a&&(this.buffer[b]|=128>>>this.length%8),this.length++}};var l=[[17,14,11,7],[32,26,20,14],[53,42,32,24],[78,62,46,34],[106,84,60,44],[134,106,74,58],[154,122,86,64],[192,152,108,84],[230,180,130,98],[271,213,151,119],[321,251,177,137],[367,287,203,155],[425,331,241,177],[458,362,258,194],[520,412,292,220],[586,450,322,250],[644,504,364,280],[718,560,394,310],[792,624,442,338],[858,666,482,382],[929,711,509,403],[1003,779,565,439],[1091,857,611,461],[1171,911,661,511],[1273,997,715,535],[1367,1059,751,593],[1465,1125,805,625],[1528,1190,868,658],[1628,1264,908,698],[1732,1370,982,742],[1840,1452,1030,790],[1952,1538,1112,842],[2068,1628,1168,898],[2188,1722,1228,958],[2303,1809,1283,983],[2431,1911,1351,1051],[2563,1989,1423,1093],[2699,2099,1499,1139],[2809,2213,1579,1219],[2953,2331,1663,1273]],o=function(){var a=function(a,b){this._el=a,this._htOption=b};return a.prototype.draw=function(a){function g(a,b){var c=document.createElementNS("http://www.w3.org/2000/svg",a);for(var d in b)b.hasOwnProperty(d)&&c.setAttribute(d,b[d]);return c}var b=this._htOption,c=this._el,d=a.getModuleCount();Math.floor(b.width/d),Math.floor(b.height/d),this.clear();var h=g("svg",{viewBox:"0 0 "+String(d)+" "+String(d),width:"100%",height:"100%",fill:b.colorLight});h.setAttributeNS("http://www.w3.org/2000/xmlns/","xmlns:xlink","http://www.w3.org/1999/xlink"),c.appendChild(h),h.appendChild(g("rect",{fill:b.colorDark,width:"1",height:"1",id:"template"}));for(var i=0;d>i;i++)for(var j=0;d>j;j++)if(a.isDark(i,j)){var k=g("use",{x:String(i),y:String(j)});k.setAttributeNS("http://www.w3.org/1999/xlink","href","#template"),h.appendChild(k)}},a.prototype.clear=function(){for(;this._el.hasChildNodes();)this._el.removeChild(this._el.lastChild)},a}(),p="svg"===document.documentElement.tagName.toLowerCase(),q=p?o:m()?function(){function a(){this._elImage.src=this._elCanvas.toDataURL("image/png"),this._elImage.style.display="block",this._elCanvas.style.display="none"}function d(a,b){var c=this;if(c._fFail=b,c._fSuccess=a,null===c._bSupportDataURI){var d=document.createElement("img"),e=function(){c._bSupportDataURI=!1,c._fFail&&_fFail.call(c)},f=function(){c._bSupportDataURI=!0,c._fSuccess&&c._fSuccess.call(c)};return d.onabort=e,d.onerror=e,d.onload=f,d.src="data:image/gif;base64,iVBORw0KGgoAAAANSUhEUgAAAAUAAAAFCAYAAACNbyblAAAAHElEQVQI12P4//8/w38GIAXDIBKE0DHxgljNBAAO9TXL0Y4OHwAAAABJRU5ErkJggg==",void 0}c._bSupportDataURI===!0&&c._fSuccess?c._fSuccess.call(c):c._bSupportDataURI===!1&&c._fFail&&c._fFail.call(c)}if(this._android&&this._android<=2.1){var b=1/window.devicePixelRatio,c=CanvasRenderingContext2D.prototype.drawImage;CanvasRenderingContext2D.prototype.drawImage=function(a,d,e,f,g,h,i,j){if("nodeName"in a&&/img/i.test(a.nodeName))for(var l=arguments.length-1;l>=1;l--)arguments[l]=arguments[l]*b;else"undefined"==typeof j&&(arguments[1]*=b,arguments[2]*=b,arguments[3]*=b,arguments[4]*=b);c.apply(this,arguments)}}var e=function(a,b){this._bIsPainted=!1,this._android=n(),this._htOption=b,this._elCanvas=document.createElement("canvas"),this._elCanvas.width=b.width,this._elCanvas.height=b.height,a.appendChild(this._elCanvas),this._el=a,this._oContext=this._elCanvas.getContext("2d"),this._bIsPainted=!1,this._elImage=document.createElement("img"),this._elImage.style.display="none",this._el.appendChild(this._elImage),this._bSupportDataURI=null};return e.prototype.draw=function(a){var b=this._elImage,c=this._oContext,d=this._htOption,e=a.getModuleCount(),f=d.width/e,g=d.height/e,h=Math.round(f),i=Math.round(g);b.style.display="none",this.clear();for(var j=0;e>j;j++)for(var k=0;e>k;k++){var l=a.isDark(j,k),m=k*f,n=j*g;c.strokeStyle=l?d.colorDark:d.colorLight,c.lineWidth=1,c.fillStyle=l?d.colorDark:d.colorLight,c.fillRect(m,n,f,g),c.strokeRect(Math.floor(m)+.5,Math.floor(n)+.5,h,i),c.strokeRect(Math.ceil(m)-.5,Math.ceil(n)-.5,h,i)}this._bIsPainted=!0},e.prototype.makeImage=function(){this._bIsPainted&&d.call(this,a)},e.prototype.isPainted=function(){return this._bIsPainted},e.prototype.clear=function(){this._oContext.clearRect(0,0,this._elCanvas.width,this._elCanvas.height),this._bIsPainted=!1},e.prototype.round=function(a){return a?Math.floor(1e3*a)/1e3:a},e}():function(){var a=function(a,b){this._el=a,this._htOption=b};return a.prototype.draw=function(a){for(var b=this._htOption,c=this._el,d=a.getModuleCount(),e=Math.floor(b.width/d),f=Math.floor(b.height/d),g=['<table style="border:0;border-collapse:collapse;">'],h=0;d>h;h++){g.push("<tr>");for(var i=0;d>i;i++)g.push('<td style="border:0;border-collapse:collapse;padding:0;margin:0;width:'+e+"px;height:"+f+"px;background-color:"+(a.isDark(h,i)?b.colorDark:b.colorLight)+';"></td>');g.push("</tr>")}g.push("</table>"),c.innerHTML=g.join("");var j=c.childNodes[0],k=(b.width-j.offsetWidth)/2,l=(b.height-j.offsetHeight)/2;k>0&&l>0&&(j.style.margin=l+"px "+k+"px")},a.prototype.clear=function(){this._el.innerHTML=""},a}();QRCode=function(a,b){if(this._htOption={width:256,height:256,typeNumber:4,colorDark:"#000000",colorLight:"#ffffff",correctLevel:d.H},"string"==typeof b&&(b={text:b}),b)for(var c in b)this._htOption[c]=b[c];"string"==typeof a&&(a=document.getElementById(a)),this._android=n(),this._el=a,this._oQRCode=null,this._oDrawing=new q(this._el,this._htOption),this._htOption.text&&this.makeCode(this._htOption.text)},QRCode.prototype.makeCode=function(a){this._oQRCode=new b(r(a,this._htOption.correctLevel),this._htOption.correctLevel),this._oQRCode.addData(a),this._oQRCode.make(),this._el.title=a,this._oDrawing.draw(this._oQRCode),this.makeImage()},QRCode.prototype.makeImage=function(){"function"==typeof this._oDrawing.makeImage&&(!this._android||this._android>=3)&&this._oDrawing.makeImage()},QRCode.prototype.clear=function(){this._oDrawing.clear()},QRCode.CorrectLevel=d}();
												]]>
												var qrcode = new QRCode(document.getElementById("qrcode"), { width : 128, height : 128, correctLevel : QRCode.CorrectLevel.M }); function makeCode (msg) {	var elText = document.getElementById("text"); qrcode.makeCode(msg); } makeCode(document.getElementById("qrvalue").innerHTML.replace(/\s/g, ''));
											</script>
                    </div>
                        <img  style="width:280px; height:100px"    src="data:image/png;base64,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"  border="0" />

                  
                </td>
                
                <td width="40%"/>
              </tr>
              <tr style="height:118px; " valign="top">
                <td width="40%" align="right" valign="bottom">
                  <table id="customerPartyTable" align="left" border="0" height="50%">
                    <tbody>
                      <tr style="height:71px; ">
                        <td>
                          <hr/>
                          <table align="center" border="0">
                            <tbody>
                              <tr>
                                <xsl:for-each select="n1:Invoice/cac:AccountingCustomerParty/cac:Party">
                                  <td style="width:469px; " align="left">
                                    <span style="font-weight:bold; ">
                                      <xsl:text>SAYIN</xsl:text>
                                    </span>
                                  </td>
                                </xsl:for-each>
                              </tr>
                              <tr>
                                <xsl:choose>
                                  <xsl:when test="n1:Invoice/cac:BuyerCustomerParty/cac:Party/cac:PartyIdentification/cbc:ID[@schemeID='PARTYTYPE' and text()='TAXFREE']">
                                    <xsl:for-each select="n1:Invoice/cac:BuyerCustomerParty/cac:Party">
                                      <xsl:call-template name="Party_Title">
                                        <xsl:with-param name="PartyType">TAXFREE</xsl:with-param>
                                      </xsl:call-template>
                                    </xsl:for-each>
                                  </xsl:when>
                                  <xsl:otherwise>
                                    <xsl:for-each select="n1:Invoice/cac:AccountingCustomerParty/cac:Party">
                                      <xsl:call-template name="Party_Title">
                                        <xsl:with-param name="PartyType">OTHER</xsl:with-param>
                                      </xsl:call-template>
                                    </xsl:for-each>
                                  </xsl:otherwise>
                                </xsl:choose>
                              </tr>
                              <xsl:choose>
                                <xsl:when test="n1:Invoice/cac:BuyerCustomerParty/cac:Party/cac:PartyIdentification/cbc:ID[@schemeID='PARTYTYPE' and text()='TAXFREE']">
                                  <xsl:for-each select="n1:Invoice/cac:BuyerCustomerParty/cac:Party">
                                    <tr>
                                      <xsl:call-template name="Party_Adress">
                                        <xsl:with-param name="PartyType">TAXFREE</xsl:with-param>
                                      </xsl:call-template>
                                    </tr>
                                    <xsl:call-template name="Party_Other">
                                      <xsl:with-param name="PartyType">TAXFREE</xsl:with-param>
                                    </xsl:call-template>
                                  </xsl:for-each>
                                </xsl:when>
                                <xsl:otherwise>
                                  <xsl:for-each select="n1:Invoice/cac:AccountingCustomerParty/cac:Party">
                                    <tr>
                                      <xsl:call-template name="Party_Adress">
                                        <xsl:with-param name="PartyType">OTHER</xsl:with-param>
                                      </xsl:call-template>
                                    </tr>
                                    <xsl:call-template name="Party_Other">
                                      <xsl:with-param name="PartyType">OTHER</xsl:with-param>
                                    </xsl:call-template>
                                  </xsl:for-each>
                                </xsl:otherwise>
                              </xsl:choose>
                            </tbody>
                          </table>
                          <hr/>
                        </td>
                      </tr>
                    </tbody>
                  </table>
                  <br/>
                </td>
                <td width="60%" align="center" valign="bottom" colspan="2">
                  <table border="1" height="13" id="despatchTable">
                    <tbody>
                      <tr>
                        <td style="width:105px;" align="left">
                          <span style="font-weight:bold; ">
                            <xsl:text>Özelleştirme No:</xsl:text>
                          </span>
                        </td>
                        <td style="width:110px;" align="left">
                          <xsl:for-each select="n1:Invoice/cbc:CustomizationID">
                            <xsl:apply-templates/>
                          </xsl:for-each>
                        </td>
                      </tr>
                      <tr style="height:13px; ">
                        <td align="left">
                          <span style="font-weight:bold; ">
                            <xsl:text>Senaryo:</xsl:text>
                          </span>
                        </td>
                        <td align="left">
                          <xsl:for-each select="n1:Invoice/cbc:ProfileID">
                            <xsl:apply-templates/>
                          </xsl:for-each>
                        </td>
                      </tr>
                      <tr style="height:13px; ">
                        <td align="left">
                          <span style="font-weight:bold; ">
                            <xsl:text>Fatura Tipi:</xsl:text>
                          </span>
                        </td>
                        <td align="left">
                          <xsl:for-each select="n1:Invoice/cbc:InvoiceTypeCode">
                            <xsl:apply-templates/>
                          </xsl:for-each>
                        </td>
                      </tr>
                      <tr style="height:13px; ">
                        <td align="left">
                          <span style="font-weight:bold; ">
                            <xsl:text>Fatura No:</xsl:text>
                          </span>
                        </td>
                        <td align="left">
                          <xsl:for-each select="n1:Invoice/cbc:ID">
                            <xsl:apply-templates/>
                          </xsl:for-each>
                        </td>
                      </tr>
                      <tr style="height:13px; ">
                        <td align="left">
                          <span style="font-weight:bold; ">
                            <xsl:text>Fatura Tarihi:</xsl:text>
                          </span>
                        </td>
                        <td align="left">
                          <xsl:for-each select="n1:Invoice/cbc:IssueDate">
                            <xsl:apply-templates select="."/>
                          </xsl:for-each>
                        </td>
                      </tr>
                      <tr style="height:13px; ">
                        <td align="left">
                          <span style="font-weight:bold; ">
                            <xsl:text>Fatura Zamanı:</xsl:text>
                          </span>
                        </td>
                        <td align="left">
                          <xsl:for-each select="n1:Invoice">
                            <xsl:for-each select="cbc:IssueTime">
                              <xsl:apply-templates/>
                            </xsl:for-each>
                          </xsl:for-each>
                        </td>
                      </tr>
                      <xsl:for-each select="n1:Invoice/cac:DespatchDocumentReference">
                        <tr style="height:13px; ">
                          <td align="left">
                            <span style="font-weight:bold; ">
                              <xsl:text>İrsaliye No:</xsl:text>
                            </span>
                            <xsl:text>&#160;</xsl:text>
                          </td>
                          <td align="left">
                            <xsl:value-of select="cbc:ID"/>
                          </td>
                        </tr>
                        <tr style="height:13px; ">
                          <td align="left">
                            <span style="font-weight:bold; ">
                              <xsl:text>İrsaliye Tarihi:</xsl:text>
                            </span>
                          </td>
                          <td align="left">
                            <xsl:for-each select="cbc:IssueDate">
                              <xsl:apply-templates select="."/>
                            </xsl:for-each>
                          </td>
                        </tr>
                      </xsl:for-each>
                      <xsl:if test="//n1:Invoice/cac:OrderReference">
                        <tr style="height:13px">
                          <td align="left">
                            <span style="font-weight:bold; ">
                              <xsl:text>Sipariş No:</xsl:text>
                            </span>
                          </td>
                          <td align="left">
                            <xsl:for-each select="n1:Invoice/cac:OrderReference/cbc:ID">
                              <xsl:apply-templates/>
                            </xsl:for-each>
                          </td>
                        </tr>
                      </xsl:if>
                      <xsl:if	test="//n1:Invoice/cac:OrderReference/cbc:IssueDate">
                        <tr style="height:13px">
                          <td align="left">
                            <span style="font-weight:bold; ">
                              <xsl:text>Sipariş Tarihi:</xsl:text>
                            </span>
                          </td>
                          <td align="left">
                            <xsl:for-each select="n1:Invoice/cac:OrderReference/cbc:IssueDate">
                              <xsl:apply-templates select="."/>
                            </xsl:for-each>
                          </td>
                        </tr>
                      </xsl:if>
                      <xsl:for-each select="n1:Invoice/cac:TaxRepresentativeParty/cac:PartyIdentification/cbc:ID[@schemeID='ARACIKURUMVKN']">
                        <tr>
                          <td style="width:105px;" align="left">
                            <span style="font-weight:bold; ">
                              <xsl:text>Aracı Kurum VKN:</xsl:text>
                            </span>
                          </td>
                          <td style="width:110px;" align="left">
                            <xsl:value-of select="."/>
                          </td>
                        </tr>
                        <tr>
                          <td style="width:105px;" align="left">
                            <span style="font-weight:bold; ">
                              <xsl:text>Aracı Kurum Unvan:</xsl:text>
                            </span>
                          </td>
                          <td style="width:110px;" align="left">
                            <xsl:value-of select="../../cac:PartyName/cbc:Name"/>
                          </td>
                        </tr>
                      </xsl:for-each>
                    </tbody>
                  </table>
                </td>
              </tr>
              <tr align="left">
                <table id="ettnTable">
                  <tr style="height:13px;">
                    <td align="left" valign="top">
                      <span style="font-weight:bold; ">
                        <xsl:text>ETTN:</xsl:text>
                      </span>
                    </td>
                    <td align="left" width="240px">
                      <xsl:for-each select="n1:Invoice/cbc:UUID">
                        <xsl:apply-templates/>
                      </xsl:for-each>
                    </td>
                  </tr>
                </table>
              </tr>
            </tbody>
          </table>
          <div id="lineTableAligner">
            <span>
              <xsl:text>&#160;</xsl:text>
            </span>
          </div>
          <table border="1" id="lineTable" width="800">
            <tbody>
              <tr id="lineTableTr">
                <td id="lineTableTd" style="width:3%">
                  <span style="font-weight:bold; " align="center">
                    <xsl:text>Sıra No</xsl:text>
                  </span>
                </td>
                <td id="lineTableTd" style="width:10%" align="center">
                  <span style="font-weight:bold; ">
                    <xsl:text>Mal Hizmet Kodu</xsl:text>
                  </span>
                </td>
                <td id="lineTableTd" style="width:25%" align="center">
                  <span style="font-weight:bold; ">
                    <xsl:text>Mal Hizmet Adı</xsl:text>
                  </span>
                </td>
                <td id="lineTableTd" style="width:10%" align="center">
                  <span style="font-weight:bold; ">
                    <xsl:text>Açıklama</xsl:text>
                  </span>
                </td>
               
                <td id="lineTableTd" style="width:7.4%" align="center">
                  <span style="font-weight:bold;">
                    <xsl:text>Miktar</xsl:text>
                  </span>
                </td>
                <td id="lineTableTd" style="width:9%" align="center">
                  <span style="font-weight:bold; ">
                    <xsl:text>Birim Fiyat</xsl:text>
                  </span>
                </td>
                
                
                <td id="lineTableTd" style="width:7%" align="center">
                  <span style="font-weight:bold; ">
                    <xsl:text>KDV Oranı</xsl:text>
                  </span>
                </td>
                <td id="lineTableTd" style="width:9%" align="center">
                  <span style="font-weight:bold; ">
                    <xsl:text>KDV Tutarı</xsl:text>
                  </span>
                </td>
              
                <td id="lineTableTd" style="width:12%" align="center">
                  <span style="font-weight:bold; ">
                    <xsl:text>Mal Hizmet Tutarı</xsl:text>
                  </span>
                </td>
              </tr>
              <xsl:if test="count(//n1:Invoice/cac:InvoiceLine) &gt;= 20">
                <xsl:for-each select="//n1:Invoice/cac:InvoiceLine">
                  <xsl:apply-templates select="."/>
                </xsl:for-each>
              </xsl:if>
              <xsl:if test="count(//n1:Invoice/cac:InvoiceLine) &lt; 20">
                <xsl:choose>
                  <xsl:when test="//n1:Invoice/cac:InvoiceLine[1]">
                    <xsl:apply-templates
											select="//n1:Invoice/cac:InvoiceLine[1]"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:apply-templates select="//n1:Invoice"/>
                  </xsl:otherwise>
                </xsl:choose>
                <xsl:choose>
                  <xsl:when test="//n1:Invoice/cac:InvoiceLine[2]">
                    <xsl:apply-templates
											select="//n1:Invoice/cac:InvoiceLine[2]"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:apply-templates select="//n1:Invoice"/>
                  </xsl:otherwise>
                </xsl:choose>
                <xsl:choose>
                  <xsl:when test="//n1:Invoice/cac:InvoiceLine[3]">
                    <xsl:apply-templates
											select="//n1:Invoice/cac:InvoiceLine[3]"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:apply-templates select="//n1:Invoice"/>
                  </xsl:otherwise>
                </xsl:choose>
                <xsl:choose>
                  <xsl:when test="//n1:Invoice/cac:InvoiceLine[4]">
                    <xsl:apply-templates
											select="//n1:Invoice/cac:InvoiceLine[4]"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:apply-templates select="//n1:Invoice"/>
                  </xsl:otherwise>
                </xsl:choose>
                <xsl:choose>
                  <xsl:when test="//n1:Invoice/cac:InvoiceLine[5]">
                    <xsl:apply-templates
											select="//n1:Invoice/cac:InvoiceLine[5]"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:apply-templates select="//n1:Invoice"/>
                  </xsl:otherwise>
                </xsl:choose>
                <xsl:choose>
                  <xsl:when test="//n1:Invoice/cac:InvoiceLine[6]">
                    <xsl:apply-templates
											select="//n1:Invoice/cac:InvoiceLine[6]"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:apply-templates select="//n1:Invoice"/>
                  </xsl:otherwise>
                </xsl:choose>
                <xsl:choose>
                  <xsl:when test="//n1:Invoice/cac:InvoiceLine[7]">
                    <xsl:apply-templates
											select="//n1:Invoice/cac:InvoiceLine[7]"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:apply-templates select="//n1:Invoice"/>
                  </xsl:otherwise>
                </xsl:choose>
                <xsl:choose>
                  <xsl:when test="//n1:Invoice/cac:InvoiceLine[8]">
                    <xsl:apply-templates
											select="//n1:Invoice/cac:InvoiceLine[8]"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:apply-templates select="//n1:Invoice"/>
                  </xsl:otherwise>
                </xsl:choose>
                <xsl:choose>
                  <xsl:when test="//n1:Invoice/cac:InvoiceLine[9]">
                    <xsl:apply-templates
											select="//n1:Invoice/cac:InvoiceLine[9]"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:apply-templates select="//n1:Invoice"/>
                  </xsl:otherwise>
                </xsl:choose>
                <xsl:choose>
                  <xsl:when test="//n1:Invoice/cac:InvoiceLine[10]">
                    <xsl:apply-templates
											select="//n1:Invoice/cac:InvoiceLine[10]"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:apply-templates select="//n1:Invoice"/>
                  </xsl:otherwise>
                </xsl:choose>
                <xsl:choose>
                  <xsl:when test="//n1:Invoice/cac:InvoiceLine[11]">
                    <xsl:apply-templates
											select="//n1:Invoice/cac:InvoiceLine[11]"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:apply-templates select="//n1:Invoice"/>
                  </xsl:otherwise>
                </xsl:choose>
                <xsl:choose>
                  <xsl:when test="//n1:Invoice/cac:InvoiceLine[12]">
                    <xsl:apply-templates
											select="//n1:Invoice/cac:InvoiceLine[12]"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:apply-templates select="//n1:Invoice"/>
                  </xsl:otherwise>
                </xsl:choose>
                <xsl:choose>
                  <xsl:when test="//n1:Invoice/cac:InvoiceLine[13]">
                    <xsl:apply-templates
											select="//n1:Invoice/cac:InvoiceLine[13]"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:apply-templates select="//n1:Invoice"/>
                  </xsl:otherwise>
                </xsl:choose>
                <xsl:choose>
                  <xsl:when test="//n1:Invoice/cac:InvoiceLine[14]">
                    <xsl:apply-templates
											select="//n1:Invoice/cac:InvoiceLine[14]"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:apply-templates select="//n1:Invoice"/>
                  </xsl:otherwise>
                </xsl:choose>
                <xsl:choose>
                  <xsl:when test="//n1:Invoice/cac:InvoiceLine[15]">
                    <xsl:apply-templates
											select="//n1:Invoice/cac:InvoiceLine[15]"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:apply-templates select="//n1:Invoice"/>
                  </xsl:otherwise>
                </xsl:choose>
                <xsl:choose>
                  <xsl:when test="//n1:Invoice/cac:InvoiceLine[16]">
                    <xsl:apply-templates
											select="//n1:Invoice/cac:InvoiceLine[16]"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:apply-templates select="//n1:Invoice"/>
                  </xsl:otherwise>
                </xsl:choose>
                <xsl:choose>
                  <xsl:when test="//n1:Invoice/cac:InvoiceLine[17]">
                    <xsl:apply-templates
											select="//n1:Invoice/cac:InvoiceLine[17]"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:apply-templates select="//n1:Invoice"/>
                  </xsl:otherwise>
                </xsl:choose>
                <xsl:choose>
                  <xsl:when test="//n1:Invoice/cac:InvoiceLine[18]">
                    <xsl:apply-templates
											select="//n1:Invoice/cac:InvoiceLine[18]"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:apply-templates select="//n1:Invoice"/>
                  </xsl:otherwise>
                </xsl:choose>
                <xsl:choose>
                  <xsl:when test="//n1:Invoice/cac:InvoiceLine[19]">
                    <xsl:apply-templates
											select="//n1:Invoice/cac:InvoiceLine[19]"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:apply-templates select="//n1:Invoice"/>
                  </xsl:otherwise>
                </xsl:choose>
                <xsl:choose>
                  <xsl:when test="//n1:Invoice/cac:InvoiceLine[20]">
                    <xsl:apply-templates
											select="//n1:Invoice/cac:InvoiceLine[20]"/>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:apply-templates select="//n1:Invoice"/>
                  </xsl:otherwise>
                </xsl:choose>
              </xsl:if>
            </tbody>
          </table>
        </xsl:for-each>
        <table id="budgetContainerTable" width="800px">
          <tr id="budgetContainerTr" align="right">
            <td id="budgetContainerDummyTd"/>
            <td id="lineTableBudgetTd" align="right" width="200px">
              <span style="font-weight:bold; ">
                <xsl:text>Mal Hizmet Toplam Tutarı</xsl:text>
              </span>
            </td>
            <td id="lineTableBudgetTd" style="width:81px; " align="right">
              <xsl:for-each select="n1:Invoice/cac:LegalMonetaryTotal/cbc:LineExtensionAmount">
                <xsl:call-template name="Curr_Type"/>
              </xsl:for-each>
            </td>
          </tr>
          <xsl:for-each select="n1:Invoice/cac:TaxTotal/cac:TaxSubtotal">
            <xsl:if test="cac:TaxCategory/cac:TaxScheme/cbc:TaxTypeCode = '4171'">
              <tr id="budgetContainerTr" align="right">
                <td id="budgetContainerDummyTd"/>
                <td id="lineTableBudgetTd" align="right" width="200px">
                  <span style="font-weight:bold; ">
                    <xsl:text>Teslim Bedeli</xsl:text>
                  </span>
                </td>
                <td id="lineTableBudgetTd" style="width:81px; " align="right">
                  <xsl:for-each select="//n1:Invoice/cac:LegalMonetaryTotal/cbc:LineExtensionAmount">
                    <xsl:call-template name="Curr_Type"/>
                  </xsl:for-each>
                </td>
              </tr>
            </xsl:if>
          </xsl:for-each>
          <tr id="budgetContainerTr" align="right">
            <td id="budgetContainerDummyTd"/>
            <td id="lineTableBudgetTd" align="right" width="200px">
              <span style="font-weight:bold; ">
                <xsl:text>Toplam İskonto</xsl:text>
              </span>
            </td>
            <td id="lineTableBudgetTd" style="width:81px; " align="right">
              <xsl:for-each select="n1:Invoice/cac:LegalMonetaryTotal/cbc:AllowanceTotalAmount">
                <xsl:call-template name="Curr_Type"/>
              </xsl:for-each>
            </td>
          </tr>
		  <tr id="budgetContainerTr" align="right">
            <td id="budgetContainerDummyTd"/>
            <td id="lineTableBudgetTd" align="right" width="200px">
              <span style="font-weight:bold; ">
                <xsl:text>Toplam Masraf</xsl:text>
              </span>
            </td>
            <td id="lineTableBudgetTd" style="width:81px; " align="right">
              <xsl:for-each select="n1:Invoice/cac:LegalMonetaryTotal/cbc:ChargeTotalAmount">
                <xsl:call-template name="Curr_Type"/>
              </xsl:for-each>
            </td>
          </tr>
          <xsl:for-each select="n1:Invoice/cac:TaxTotal/cac:TaxSubtotal">
            <tr id="budgetContainerTr" align="right">
              <td id="budgetContainerDummyTd"/>
              <td id="lineTableBudgetTd" width="211px" align="right">
                <span style="font-weight:bold; ">
                  <xsl:text>Hesaplanan </xsl:text>
                  <xsl:value-of select="cac:TaxCategory/cac:TaxScheme/cbc:Name"/>
                  <xsl:text>(%</xsl:text>
                  <xsl:value-of select="cbc:Percent"/>
                  <xsl:text>)</xsl:text>
                </span>
              </td>
              <td id="lineTableBudgetTd" style="width:82px; " align="right">
                <xsl:for-each select="cac:TaxCategory/cac:TaxScheme">
                  <xsl:text> </xsl:text>
                  <xsl:value-of
										select="format-number(../../cbc:TaxAmount, '###.##0,00', 'european')"/>
                  <xsl:if test="../../cbc:TaxAmount/@currencyID">
                    <xsl:text> </xsl:text>
                    <xsl:if test="../../cbc:TaxAmount/@currencyID = 'TRL' or ../../cbc:TaxAmount/@currencyID = 'TRY'">
                      <xsl:text>TL</xsl:text>
                    </xsl:if>
                    <xsl:if test="../../cbc:TaxAmount/@currencyID != 'TRL' and ../../cbc:TaxAmount/@currencyID != 'TRY'">
                      <xsl:value-of select="../../cbc:TaxAmount/@currencyID"/>
                    </xsl:if>
                  </xsl:if>
                </xsl:for-each>
              </td>
            </tr>
          </xsl:for-each>
          <xsl:for-each select="n1:Invoice/cac:TaxTotal/cac:TaxSubtotal">
            <xsl:if test="cac:TaxCategory/cac:TaxScheme/cbc:TaxTypeCode = '4171'">
              <tr id="budgetContainerTr" align="right">
                <td id="budgetContainerDummyTd"/>
                <td id="lineTableBudgetTd" align="right" width="200px">
                  <span style="font-weight:bold; ">
                    <xsl:text>KDV Matrahı</xsl:text>
                  </span>
                </td>
                <td id="lineTableBudgetTd" style="width:81px; " align="right">
                  <xsl:value-of
											select="format-number(sum(//n1:Invoice/cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:TaxTypeCode=0015]/cbc:TaxableAmount), '###.##0,00', 'european')"/>
                  <xsl:if
										test="//n1:Invoice/cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount/@currencyID">
                    <xsl:text> </xsl:text>
                    <xsl:if
											test="//n1:Invoice/cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount/@currencyID = 'TRL' or //n1:Invoice/cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount/@currencyID = 'TRY'">
                      <xsl:text>TL</xsl:text>
                    </xsl:if>
                    <xsl:if
											test="//n1:Invoice/cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount/@currencyID != 'TRL' and //n1:Invoice/cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount/@currencyID != 'TRY'">
                      <xsl:value-of
												select="//n1:Invoice/cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount/@currencyID"
											/>
                    </xsl:if>
                  </xsl:if>
                </td>
              </tr>
              <tr id="budgetContainerTr" align="right">
                <td id="budgetContainerDummyTd"/>
                <td id="lineTableBudgetTd" align="right" width="200px">
                  <span style="font-weight:bold; ">
                    <xsl:text>Tevkifat Dahil Toplam Tutar</xsl:text>
                  </span>
                </td>
                <td id="lineTableBudgetTd" style="width:81px; " align="right">
                  <xsl:for-each select="//n1:Invoice/cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount">
                    <xsl:call-template name="Curr_Type"/>
                  </xsl:for-each>
                </td>
              </tr>
              <tr id="budgetContainerTr" align="right">
                <td id="budgetContainerDummyTd"/>
                <td id="lineTableBudgetTd" align="right" width="200px">
                  <span style="font-weight:bold; ">
                    <xsl:text>Tevkifat Hariç Toplam Tutar</xsl:text>
                  </span>
                </td>
                <td id="lineTableBudgetTd" style="width:81px; " align="right">
                  <xsl:for-each select="//n1:Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount">
                    <xsl:call-template name="Curr_Type"/>
                  </xsl:for-each>
                </td>
              </tr>
            </xsl:if>
          </xsl:for-each>
          <xsl:for-each select="n1:Invoice/cac:WithholdingTaxTotal/cac:TaxSubtotal">
            <tr id="budgetContainerTr" align="right">
              <td id="budgetContainerDummyTd"/>
              <td id="lineTableBudgetTd" width="211px" align="right">
                <span style="font-weight:bold; ">
                  <xsl:text>Hesaplanan KDV Tevkifat</xsl:text>
                  <xsl:text>(%</xsl:text>
                  <xsl:value-of select="cbc:Percent"/>
                  <xsl:text>)</xsl:text>
                </span>
              </td>
              <td id="lineTableBudgetTd" style="width:82px; " align="right">
                <xsl:for-each select="cac:TaxCategory/cac:TaxScheme">
                  <xsl:text> </xsl:text>
                  <xsl:value-of
										select="format-number(../../cbc:TaxAmount, '###.##0,00', 'european')"/>
                  <xsl:if test="../../cbc:TaxAmount/@currencyID">
                    <xsl:text> </xsl:text>
                    <xsl:if test="../../cbc:TaxAmount/@currencyID = 'TRL' or ../../cbc:TaxAmount/@currencyID = 'TRY'">
                      <xsl:text>TL</xsl:text>
                    </xsl:if>
                    <xsl:if test="../../cbc:TaxAmount/@currencyID != 'TRL' and ../../cbc:TaxAmount/@currencyID != 'TRY'">
                      <xsl:value-of select="../../cbc:TaxAmount/@currencyID"/>
                    </xsl:if>
                  </xsl:if>
                </xsl:for-each>
              </td>
            </tr>
          </xsl:for-each>
          <xsl:if
						test="sum(n1:Invoice/cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:TaxTypeCode=9015]/cbc:TaxableAmount)>0">
            <tr id="budgetContainerTr" align="right">
              <td id="budgetContainerDummyTd"/>
              <td id="lineTableBudgetTd" width="211px" align="right">
                <span style="font-weight:bold; ">
                  <xsl:text>Tevkifata Tabi İşlem Tutarı</xsl:text>
                </span>
              </td>
              <td id="lineTableBudgetTd" style="width:82px; " align="right">
                <xsl:value-of
									select="format-number(sum(n1:Invoice/cac:InvoiceLine[cac:TaxTotal/cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme/cbc:TaxTypeCode=9015]/cbc:LineExtensionAmount), '###.##0,00', 'european')"/>
                <xsl:if test="n1:Invoice/cbc:DocumentCurrencyCode = 'TRL'">
                  <xsl:text>TL</xsl:text>
                </xsl:if>
                <xsl:if test="n1:Invoice/cbc:DocumentCurrencyCode != 'TRL'">
                  <xsl:value-of select="n1:Invoice/cbc:DocumentCurrencyCode"/>
                </xsl:if>
              </td>
            </tr>
            <tr id="budgetContainerTr" align="right">
              <td id="budgetContainerDummyTd"/>
              <td id="lineTableBudgetTd" width="211px" align="right">
                <span style="font-weight:bold; ">
                  <xsl:text>Tevkifata Tabi İşlem Üzerinden Hes. KDV</xsl:text>
                </span>
              </td>
              <td id="lineTableBudgetTd" style="width:82px; " align="right">
                <xsl:value-of
									select="format-number(sum(n1:Invoice/cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:TaxTypeCode=9015]/cbc:TaxableAmount), '###.##0,00', 'european')"/>
                <xsl:if test="n1:Invoice/cbc:DocumentCurrencyCode = 'TRL'">
                  <xsl:text>TL</xsl:text>
                </xsl:if>
                <xsl:if test="n1:Invoice/cbc:DocumentCurrencyCode != 'TRL'">
                  <xsl:value-of select="n1:Invoice/cbc:DocumentCurrencyCode"/>
                </xsl:if>
              </td>
            </tr>
          </xsl:if>
          <xsl:if test = "n1:Invoice/cac:InvoiceLine[cac:WithholdingTaxTotal/cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme]">
            <tr id="budgetContainerTr" align="right">
              <td id="budgetContainerDummyTd"/>
              <td id="lineTableBudgetTd" width="211px" align="right">
                <span style="font-weight:bold; ">
                  <xsl:text>Tevkifata Tabi İşlem Tutarı</xsl:text>
                </span>
              </td>
              <td id="lineTableBudgetTd" style="width:82px; " align="right">
                <xsl:if test = "n1:Invoice/cac:InvoiceLine[cac:WithholdingTaxTotal/cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme]">
                  <xsl:value-of
										select="format-number(sum(n1:Invoice/cac:InvoiceLine[cac:WithholdingTaxTotal/cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme]/cbc:LineExtensionAmount), '###.##0,00', 'european')"/>
                </xsl:if>
                <xsl:if test = "//n1:Invoice/cac:TaxTotal/cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme/cbc:TaxTypeCode=&apos;9015&apos;">
                  <xsl:value-of
										select="format-number(sum(n1:Invoice/cac:InvoiceLine[cac:TaxTotal/cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme/cbc:TaxTypeCode=9015]/cbc:LineExtensionAmount), '###.##0,00', 'european')"/>
                </xsl:if>
                <xsl:if test="n1:Invoice/cbc:DocumentCurrencyCode = 'TRL' or n1:Invoice/cbc:DocumentCurrencyCode = 'TRY'">
                  <xsl:text>TL</xsl:text>
                </xsl:if>
                <xsl:if test="n1:Invoice/cbc:DocumentCurrencyCode != 'TRL' and n1:Invoice/cbc:DocumentCurrencyCode != 'TRY'">
                  <xsl:value-of select="n1:Invoice/cbc:DocumentCurrencyCode"/>
                </xsl:if>
              </td>
            </tr>
            <tr id="budgetContainerTr" align="right">
              <td id="budgetContainerDummyTd"/>
              <td id="lineTableBudgetTd" width="211px" align="right">
                <span style="font-weight:bold; ">
                  <xsl:text>Tevkifata Tabi İşlem Üzerinden Hes. KDV</xsl:text>
                </span>
              </td>
              <td id="lineTableBudgetTd" style="width:82px; " align="right">
                <xsl:if test = "n1:Invoice/cac:InvoiceLine[cac:WithholdingTaxTotal/cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme]">
                  <xsl:value-of
										select="format-number(sum(n1:Invoice/cac:WithholdingTaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme]/cbc:TaxableAmount), '###.##0,00', 'european')"/>
                </xsl:if>
                <xsl:if test = "//n1:Invoice/cac:TaxTotal/cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme/cbc:TaxTypeCode=&apos;9015&apos;">
                  <xsl:value-of
										select="format-number(sum(n1:Invoice/cac:TaxTotal/cac:TaxSubtotal[cac:TaxCategory/cac:TaxScheme/cbc:TaxTypeCode=9015]/cbc:TaxableAmount), '###.##0,00', 'european')"/>
                </xsl:if>
                <xsl:if test="n1:Invoice/cbc:DocumentCurrencyCode = 'TRL' or n1:Invoice/cbc:DocumentCurrencyCode = 'TRY'">
                  <xsl:text>TL</xsl:text>
                </xsl:if>
                <xsl:if test="n1:Invoice/cbc:DocumentCurrencyCode != 'TRL' and n1:Invoice/cbc:DocumentCurrencyCode != 'TRY'">
                  <xsl:value-of select="n1:Invoice/cbc:DocumentCurrencyCode"/>
                </xsl:if>
              </td>
            </tr>
          </xsl:if>
          <tr id="budgetContainerTr" align="right">
            <td id="budgetContainerDummyTd"/>
            <td id="lineTableBudgetTd" width="200px" align="right">
              <span style="font-weight:bold; ">
                <xsl:text>Vergiler Dahil Toplam Tutar</xsl:text>
              </span>
            </td>
            <td id="lineTableBudgetTd" style="width:82px; " align="right">
              <xsl:for-each select="n1:Invoice/cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount">
                <xsl:call-template name="Curr_Type"/>
              </xsl:for-each>
            </td>
          </tr>
          <tr id="budgetContainerTr" align="right">
            <td id="budgetContainerDummyTd"/>
            <td id="lineTableBudgetTd" width="200px" align="right">
              <span style="font-weight:bold; ">
                <xsl:text>Ödenecek Tutar</xsl:text>
              </span>
            </td>
            <td id="lineTableBudgetTd" style="width:82px; " align="right">
              <xsl:for-each select="n1:Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount">
                <xsl:call-template name="Curr_Type"/>
              </xsl:for-each>
            </td>
          </tr>
          <xsl:if
						test="//n1:Invoice/cac:LegalMonetaryTotal/cbc:LineExtensionAmount/@currencyID != 'TRL' and //n1:Invoice/cac:LegalMonetaryTotal/cbc:LineExtensionAmount/@currencyID != 'TRY'">
            <tr align="right">
              <td/>
              <td id="lineTableBudgetTd" align="right" width="200px">
                <span style="font-weight:bold; ">
                  <xsl:text>Mal Hizmet Toplam Tutarı(TL)</xsl:text>
                </span>
              </td>
              <td id="lineTableBudgetTd" style="width:81px; " align="right">
                <xsl:value-of
									select="format-number(//n1:Invoice/cac:LegalMonetaryTotal/cbc:LineExtensionAmount * //n1:Invoice/cac:PricingExchangeRate/cbc:CalculationRate, '###.##0,00', 'european')"/>
                <xsl:text> TL</xsl:text>
              </td>
            </tr>
            <tr id="budgetContainerTr" align="right">
              <td/>
              <td id="lineTableBudgetTd" width="200px" align="right">
                <span style="font-weight:bold; ">
                  <xsl:text>Vergiler Dahil Toplam Tutar(TL)</xsl:text>
                </span>
              </td>
              <td id="lineTableBudgetTd" style="width:82px; " align="right">
                <xsl:value-of
									select="format-number(//n1:Invoice/cac:LegalMonetaryTotal/cbc:TaxInclusiveAmount * //n1:Invoice/cac:PricingExchangeRate/cbc:CalculationRate, '###.##0,00', 'european')"/>
                <xsl:text> TL</xsl:text>
              </td>
            </tr>
            <tr align="right">
              <td/>
              <td id="lineTableBudgetTd" width="200px" align="right">
                <span style="font-weight:bold; ">
                  <xsl:text>Ödenecek Tutar(TL)</xsl:text>
                </span>
              </td>
              <td id="lineTableBudgetTd" style="width:82px; " align="right">
                <xsl:value-of
									select="format-number(//n1:Invoice/cac:LegalMonetaryTotal/cbc:PayableAmount * //n1:Invoice/cac:PricingExchangeRate/cbc:CalculationRate, '###.##0,00', 'european')"/>
                <xsl:text> TL</xsl:text>
              </td>
            </tr>
          </xsl:if>
        </table>
        <br/>

        <table id="notesTable" width="800" align="left" height="50">
          <tbody>
            <tr align="left">
              <td id="notesTableTd">
                <xsl:for-each select="//n1:Invoice/cac:TaxTotal/cac:TaxSubtotal">
                  <xsl:if	test="cac:TaxCategory/cac:TaxScheme/cbc:TaxTypeCode='0015' and cac:TaxCategory/cbc:TaxExemptionReason">
                    <b>&#160;&#160;&#160;&#160;&#160; Vergi İstisna Muafiyet Sebebi: </b>
                    <xsl:value-of select="cac:TaxCategory/cbc:TaxExemptionReasonCode"/>
                    <xsl:text>-</xsl:text>
                    <xsl:value-of select="cac:TaxCategory/cbc:TaxExemptionReason"/>
                    <br/>
                  </xsl:if>
                </xsl:for-each>
                <xsl:if	test="//n1:Invoice/cbc:InvoiceTypeCode = 'IHRACKAYITLI'">
                  <xsl:for-each select="//n1:Invoice/cac:TaxTotal/cac:TaxSubtotal">
                    <xsl:if	test="cac:TaxCategory/cbc:TaxExemptionReason">
                      <b>&#160;&#160;&#160;&#160;&#160; İhraç Kayıtlı Fatura Sebebi: </b>
                      <xsl:value-of select="cac:TaxCategory/cbc:TaxExemptionReasonCode"/>
                      <xsl:text>-</xsl:text>
                      <xsl:value-of select="cac:TaxCategory/cbc:TaxExemptionReason"/>
                      <br/>
                    </xsl:if>
                  </xsl:for-each>
                </xsl:if>
                <xsl:for-each select="//n1:Invoice/cac:WithholdingTaxTotal/cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme">
                  <b>&#160;&#160;&#160;&#160;&#160; Tevkifat Sebebi: </b>
                  <xsl:value-of select="cbc:TaxTypeCode"/>
                  <xsl:text>-</xsl:text>
                  <xsl:value-of select="cbc:Name"/>
                  <br/>
                </xsl:for-each>
                <xsl:if test="//n1:Invoice/cbc:Note">
                  <xsl:for-each select="//n1:Invoice/cbc:Note">

                    <xsl:value-of select="."/>
                    <br/>
                  </xsl:for-each>
                </xsl:if>
                <xsl:if test="//n1:Invoice/cac:PaymentMeans/cbc:PaymentDueDate">
                  <xsl:text>Fatura Vadesi : </xsl:text>
                  <xsl:for-each
                    select="n1:Invoice/cac:PaymentMeans">
                    <xsl:for-each select="cbc:PaymentDueDate">
                      <xsl:value-of select="substring(.,9,2)"
                      />-<xsl:value-of select="substring(.,6,2)"
                      />-<xsl:value-of select="substring(.,1,4)"/>
                    </xsl:for-each>
                  </xsl:for-each>
                </xsl:if>
                <xsl:if test="//n1:Invoice/cac:PaymentMeans/cbc:InstructionNote">
                  <b>&#160;&#160;&#160;&#160;&#160; Ödeme Notu: </b>
                  <xsl:value-of
										select="//n1:Invoice/cac:PaymentMeans/cbc:InstructionNote"/>
                  <br/>
                </xsl:if>
                <xsl:if
									test="//n1:Invoice/cac:PaymentMeans/cac:PayeeFinancialAccount/cbc:PaymentNote">
                  <b>&#160;&#160;&#160;&#160;&#160; Hesap Açıklaması: </b>
                  <xsl:value-of
										select="//n1:Invoice/cac:PaymentMeans/cac:PayeeFinancialAccount/cbc:PaymentNote"/>
                  <br/>
                </xsl:if>
                <xsl:if test="//n1:Invoice/cac:PaymentTerms/cbc:Note">
                  <b>&#160;&#160;&#160;&#160;&#160; Ödeme Koşulu: </b>
                  <xsl:value-of select="//n1:Invoice/cac:PaymentTerms/cbc:Note"/>
                  <br/>
                  <xsl:if test="not(//n1:Invoice/cac:DespatchDocumentReference)">
                    <br/>
                    <b>İrsaliye yerine geçer.</b>
                  </xsl:if>
                  <br/>
                  <b>e-Arşiv izni kapsamında elektronik ortamda iletilmiştir.</b>
                  <br/>
                </xsl:if>

                <xsl:if test="//n1:Invoice/cac:BuyerCustomerParty/cac:Party/cac:PartyIdentification/cbc:ID[@schemeID='PARTYTYPE']='TAXFREE' and //n1:Invoice/cac:TaxRepresentativeParty/cac:PartyTaxScheme/cbc:ExemptionReasonCode">
                  <br/>
                  <b>&#160;&#160;&#160;&#160;&#160; VAT OFF - NO CASH REFUND </b>
                </xsl:if>

                <br/>
                <b>. </b>
                <br/>
                <br/>
                <div id="staticFooter" align="center">						
									
								<table id="hesapBilgileriTable" style="border-collapse: collapse;font-size: 10px;font-weight: normal;">
                    <tr id="IBANHesapBaslik" height="20" style="color:#FFFFFF;font-weight:bold;font-size=11px;border-bottom: 2px solid #FFFFFF">
                      <td id="lineTableTd" align="center" style="background: #D3051A;border-bottom: 2px solid black">Banka Adı </td>
                      <td id="lineTableTd" align="center" style="background: #D3051A;border-bottom: 2px solid black">Şubesi</td>
                      <td id="lineTableTd" style="background: #D3051A;border-bottom: 2px solid black" align="center">IBAN Kodu</td>

                      <td id="lineTableTd" style="background: #D3051A;border-bottom: 2px solid black" align="center">Döviz Cinsi</td>
                    </tr>
                     <tr id="BankalineTableTR" height="20">
                      <td id="BankalineTableTR" width="190" align="center"><b>GARANTİ BANKASI </b></td>
                      <td id="BankalineTableTR" width="190" align="center"><b>FINDIKZADE</b></td>
                      <td id="BankalineTableTR" width="300" align="center"><b>TR50 0006 2000 4370 0006 2897 35</b></td>
                      <td id="BankalineTableTR" width="80" align="center"><b>TL</b></td>
                    </tr>
                                       <tr id="BankalineTableTR" height="20">
                      <td id="BankalineTableTR" width="190" align="center"><b>HALK BANKASI </b></td>
                      <td id="BankalineTableTR" width="190" align="center"><b>BULVAR ESENYURT İSTANBUL</b></td>
                      <td id="BankalineTableTR" width="300" align="center"><b>TR57 0001 2001 6400 0010 1001 16</b></td>
                      <td id="BankalineTableTR" width="80" align="center"><b>TL</b></td>
                    </tr>

                    <tr id="BankalineTableTR" height="20">
                      <td id="BankalineTableTR" width="190" align="center"><b>YAPI VE KREDİ BANKASI	</b></td>
                      <td id="BankalineTableTR" width="190" align="center"><b>FİRUZKÖY </b></td>
                      <td id="BankalineTableTR" width="300" align="center"><b>TR98 0006 7010 0000 0046 6688 17</b></td>
                      <td id="BankalineTableTR" width="80" align="center"><b>TL</b></td>
                    </tr>

               

                    
										
									</table>
									
								</div>
								<br/>
							</td>
						</tr>
					</tbody>
				</table>
			</body>
		</html>
  </xsl:template>
  <xsl:template match="//n1:Invoice/cac:InvoiceLine">
    <tr id="lineTableTr">
      <td id="lineTableTd">
        <xsl:text>&#160;</xsl:text>
        <xsl:value-of select="./cbc:ID"/>
      </td>
      <td id="lineTableTd">
        <xsl:text>&#160;</xsl:text>
        <xsl:value-of select="./cac:Item/cac:SellersItemIdentification/cbc:ID"/>
      </td>
      <td id="lineTableTd">
        <xsl:text>&#160;</xsl:text>
        <xsl:value-of select="./cac:Item/cbc:Name"/>
      </td>
      <td id="lineTableTd">
        <xsl:text>&#160;</xsl:text>
        <xsl:value-of select="./cbc:Note"/>
      </td>
      <td id="lineTableTd" align="right">
        <xsl:text>&#160;</xsl:text>
        <xsl:value-of
					select="format-number(./cbc:InvoicedQuantity, '###.###,####', 'european')"/>
        <xsl:if test="./cbc:InvoicedQuantity/@unitCode">
          <xsl:for-each select="./cbc:InvoicedQuantity">
            <xsl:text> </xsl:text>
            <xsl:choose>
              <xsl:when test="@unitCode  = '26'">
                <xsl:text>ton</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'BX'">
                <xsl:text>Kutu</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'LTR'">
                <xsl:text>lt</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'NIU'">
                <xsl:text>Adet</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'KGM'">
                <xsl:text>kg</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'KJO'">
                <xsl:text>kJ</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'GRM'">
                <xsl:text>g</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'MGM'">
                <xsl:text>mg</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'NT'">
                <xsl:text>Net Ton</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'GT'">
                <xsl:text>Gross Ton</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'MTR'">
                <xsl:text>m</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'MMT'">
                <xsl:text>mm</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'KTM'">
                <xsl:text>km</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'MLT'">
                <xsl:text>ml</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'MMQ'">
                <xsl:text>mm3</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'CLT'">
                <xsl:text>cl</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'CMK'">
                <xsl:text>cm2</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'CMQ'">
                <xsl:text>cm3</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'CMT'">
                <xsl:text>cm</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'MTK'">
                <xsl:text>m2</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'MTQ'">
                <xsl:text>m3</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'DAY'">
                <xsl:text> Gün</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'MON'">
                <xsl:text> Ay</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'PA'">
                <xsl:text> Paket</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'KWH'">
                <xsl:text> KWH</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'ANN'">
                <xsl:text> Yıl</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'HUR'">
                <xsl:text> Saat</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'D61'">
                <xsl:text> Dakika</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'D62'">
                <xsl:text> Saniye</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'CCT'">
                <xsl:text> Ton baş.taşıma kap.</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'D30'">
                <xsl:text> Brüt kalori</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'D40'">
                <xsl:text> 1000 lt</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'LPA'">
                <xsl:text> saf alkol lt</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'B32'">
                <xsl:text> kg.m2</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'NCL'">
                <xsl:text> hücre adet</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'PR'">
                <xsl:text> Çift</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'R9'">
                <xsl:text> 1000 m3</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'SET'">
                <xsl:text> Set</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'T3'">
                <xsl:text> 1000 adet</xsl:text>
              </xsl:when>
              <xsl:when test="@unitCode  = 'C62'">
                <xsl:text> Adet</xsl:text>
              </xsl:when>
            </xsl:choose>
          </xsl:for-each>
        </xsl:if>
      </td>
      <td id="lineTableTd" align="right">
        <xsl:text>&#160;</xsl:text>
        <xsl:value-of
					select="format-number(./cac:Price/cbc:PriceAmount, '###.##0,########', 'european')"/>
        <xsl:if test="./cac:Price/cbc:PriceAmount/@currencyID">
          <xsl:text> </xsl:text>
          <xsl:if test="./cac:Price/cbc:PriceAmount/@currencyID = &quot;TRL&quot; or ./cac:Price/cbc:PriceAmount/@currencyID = &quot;TRY&quot;">
            <xsl:text>TL</xsl:text>
          </xsl:if>
          <xsl:if test="./cac:Price/cbc:PriceAmount/@currencyID != &quot;TRL&quot; and ./cac:Price/cbc:PriceAmount/@currencyID != &quot;TRY&quot;">
            <xsl:value-of select="./cac:Price/cbc:PriceAmount/@currencyID"/>
          </xsl:if>
        </xsl:if>
      </td>
	  
      
      <td id="lineTableTd" align="right">
        <xsl:text>&#160;</xsl:text>
        <xsl:for-each select="./cac:TaxTotal/cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme">
          <xsl:if test="cbc:TaxTypeCode='0015' ">
            <xsl:text> </xsl:text>
            <xsl:if test="../../cbc:Percent">
              <xsl:text> %</xsl:text>
              <xsl:value-of select="format-number(../../cbc:Percent, '###.##0,00', 'european')"/>
            </xsl:if>
          </xsl:if>
        </xsl:for-each>
      </td>
      <td id="lineTableTd" align="right">
        <xsl:text>&#160;</xsl:text>
        <xsl:for-each
					select="./cac:TaxTotal/cac:TaxSubtotal/cac:TaxCategory/cac:TaxScheme">
          <xsl:if test="cbc:TaxTypeCode='0015' ">
            <xsl:text> </xsl:text>
            <xsl:for-each select="../../cbc:TaxAmount">
              <xsl:call-template name="Curr_Type"/>
            </xsl:for-each>
          </xsl:if>
        </xsl:for-each>
      </td>
    
      <td id="lineTableTd" align="right">
        <xsl:text>&#160;</xsl:text>
        <xsl:for-each select="cbc:LineExtensionAmount">
          <xsl:call-template name="Curr_Type"/>
        </xsl:for-each>
      </td>
    </tr>
  </xsl:template>
  <xsl:template match="//cbc:IssueDate">
    <xsl:value-of select="substring(.,9,2)"/>-<xsl:value-of select="substring(.,6,2)"/>-<xsl:value-of select="substring(.,1,4)"/>
  </xsl:template>
  <xsl:template match="//n1:Invoice">
   
  </xsl:template>
  <xsl:template name="Party_Title" >
    <xsl:param name="PartyType" />
    <td style="width:469px; " align="left">
      <xsl:if test="cac:PartyName">
        <xsl:value-of select="cac:PartyName/cbc:Name"/>
        <br/>
      </xsl:if>
      <xsl:for-each select="cac:Person">
        <xsl:for-each select="cbc:Title">
          <xsl:apply-templates/>
          <xsl:text>&#160;</xsl:text>
        </xsl:for-each>
        <xsl:for-each select="cbc:FirstName">
          <xsl:apply-templates/>
          <xsl:text>&#160;</xsl:text>
        </xsl:for-each>
        <xsl:for-each select="cbc:MiddleName">
          <xsl:apply-templates/>
          <xsl:text>&#160; </xsl:text>
        </xsl:for-each>
        <xsl:for-each select="cbc:FamilyName">
          <xsl:apply-templates/>
          <xsl:text>&#160;</xsl:text>
        </xsl:for-each>
        <xsl:for-each select="cbc:NameSuffix">
          <xsl:apply-templates/>
        </xsl:for-each>
        <xsl:if test="$PartyType='TAXFREE'">
          <br/>
          <xsl:text>Pasaport No: </xsl:text>
          <xsl:value-of select="cac:IdentityDocumentReference/cbc:ID"/>
          <br/>
          <xsl:text>Ülkesi: </xsl:text>
          <xsl:value-of select="cbc:NationalityID"/>
        </xsl:if>
      </xsl:for-each>
    </td>
  </xsl:template>
  <xsl:template name="Party_Adress" >
    <xsl:param name="PartyType" />
    <td style="width:469px; " align="left">
      <xsl:for-each select="cac:PostalAddress">
        <xsl:for-each select="cbc:StreetName">
          <xsl:apply-templates/>
          <xsl:text>&#160;</xsl:text>
        </xsl:for-each>
        <xsl:for-each select="cbc:BuildingName">
          <xsl:apply-templates/>
        </xsl:for-each>
        <xsl:for-each select="cbc:BuildingNumber">
          <xsl:text> No:</xsl:text>
          <xsl:apply-templates/>
          <xsl:text>&#160;</xsl:text>
        </xsl:for-each>
        <br/>
        <xsl:for-each select="cbc:Room">
          <xsl:text>Kapı No:</xsl:text>
          <xsl:apply-templates/>
          <xsl:text>&#160;</xsl:text>
        </xsl:for-each>
        <br/>
        <xsl:for-each select="cbc:PostalZone">
          <xsl:apply-templates/>
          <xsl:text>&#160;</xsl:text>
        </xsl:for-each>
        <xsl:for-each select="cbc:CitySubdivisionName">
          <xsl:apply-templates/>
          <xsl:text>/ </xsl:text>
        </xsl:for-each>
        <xsl:for-each select="cbc:CityName">
          <xsl:apply-templates/>
          <xsl:text>&#160;</xsl:text>
        </xsl:for-each>
        <xsl:if test="$PartyType='TAXFREE'">
          <br/>
          <xsl:value-of select="cac:Country/cbc:Name"/>
          <br/>
        </xsl:if>
      </xsl:for-each>
    </td>
  </xsl:template>
  <xsl:template name='Party_Other'>
    <xsl:param name="PartyType" />
    <xsl:for-each select="cbc:WebsiteURI">
      <tr align="left">
        <td>
          <xsl:text>Web Sitesi: </xsl:text>
          <xsl:value-of select="."/>
        </td>
      </tr>
    </xsl:for-each>
    <xsl:for-each select="cac:Contact/cbc:ElectronicMail">
      <tr align="left">
        <td>
          <xsl:text>E-Posta: </xsl:text>
          <xsl:value-of select="."/>
        </td>
      </tr>
    </xsl:for-each>
    <xsl:for-each select="cac:Contact">
      <xsl:if test="cbc:Telephone or cbc:Telefax">
        <tr align="left">
          <td style="width:469px; " align="left">
            <xsl:for-each select="cbc:Telephone">
              <xsl:text>Tel: </xsl:text>
              <xsl:apply-templates/>
            </xsl:for-each>
            <xsl:for-each select="cbc:Telefax">
              <xsl:text> Faks: </xsl:text>
              <xsl:apply-templates/>
            </xsl:for-each>
            <xsl:text>&#160;</xsl:text>
          </td>
        </tr>
      </xsl:if>
    </xsl:for-each>
    <xsl:if test="$PartyType!='TAXFREE'">
      <xsl:for-each select="cac:PartyTaxScheme/cac:TaxScheme/cbc:Name">
        <tr align="left">
          <td>
            <xsl:text>Vergi Dairesi: </xsl:text>
            <xsl:apply-templates/>
          </td>
        </tr>
      </xsl:for-each>
      <xsl:for-each select="cac:PartyIdentification">
        <tr align="left">
          <td>
            <xsl:value-of select="cbc:ID/@schemeID"/>
            <xsl:text>: </xsl:text>
            <xsl:value-of select="cbc:ID"/>
          </td>
        </tr>
      </xsl:for-each>
    </xsl:if>
  </xsl:template>
  <xsl:template name="Curr_Type">
    <xsl:value-of select="format-number(., '###.##0,00', 'european')"/>
    <xsl:if	test="@currencyID">
      <xsl:text> </xsl:text>
      <xsl:choose>
        <xsl:when test="@currencyID = 'TRL' or @currencyID = 'TRY'">
          <xsl:text>TL</xsl:text>
        </xsl:when>
        <xsl:otherwise>
          <xsl:value-of select="@currencyID"/>
        </xsl:otherwise>
      </xsl:choose>
    </xsl:if>
  </xsl:template>
</xsl:stylesheet>