using System.Text;

namespace EfCoreWebApp.Tests.Utils;

public static class VisioReportGenerator
{
    public static void GenerateDiagram(string vdxPath, string featureFilePath)
    {
        var featureContent = File.ReadAllText(featureFilePath);
        var feature = ParseFeatureFile(featureContent);
        var vdxContent = GenerateVdxContent(feature);
        File.WriteAllText(vdxPath, vdxContent);
    }

    private static string GenerateVdxContent(FeatureInfo feature)
    {
        var sb = new StringBuilder();
        var connects = new StringBuilder(); // Retain glue logic for "stickiness"

        // XML Header
        sb.AppendLine("<?xml version='1.0' encoding='utf-8' ?>");
        sb.AppendLine("<VisioDocument xmlns='http://schemas.microsoft.com/visio/2003/core' version='14.0' >");
        sb.AppendLine("<DocumentProperties>");
        sb.AppendLine("<Creator>EfCoreWebApp Tests</Creator>");
        sb.AppendLine("</DocumentProperties>");
        
        // Masters
        sb.AppendLine("<Masters>");
        // Master 0: Rectangle
        sb.AppendLine("<Master ID='0' Name='Rectangle' NameU='Rectangle' Prompt='Rectangle'>");
        sb.AppendLine("<IconUpdate>1</IconUpdate>");
        sb.AppendLine("<Shapes>");
        sb.AppendLine("<Shape ID='1' Type='Shape' MasterShape='0'>");
        sb.AppendLine("<XForm><PinX>1</PinX><PinY>1</PinY><Width>2</Width><Height>1</Height></XForm>");
        sb.AppendLine("<Fill><FillFore>#ffffff</FillFore></Fill>");
        sb.AppendLine("<Line><LineWeight>0.01</LineWeight><LineColor>#000000</LineColor><Rounding>0.1</Rounding></Line>");
        sb.AppendLine("</Shape>");
        sb.AppendLine("</Shapes>");
        sb.AppendLine("</Master>");
        // Master 1: Dynamic Connector (Template)
        sb.AppendLine("<Master ID='1' Name='Dynamic Connector' NameU='Dynamic connector' Prompt='Dynamic connector'>");
        sb.AppendLine("<IconUpdate>1</IconUpdate>");
        sb.AppendLine("<Shapes><Shape ID='1' Type='Shape' MasterShape='0'><ObjType>2</ObjType>");
        sb.AppendLine("<Line><LineWeight>0.01</LineWeight><LineColor>#000000</LineColor><EndArrow>1</EndArrow><LinePattern>1</LinePattern></Line>");
        sb.AppendLine("</Shape></Shapes>");
        sb.AppendLine("</Master>");
        sb.AppendLine("</Masters>");

        // Pages
        sb.AppendLine("<Pages>");
        sb.AppendLine("<Page ID='0' Name='FeatureDiagram' NameU='FeatureDiagram' ViewScale='0.5' ViewCenterX='5' ViewCenterY='5'>");
        sb.AppendLine("<Shapes>");

        int shapeId = 1;
        
        // --- 1. Draw Feature Node (Top Center) ---
        double rootX = 5.0;
        double rootY = 10.0;
        double rootW = 2.0;
        double rootH = 0.8;
        
        int rootShapeId = shapeId++;
        sb.Append(CreateShapeXml(rootShapeId, feature.Name, rootX, rootY, rootW, rootH, "4472C4")); // Blue

        // --- 2. Draw Scenarios (Row below Feature) ---
        double scenarioY = rootY - 2.0;
        int scenarioCount = feature.Scenarios.Count;
        double scenarioGap = 3.5;
        double startX = rootX - ((scenarioCount - 1) * scenarioGap) / 2.0;
        
        double scW = 2.5; 
        double scH = 0.8;

        for (int i = 0; i < scenarioCount; i++)
        {
            var scenario = feature.Scenarios[i];
            double scenarioX = startX + (i * scenarioGap);
            
            int scenarioShapeId = shapeId++;
            sb.Append(CreateShapeXml(scenarioShapeId, scenario.Name, scenarioX, scenarioY, scW, scH, "70AD47")); // Green
            
            // Connect Feature (Bottom) -> Scenario (Top)
            int connectorId = shapeId++;
            // Calculate coords: From (RootX, RootY - RootH/2) -> To (ScenX, ScenY + ScH/2)
            double startX_c = rootX;
            double startY_c = rootY - (rootH / 2.0);
            double endX_c = scenarioX;
            double endY_c = scenarioY + (scH / 2.0);
            
            sb.Append(CreateExplicitConnectorXml(connectorId, startX_c, startY_c, endX_c, endY_c));
            AddConnection(connects, connectorId, rootShapeId, scenarioShapeId);

            // --- 3. Draw Steps (Column below Scenario) ---
            double stepY = scenarioY - 1.5;
            double stepGap = 1.0;
            double stepW = 2.0; 
            double stepH = 0.6;
            
            int previousShapeId = scenarioShapeId;
            // Previous shape geometry tracking
            double prevX = scenarioX;
            double prevY = scenarioY;
            double prevH = scH;
            
            foreach (var step in scenario.Steps)
            {
                int stepShapeId = shapeId++;
                string stepText = step.Length > 20 ? step.Substring(0, 18) + ".." : step;
                string color = "FFC000"; // Orange
                if (step.StartsWith("前提") || step.StartsWith("Given")) color = "FFE699"; 
                if (step.StartsWith("もし") || step.StartsWith("When")) color = "BDD7EE";
                if (step.StartsWith("ならば") || step.StartsWith("Then")) color = "F8CBAD";
                
                sb.Append(CreateShapeXml(stepShapeId, stepText, scenarioX, stepY, stepW, stepH, color));
                
                // Connect Previous (Bottom) -> Step (Top)
                connectorId = shapeId++;
                
                double sXc = prevX;
                double sYc = prevY - (prevH / 2.0);
                double eXc = scenarioX;
                double eYc = stepY + (stepH / 2.0);
                
                sb.Append(CreateExplicitConnectorXml(connectorId, sXc, sYc, eXc, eYc));
                AddConnection(connects, connectorId, previousShapeId, stepShapeId);
                
                previousShapeId = stepShapeId;
                prevX = scenarioX;
                prevY = stepY;
                prevH = stepH;
                
                stepY -= stepGap;
            }
        }

        sb.AppendLine("</Shapes>");
        
        sb.AppendLine("<Connects>");
        sb.Append(connects.ToString());
        sb.AppendLine("</Connects>");
        
        sb.AppendLine("</Page>");
        sb.AppendLine("</Pages>");
        sb.AppendLine("</VisioDocument>");

        return sb.ToString();
    }

