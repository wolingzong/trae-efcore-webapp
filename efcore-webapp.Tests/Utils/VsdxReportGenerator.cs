using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Packaging;
using System.Text;

namespace EfCoreWebApp.Tests.Utils;

public static class VsdxReportGenerator
{
    private const string NsVisio = "http://schemas.microsoft.com/office/visio/2012/main";
    private const string NsRels = "http://schemas.openxmlformats.org/package/2006/relationships";
    private const string ContentTypeMain = "application/vnd.ms-visio.drawing.main+xml";
    private const string ContentTypePage = "application/vnd.ms-visio.page+xml";
    private const string ContentTypeMaster = "application/vnd.ms-visio.master+xml";
    private const string ContentTypeRel = "application/vnd.openxmlformats-package.relationships+xml";

    public static void GenerateDiagram(string vsdxPath, string featureFilePath)
    {
        var featureContent = File.ReadAllText(featureFilePath);
        var feature = ParseFeatureFile(featureContent);

        if (File.Exists(vsdxPath)) File.Delete(vsdxPath);

        using (var package = Package.Open(vsdxPath, FileMode.Create))
        {
            // 1. visio/masters/master1.xml (Rectangle Master)
            var rectMasterUri = PackUriHelper.CreatePartUri(new Uri("/visio/masters/master1.xml", UriKind.Relative));
            var rectMasterPart = package.CreatePart(rectMasterUri, ContentTypeMaster);
            WritePartContent(rectMasterPart, GenerateRectangleMasterXml());

            // 2. visio/masters/master2.xml (Connector Master)
            var connMasterUri = PackUriHelper.CreatePartUri(new Uri("/visio/masters/master2.xml", UriKind.Relative));
            var connMasterPart = package.CreatePart(connMasterUri, ContentTypeMaster);
            WritePartContent(connMasterPart, GenerateConnectorMasterXml());

            // 3. visio/document.xml (Main Part)
            var docUri = PackUriHelper.CreatePartUri(new Uri("/visio/document.xml", UriKind.Relative));
            var docPart = package.CreatePart(docUri, ContentTypeMain);
            WritePartContent(docPart, GenerateDocumentXmlWithMasters());
            package.CreateRelationship(docPart.Uri, TargetMode.Internal, "http://schemas.microsoft.com/visio/2010/relationships/document", "rId1");

            // 4. visio/pages/page1.xml
            var pageUri = PackUriHelper.CreatePartUri(new Uri("/visio/pages/page1.xml", UriKind.Relative));
            var pagePart = package.CreatePart(pageUri, ContentTypePage);
            WritePartContent(pagePart, GeneratePageXml(feature));
            docPart.CreateRelationship(pageUri, TargetMode.Internal, "http://schemas.microsoft.com/visio/2010/relationships/page", "rIdPage1");

            // 5. visio/windows.xml
            var winUri = PackUriHelper.CreatePartUri(new Uri("/visio/windows.xml", UriKind.Relative));
            var winPart = package.CreatePart(winUri, "application/vnd.ms-visio.windows+xml");
            WritePartContent(winPart, GenerateWindowsXml());
            docPart.CreateRelationship(winUri, TargetMode.Internal, "http://schemas.microsoft.com/visio/2010/relationships/windows", "rIdWindows");

            // 6. Create relationships for masters
            docPart.CreateRelationship(rectMasterUri, TargetMode.Internal, "http://schemas.microsoft.com/visio/2010/relationships/master", "rIdMaster1");
            docPart.CreateRelationship(connMasterUri, TargetMode.Internal, "http://schemas.microsoft.com/visio/2010/relationships/master", "rIdMaster2");

            // 7. app.xml (Application Properties)
            var appUri = PackUriHelper.CreatePartUri(new Uri("/docProps/app.xml", UriKind.Relative));
            var appPart = package.CreatePart(appUri, "application/vnd.openxmlformats-officedocument.extended-properties+xml");
            WritePartContent(appPart, GenerateAppPropertiesXml());
            package.CreateRelationship(appPart.Uri, TargetMode.Internal, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties", "rId2");

            // 8. core.xml (Core Properties)
            var coreUri = PackUriHelper.CreatePartUri(new Uri("/docProps/core.xml", UriKind.Relative));
            var corePart = package.CreatePart(coreUri, "application/vnd.openxmlformats-package.core-properties+xml");
            WritePartContent(corePart, GenerateCorePropertiesXml());
            package.CreateRelationship(corePart.Uri, TargetMode.Internal, "http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties", "rId3");

            // 9. Create explicit document relationships file for better compatibility
            CreateDocumentRelationshipsFile(package, docPart);

            // 10. Create empty relationships for masters (Compatibility fix - REMOVED as it causes corruption if empty)
            // CreateMasterRelationshipsFile(package, "/visio/masters/_rels/master1.xml.rels");
            // CreateMasterRelationshipsFile(package, "/visio/masters/_rels/master2.xml.rels");
        }
    }

    private static void CreateMasterRelationshipsFile(Package package, string uriPath)
    {
        var partUri = PackUriHelper.CreatePartUri(new Uri(uriPath, UriKind.Relative));
        if (package.PartExists(partUri)) return;

        var part = package.CreatePart(partUri, ContentTypeRel);
        var xml = $@"<?xml version='1.0' encoding='UTF-8' standalone='yes'?>
<Relationships xmlns='{NsRels}'>
</Relationships>";
        WritePartContent(part, xml);
    }

    private static void WritePartContent(PackagePart part, string content)
    {
        using var stream = part.GetStream(FileMode.Create);
        using var writer = new StreamWriter(stream, new UTF8Encoding(false));
        writer.Write(content);
    }

    /// <summary>
    /// 创建显式的文档关系文件以增强兼容性
    /// </summary>
    private static void CreateDocumentRelationshipsFile(Package package, PackagePart docPart)
    {
        var docRelsUri = PackUriHelper.CreatePartUri(new Uri("/visio/_rels/document.xml.rels", UriKind.Relative));
        var docRelsPart = package.CreatePart(docRelsUri, ContentTypeRel);
        
        var docRelsXml = $@"<?xml version='1.0' encoding='UTF-8' standalone='yes'?>
<Relationships xmlns='{NsRels}'>
    <Relationship Id='rIdPage1' Type='http://schemas.microsoft.com/visio/2010/relationships/page' Target='pages/page1.xml'/>
    <Relationship Id='rIdWindows' Type='http://schemas.microsoft.com/visio/2010/relationships/windows' Target='windows.xml'/>
    <Relationship Id='rIdMaster1' Type='http://schemas.microsoft.com/visio/2010/relationships/master' Target='masters/master1.xml'/>
    <Relationship Id='rIdMaster2' Type='http://schemas.microsoft.com/visio/2010/relationships/master' Target='masters/master2.xml'/>
</Relationships>";
        
        WritePartContent(docRelsPart, docRelsXml);
    }

    private static string GenerateAppPropertiesXml()
    {
        return @"<?xml version='1.0' encoding='UTF-8' standalone='yes'?>
<Properties xmlns='http://schemas.openxmlformats.org/officeDocument/2006/extended-properties' xmlns:vt='http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes'>
    <Application>Test Report Generator</Application>
    <DocSecurity>0</DocSecurity>
    <ScaleCrop>false</ScaleCrop>
    <SharedDoc>false</SharedDoc>
    <HyperlinksChanged>false</HyperlinksChanged>
    <AppVersion>16.0000</AppVersion>
</Properties>";
    }

    private static string GenerateCorePropertiesXml()
    {
        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        return $@"<?xml version='1.0' encoding='UTF-8' standalone='yes'?>
<cp:coreProperties xmlns:cp='http://schemas.openxmlformats.org/package/2006/metadata/core-properties' xmlns:dc='http://purl.org/dc/elements/1.1/' xmlns:dcterms='http://purl.org/dc/terms/' xmlns:dcmitype='http://purl.org/dc/dcmitype/' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance'>
    <dc:title>Test Hierarchy Diagram</dc:title>
    <dc:creator>Test Report Generator</dc:creator>
    <dcterms:created xsi:type='dcterms:W3CDTF'>{now}</dcterms:created>
    <dcterms:modified xsi:type='dcterms:W3CDTF'>{now}</dcterms:modified>
</cp:coreProperties>";
    }

    private static string GenerateWindowsXml()
    {
        return $@"<?xml version='1.0' encoding='UTF-8' standalone='yes'?>
<Windows xmlns='{NsVisio}'>
    <Window ID='0' WindowType='Drawing' WindowState='1073741824' ViewScale='1' ViewCenterX='4.1' ViewCenterY='5.5'>
        <ShowRulers>1</ShowRulers>
        <ShowGrid>1</ShowGrid>
        <ShowPageBreaks>0</ShowPageBreaks>
        <ShowGuides>1</ShowGuides>
        <ShowConnectionPoints>1</ShowConnectionPoints>
    </Window>
</Windows>";
    }



    private static string GeneratePageXml(FeatureInfo feature)
    {
        var sb = new StringBuilder();
        var connects = new StringBuilder();

        sb.AppendLine($@"<?xml version='1.0' encoding='UTF-8' standalone='yes'?>
<PageContents xmlns='{NsVisio}' xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships' xml:space='preserve'>
    <PageSheet>
        <Cell N='PageWidth' V='8.27' U='IN'/>
        <Cell N='PageHeight' V='11.69' U='IN'/>
        <Cell N='PageScale' V='1' U='IN'/>
        <Cell N='DrawingScale' V='1' U='IN'/>
        <Cell N='ShdwObliqueAngle' V='0'/>
        <Cell N='ShdwScaleFactor' V='1'/>
    </PageSheet>
    <Shapes>");

        int shapeId = 1;
        
        // --- 1. Draw Feature Node (Top Center) ---
        double rootX = 4.1; // Center of 8.27
        double rootY = 10.0;
        double width = 2.0;
        double height = 0.8;
        
        int rootShapeId = shapeId++;
        sb.Append(CreateShapeXmlRobust(rootShapeId, feature.Name, rootX, rootY, width, height, 8)); // Blue Index 8

        // --- 2. Draw Scenarios (Row below Feature) ---
        double scenarioY = rootY - 1.5;
        int scenarioCount = feature.Scenarios.Count;
        double scenarioGap = 3.0;
        double startX = rootX - ((scenarioCount - 1) * scenarioGap) / 2.0;

        for (int i = 0; i < scenarioCount; i++)
        {
            var scenario = feature.Scenarios[i];
            double scenarioX = startX + (i * scenarioGap);
            
            int scenarioShapeId = shapeId++;
            sb.Append(CreateShapeXmlRobust(scenarioShapeId, scenario.Name, scenarioX, scenarioY, 2.5, 0.8, 9)); // Green Index 9
            
            // Connect Feature -> Scenario (Bottom of Feature to Top of Scenario)
            int connectorId = shapeId++;
            // Calculate connection points
            double x1 = rootX;
            double y1 = rootY - (height / 2.0);
            double x2 = scenarioX;
            double y2 = scenarioY + (0.8 / 2.0); // 0.8 is height of scenario
            
            sb.Append(CreateConnectorXmlRobust(connectorId, x1, y1, x2, y2));
            AddConnection(connects, connectorId, rootShapeId, scenarioShapeId);

            // --- 3. Draw Steps (Column below Scenario) ---
            double stepY = scenarioY - 1.2;
            double stepGap = 0.8;
            double stepWidth = 2.0;
            double stepHeight = 0.6;
            
            int previousShapeId = scenarioShapeId;
            double prevX = scenarioX;
            double prevY = scenarioY; // Center of previous shape
            double prevHeight = 0.8;
            
            foreach (var step in scenario.Steps)
            {
                int stepShapeId = shapeId++;
                string stepText = step;
                
                int colorIndex = 10; // Default Orange #FFC000
                if (step.StartsWith("前提") || step.StartsWith("Given")) colorIndex = 11; // #FFE699
                if (step.StartsWith("もし") || step.StartsWith("When")) colorIndex = 12; // #BDD7EE
                if (step.StartsWith("ならば") || step.StartsWith("Then")) colorIndex = 13; // #F8CBAD
                
                sb.Append(CreateShapeXmlRobust(stepShapeId, stepText, scenarioX, stepY, stepWidth, stepHeight, colorIndex));
                
                // Connect Previous -> Step
                connectorId = shapeId++;
                
                // From bottom of previous
                double cx1 = prevX;
                double cy1 = prevY - (prevHeight / 2.0);
                // To top of current
                double cx2 = scenarioX;
                double cy2 = stepY + (stepHeight / 2.0);

                sb.Append(CreateConnectorXmlRobust(connectorId, cx1, cy1, cx2, cy2));
                AddConnection(connects, connectorId, previousShapeId, stepShapeId);
                
                previousShapeId = stepShapeId;
                prevX = scenarioX;
                prevY = stepY;
                prevHeight = stepHeight;
                
                stepY -= stepGap;
            }
        }

        sb.AppendLine("    </Shapes>");
        
        if (connects.Length > 0)
        {
            sb.AppendLine("    <Connects>");
            sb.Append(connects.ToString());
            sb.AppendLine("    </Connects>");
        }

        sb.AppendLine("</PageContents>");
        return sb.ToString();
    }
    
    // AddConnection matches previous implementation if not shown here, assuming it's available or re-added if overwritten.
    private static void AddConnection(StringBuilder sb, int connectorId, int fromId, int toId)
    {
        sb.AppendLine($"<Connect FromSheet='{connectorId}' FromCell='BeginX' ToSheet='{fromId}' ToCell='PinX'/>");
        sb.AppendLine($"<Connect FromSheet='{connectorId}' FromCell='EndX' ToSheet='{toId}' ToCell='PinX'/>");
    }



    // Reuse parsing logic
    private static FeatureInfo ParseFeatureFile(string featureContent)
    {
        // Simple reuse from previous - copy paste needed since static classes
         var lines = featureContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var feature = new FeatureInfo();
        var currentScenario = new ScenarioInfo();
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("機能:") || trimmedLine.StartsWith("Feature:"))
                feature.Name = trimmedLine.Contains(":") ? trimmedLine.Substring(trimmedLine.IndexOf(':') + 1).Trim() : "Feature";
            else if (trimmedLine.StartsWith("シナリオ:") || trimmedLine.StartsWith("Scenario:")) {
                if (!string.IsNullOrEmpty(currentScenario.Name)) feature.Scenarios.Add(currentScenario);
                currentScenario = new ScenarioInfo { Name = trimmedLine.Contains(":") ? trimmedLine.Substring(trimmedLine.IndexOf(':') + 1).Trim() : trimmedLine };
            }
            else if (trimmedLine.StartsWith("前提") || trimmedLine.StartsWith("Given") || trimmedLine.StartsWith("When") || trimmedLine.StartsWith("Then"))
                currentScenario.Steps.Add(trimmedLine);
        }
        if (!string.IsNullOrEmpty(currentScenario.Name)) feature.Scenarios.Add(currentScenario);
        return feature;
    }

    /// <summary>
    /// 安全地截断文本，避免多字节字符问题
    /// </summary>
    private static string TruncateTextSafely(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        // 对于包含日文字符的文本，使用字符数而不是字节数
        // 但确保不会在代理对中间截断
        if (text.Length <= maxLength)
            return text;

        // 找到安全的截断点（避免在代理对中间截断）
        int truncateAt = maxLength - 2; // 为 ".." 留空间
        
        // 检查截断点是否在代理对中间
        if (char.IsHighSurrogate(text[truncateAt]) || 
            (truncateAt > 0 && char.IsLowSurrogate(text[truncateAt])))
        {
            truncateAt--;
        }

        return text.Substring(0, truncateAt) + "..";
    }

    /// <summary>
    /// 转义XML中的特殊字符
    /// </summary>
    private static string EscapeXml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    /// <summary>
    /// 创建更健壮的形状XML，使用Master模板
    /// </summary>
    /// <summary>
    /// Create Shape XML using compliant VSDX Cell syntax (not wrapped in XForm/nested tags).
    /// </summary>
    private static string CreateShapeXmlRobust(int id, string text, double x, double y, double width, double height, int colorIndex)
    {
        // Strictly ensure text is not null. 
        // User provided specific Japanese text in their request, so we must support full UTF-8.
        string safeText = EscapeXml(text ?? ""); 
        
        return $@"
        <Shape ID='{id}' Type='Shape' Master='0' MasterShape='5' LineStyle='0' FillStyle='0' TextStyle='0'>
            <Cell N='PinX' V='{x}' U='IN'/>
            <Cell N='PinY' V='{y}' U='IN'/>
            <Cell N='Width' V='{width}' U='IN'/>
            <Cell N='Height' V='{height}' U='IN'/>
            <Cell N='LocPinX' V='{width / 2}' U='IN'/>
            <Cell N='LocPinY' V='{height / 2}' U='IN'/>
            <Cell N='FillForegnd' V='{colorIndex}'/>
            <Cell N='FillPattern' V='1'/>
            <Text>{safeText}</Text>
        </Shape>";
    }