    private static void AddConnection(StringBuilder sb, int connectorId, int fromId, int toId)
    {
        // Glue BeginX to FromShape PinX
        sb.AppendLine($"<Connect FromSheet='{connectorId}' FromCell='BeginX' ToSheet='{fromId}' ToCell='PinX'/>");
        // Glue EndX to ToShape PinX
        sb.AppendLine($"<Connect FromSheet='{connectorId}' FromCell='EndX' ToSheet='{toId}' ToCell='PinX'/>");
    }

    private static string CreateShapeXml(int id, string text, double x, double y, double w, double h, string hexColor)
    {
        // Simple shape definition reusing Master 0 (Rectangle)
        // Clean text for XML
        text = text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        
        return $@"
        <Shape ID='{id}' Type='Shape' Master='0'>
            <XForm>
                <PinX>{x}</PinX>
                <PinY>{y}</PinY>
                <Width>{w}</Width>
                <Height>{h}</Height>
            </XForm>
            <Fill>
                <FillFore>#{hexColor}</FillFore>
            </Fill>
            <Text>{text}</Text>
        </Shape>";
    }

    private static string CreateExplicitConnectorXml(int id, double x1, double y1, double x2, double y2)
    {
        // Calculate Bounding Box
        double w = Math.Abs(x2 - x1);
        double h = Math.Abs(y2 - y1);
        
        // Ensure non-zero size for Visio 2D shape (avoid div by zero or invisible shape)
        if (w < 0.0001) w = 0.0001;
        if (h < 0.0001) h = 0.0001;

        double pinX = (x1 + x2) / 2.0;
        double pinY = (y1 + y2) / 2.0;

        // Calculate Local Coordinates (Relative to Bottom-Left of the bounding box)
        double left = pinX - (w / 2.0);
        double bottom = pinY - (h / 2.0);

        double lx1 = x1 - left;
        double ly1 = y1 - bottom;
        double lx2 = x2 - left;
        double ly2 = y2 - bottom;

        // Explicit 2D Shape with Line Geometry
        // No Main Master (Master='0' is Rectangle, but we overwrite Geom so it's fine, or use no master)
        // Using Type='Shape' (2D)
        return $@"
        <Shape ID='{id}' Type='Shape'>
             <XForm>
                <PinX>{pinX}</PinX><PinY>{pinY}</PinY>
                <Width>{w}</Width><Height>{h}</Height>
                <LocPinX>{w/2.0}</LocPinX><LocPinY>{h/2.0}</LocPinY>
            </XForm>
             <Geom IX='0'>
                <NoFill>1</NoFill>
                <NoLine>0</NoLine>
                <MoveTo IX='1'><X>{lx1}</X><Y>{ly1}</Y></MoveTo>
                <LineTo IX='2'><X>{lx2}</X><Y>{ly2}</Y></LineTo>
            </Geom>
            <Line><LineWeight>0.01</LineWeight><LineColor>#000000</LineColor><EndArrow>1</EndArrow><LinePattern>1</LinePattern></Line>
        </Shape>";
    }
    