    /// <summary>
    /// 创建改进的连接器XML，使用连接器Master模板和动态几何
    /// </summary>
    /// <summary>
    /// Create Connector XML using compliant VSDX Cell syntax and explicit start/end coordinates.
    /// </summary>
    private static string CreateConnectorXmlRobust(int id, double x1, double y1, double x2, double y2)
    {
        // Enforce minimum width to prevent visibility issues
        double width = Math.Abs(x2 - x1);
        double height = Math.Abs(y2 - y1);
        width = Math.Max(width, 0.01);
        height = Math.Max(height, 0.01);

        double pinX = Math.Min(x1, x2) + (width / 2);
        double pinY = Math.Min(y1, y2) + (height / 2);
        
        return $@"
        <Shape ID='{id}' Type='Shape' Master='1' MasterShape='6' LineStyle='1' FillStyle='0' TextStyle='0'>
            <Cell N='PinX' V='{pinX}' U='IN'/>
            <Cell N='PinY' V='{pinY}' U='IN'/>
            <Cell N='Width' V='{width}' U='IN'/>
            <Cell N='Height' V='{height}' U='IN'/>
            <Cell N='LocPinX' V='{width / 2}' U='IN'/>
            <Cell N='LocPinY' V='{height / 2}' U='IN'/>
            <Cell N='BeginX' V='{x1}' U='IN'/>
            <Cell N='BeginY' V='{y1}' U='IN'/>
            <Cell N='EndX' V='{x2}' U='IN'/>
            <Cell N='EndY' V='{y2}' U='IN'/>
            <Cell N='LineWeight' V='0.02'/>

            <Cell N='EndArrow' V='2'/>
            <Cell N='ObjType' V='2'/>
        </Shape>";
    }



    /// <summary>
    /// 生成带有Masters定义的文档XML
    /// </summary>
    private static string GenerateDocumentXmlWithMasters()
    {
        return $@"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""yes""?>
<VisioDocument xmlns=""{NsVisio}"" xmlns:r=""http://schemas.openxmlformats.org/officeDocument/2006/relationships"">
    <DocumentSettings>
        <GlueSettings>9</GlueSettings>
        <SnapSettings>65847</SnapSettings>
        <SnapExtensions>34</SnapExtensions>
        <SnapAngles>0.0,0.261799387799149,0.523598775598299,0.785398163397448,1.04719755119660,1.30899693899575,1.57079632679490,1.83259571459405,2.09439510239320,2.35619449019234,2.61799387799149,2.87979326579064</SnapAngles>
        <DynamicGridEnabled>1</DynamicGridEnabled>
        <ProtectStyles>0</ProtectStyles>
        <ProtectShapes>0</ProtectShapes>
        <ProtectMasters>0</ProtectMasters>
        <ProtectBkgnds>0</ProtectBkgnds>
    </DocumentSettings>
    <Colors>
        <ColorEntry IX='0' RGB='#000000'/>
        <ColorEntry IX='1' RGB='#FFFFFF'/>
        <ColorEntry IX='2' RGB='#FF0000'/>
        <ColorEntry IX='3' RGB='#00FF00'/>
        <ColorEntry IX='4' RGB='#0000FF'/>
        <ColorEntry IX='5' RGB='#FFFF00'/>
        <ColorEntry IX='6' RGB='#FF00FF'/>
        <ColorEntry IX='7' RGB='#00FFFF'/>
        <ColorEntry IX='8' RGB='#4472C4'/>
        <ColorEntry IX='9' RGB='#70AD47'/>
        <ColorEntry IX='10' RGB='#FFC000'/>
        <ColorEntry IX='11' RGB='#FFE699'/>
        <ColorEntry IX='12' RGB='#BDD7EE'/>
        <ColorEntry IX='13' RGB='#F8CBAD'/>
    </Colors>
    <FaceNames>
        <FaceName ID='0' Name='Arial' UnicodeRanges='31367 -2147483648 8 0' CharSets='536870145 0' Panos='2 11 6 4 2 2 2 2 2 4' Flags='325'/>
        <FaceName ID='1' Name='Arial Unicode MS' UnicodeRanges='-1 -1 -1 -1' CharSets='1073741824 0' Panos='2 11 6 4 2 2 2 2 2 4' Flags='325'/>
    </FaceNames>
    <StyleSheets>
        <StyleSheet ID='0' Name='Normal' NameU='Normal'>
            <Cell N='EnableLineProps' V='1'/>
            <Cell N='EnableFillProps' V='1'/>
            <Cell N='EnableTextProps' V='1'/>
            <Cell N='HideForApply' V='0'/>
            <Line>
                <Cell N='LineWeight' V='0.01'/>
                <Cell N='LineColor' V='0'/>
                <Cell N='LinePattern' V='1'/>
            </Line>
            <Fill>
                <Cell N='FillForegnd' V='1'/>
                <Cell N='FillBkgnd' V='0'/>
                <Cell N='FillPattern' V='1'/>
            </Fill>
            <Char>
                <Cell N='Font' V='1'/>
                <Cell N='Color' V='0'/>
                <Cell N='Style' V='0'/>
                <Cell N='Case' V='0'/>
                <Cell N='Pos' V='0'/>
                <Cell N='FontScale' V='1'/>
                <Cell N='Size' V='0.1666666666666667'/>
            </Char>
        </StyleSheet>
        <StyleSheet ID='1' Name='Connector' NameU='Connector'>
            <Cell N='EnableLineProps' V='1'/>
            <Cell N='EnableFillProps' V='0'/>
            <Cell N='EnableTextProps' V='1'/>
            <Cell N='HideForApply' V='0'/>
            <Line>
                <Cell N='LineWeight' V='0.02'/>
                <Cell N='LineColor' V='0'/>
                <Cell N='LinePattern' V='1'/>
                <Cell N='EndArrow' V='2'/>
                <Cell N='BeginArrow' V='0'/>
            </Line>
            <Char>
                <Cell N='Font' V='0'/>
                <Cell N='Color' V='0'/>
                <Cell N='Style' V='0'/>
                <Cell N='Case' V='0'/>
                <Cell N='Pos' V='0'/>
                <Cell N='FontScale' V='1'/>
                <Cell N='Size' V='0.125'/>
            </Char>
        </StyleSheet>
    </StyleSheets>
    <Masters>
        <Master ID='0' Name='Rectangle' NameU='Rectangle' r:id='rIdMaster1'/>
        <Master ID='1' Name='Dynamic Connector' NameU='Dynamic Connector' r:id='rIdMaster2'/>
    </Masters>
    <Pages>
        <Page ID='0' Name='FeatureDiagram' NameU='FeatureDiagram' r:id='rIdPage1'/>
    </Pages>
</VisioDocument>";
    }