    // --- Parser (Duplicate from MermaidDiagramGenerator for independence) ---
    private static FeatureInfo ParseFeatureFile(string featureContent)
    {
        var lines = featureContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var feature = new FeatureInfo();
        var currentScenario = new ScenarioInfo();
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            if (trimmedLine.StartsWith("機能:") || trimmedLine.StartsWith("Feature:"))
            {
                feature.Name = trimmedLine.Contains(":") 
                    ? trimmedLine.Substring(trimmedLine.IndexOf(':') + 1).Trim()
                    : "商品管理機能";
            }
            else if (trimmedLine.StartsWith("シナリオ:") || trimmedLine.StartsWith("Scenario:"))
            {
                if (!string.IsNullOrEmpty(currentScenario.Name)) feature.Scenarios.Add(currentScenario);
                currentScenario = new ScenarioInfo
                {
                    Name = trimmedLine.Contains(":") ? trimmedLine.Substring(trimmedLine.IndexOf(':') + 1).Trim() : trimmedLine
                };
            }
            else if (trimmedLine.StartsWith("前提") || trimmedLine.StartsWith("もし") || 
                     trimmedLine.StartsWith("ならば") || trimmedLine.StartsWith("Given") || 
                     trimmedLine.StartsWith("When") || trimmedLine.StartsWith("Then") || trimmedLine.StartsWith("And"))
            {
                currentScenario.Steps.Add(trimmedLine);
            }
        }
        if (!string.IsNullOrEmpty(currentScenario.Name)) feature.Scenarios.Add(currentScenario);
        return feature;
    }

    private class FeatureInfo
    {
        public string Name { get; set; } = "Feature";
        public List<ScenarioInfo> Scenarios { get; set; } = new List<ScenarioInfo>();
    }
    
    private class ScenarioInfo
    {
        public string Name { get; set; } = "";
        public List<string> Steps { get; set; } = new List<string>();
    }
}