    /// <summary>
    /// 生成矩形Master模板
    /// </summary>
    private static string GenerateRectangleMasterXml()
    {
        return $@"<?xml version='1.0' encoding='UTF-8' standalone='yes'?>
<MasterContents xmlns='{NsVisio}' xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships' xml:space='preserve'>
    <MasterSheet>
        <Cell N='PageWidth' V='2' U='IN'/>
        <Cell N='PageHeight' V='1' U='IN'/>
        <Cell N='PageScale' V='1' U='IN'/>
        <Cell N='DrawingScale' V='1' U='IN'/>
    </MasterSheet>
    <Shapes>
        <Shape ID='5' Type='Shape' LineStyle='0' FillStyle='0' TextStyle='0'>
            <Cell N='PinX' V='1' U='IN'/>
            <Cell N='PinY' V='0.5' U='IN'/>
            <Cell N='Width' V='2' U='IN'/>
            <Cell N='Height' V='1' U='IN'/>
            <Cell N='LocPinX' V='1' U='IN'/>
            <Cell N='LocPinY' V='0.5' U='IN'/>
            <Section N='Geometry' IX='0'>
                <Row T='MoveTo' IX='1'>
                    <Cell N='X' V='0' F='Width*0'/>
                    <Cell N='Y' V='0' F='Height*0'/>
                </Row>
                <Row T='LineTo' IX='2'>
                    <Cell N='X' V='2' F='Width*1'/>
                    <Cell N='Y' V='0' F='Height*0'/>
                </Row>
                <Row T='LineTo' IX='3'>
                    <Cell N='X' V='2' F='Width*1'/>
                    <Cell N='Y' V='1' F='Height*1'/>
                </Row>
                <Row T='LineTo' IX='4'>
                    <Cell N='X' V='0' F='Width*0'/>
                    <Cell N='Y' V='1' F='Height*1'/>
                </Row>
                <Row T='LineTo' IX='5'>
                    <Cell N='X' V='0' F='Width*0'/>
                    <Cell N='Y' V='0' F='Height*0'/>
                </Row>
            </Section>
            <Cell N='FillForegnd' V='1'/>
            <Cell N='FillPattern' V='1'/>
        </Shape>
    </Shapes>
</MasterContents>";
    }

    /// <summary>
    /// 生成连接器Master模板
    /// </summary>
    private static string GenerateConnectorMasterXml()
    {
        return $@"<?xml version='1.0' encoding='UTF-8' standalone='yes'?>
<MasterContents xmlns='{NsVisio}' xmlns:r='http://schemas.openxmlformats.org/officeDocument/2006/relationships' xml:space='preserve'>
    <MasterSheet>
        <Cell N='PageWidth' V='1' U='IN'/>
        <Cell N='PageHeight' V='1' U='IN'/>
        <Cell N='PageScale' V='1' U='IN'/>
        <Cell N='DrawingScale' V='1' U='IN'/>
    </MasterSheet>
    <Shapes>
        <Shape ID='6' Type='Shape' LineStyle='1' FillStyle='0' TextStyle='0'>
            <Cell N='PinX' V='0.5' U='IN'/>
            <Cell N='PinY' V='0.5' U='IN'/>
            <Cell N='Width' V='1' U='IN'/>
            <Cell N='Height' V='1' U='IN'/>
            <Cell N='LocPinX' V='0.5' U='IN'/>
            <Cell N='LocPinY' V='0.5' U='IN'/>
            <Cell N='ObjType' V='2'/>
            <Section N='Geometry' IX='0'>
                <Row T='MoveTo' IX='1'>
                    <Cell N='X' V='0' F='Width*0'/>
                    <Cell N='Y' V='0' F='Height*0'/>
                </Row>
                <Row T='LineTo' IX='2'>
                    <Cell N='X' V='1' F='Width*1'/>
                    <Cell N='Y' V='1' F='Height*1'/>
                </Row>
            </Section>
        </Shape>
    </Shapes>
</MasterContents>";
    }

    private class FeatureInfo { public string Name { get; set; } = "Feature"; public List<ScenarioInfo> Scenarios { get; set; } = new List<ScenarioInfo>(); }
    private class ScenarioInfo { public string Name { get; set; } = ""; public List<string> Steps { get; set; } = new List<string>(); }
}
