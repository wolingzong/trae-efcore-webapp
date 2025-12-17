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

        // XML Header
        sb.AppendLine("<?xml version='1.0' encoding='utf-8' ?>");
        sb.AppendLine("<VisioDocument xmlns='http://schemas.microsoft.com/visio/2003/core' version='14.0' >");
        sb.AppendLine("<DocumentProperties>");
        sb.AppendLine("<Creator>EfCoreWebApp Tests</Creator>");
        sb.AppendLine("</DocumentProperties>");
        
        // Masters (Templates)
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
        // Master 1: Dynamic Connector
        sb.AppendLine("<Master ID='1' Name='Dynamic Connector' NameU='Dynamic connector' Prompt='Dynamic connector'>");
        sb.AppendLine("<Shapes><Shape ID='1' Type='Shape' MasterShape='0'><ObjType>2</ObjType></Shape></Shapes>");
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
        double width = 2.0;
        double height = 0.8;
        
        int rootShapeId = shapeId++;
        sb.Append(CreateShapeXml(rootShapeId, feature.Name, rootX, rootY, width, height, "4472C4")); // Blue

        // --- 2. Draw Scenarios (Row below Feature) ---
        double scenarioY = rootY - 2.0;
        int scenarioCount = feature.Scenarios.Count;
        double scenarioGap = 3.5;
        double startX = rootX - ((scenarioCount - 1) * scenarioGap) / 2.0;

        for (int i = 0; i < scenarioCount; i++)
        {
            var scenario = feature.Scenarios[i];
            double scenarioX = startX + (i * scenarioGap);
            
            int scenarioShapeId = shapeId++;
            sb.Append(CreateShapeXml(scenarioShapeId, scenario.Name, scenarioX, scenarioY, 2.5, 0.8, "70AD47")); // Green
            
            // Connect Feature -> Scenario
            sb.Append(CreateConnectorXml(shapeId++, rootShapeId, scenarioShapeId));

            // --- 3. Draw Steps (Column below Scenario) ---
            double stepY = scenarioY - 1.5;
            double stepGap = 1.0;
            
            int previousShapeId = scenarioShapeId;
            
            foreach (var step in scenario.Steps)
            {
                int stepShapeId = shapeId++;
                // Truncate step text
                string stepText = step.Length > 20 ? step.Substring(0, 18) + ".." : step;
                
                // Determine shape color based on type
                string color = "FFC000"; // Orange
                if (step.StartsWith("前提") || step.StartsWith("Given")) color = "FFE699"; 
                if (step.StartsWith("もし") || step.StartsWith("When")) color = "BDD7EE";
                if (step.StartsWith("ならば") || step.StartsWith("Then")) color = "F8CBAD";
                
                sb.Append(CreateShapeXml(stepShapeId, stepText, scenarioX, stepY, 2.0, 0.6, color));
                
                // Connect Previous -> Step
                sb.Append(CreateConnectorXml(shapeId++, previousShapeId, stepShapeId));
                
                previousShapeId = stepShapeId;
                stepY -= stepGap;
            }
        }

        sb.AppendLine("</Shapes>");
        sb.AppendLine("</Page>");
        sb.AppendLine("</Pages>");
        sb.AppendLine("</VisioDocument>");

        return sb.ToString();
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

    private static string CreateConnectorXml(int id, int fromId, int toId)
    {
        // Simple Dynamic Connector logic
        // This relies on basic XForm, normally VDX connects via BeginX/BeginY but simple overlapping lines often work or need cell references
        // NOTE: Dynamic connection in strict VDX requires <Cell N='BeginX' .../> referencing the shape.
        // For simplicity, we just draw a line if we have coordinates, BUT since we want dynamic, we try to use the Connector Master.
        // A robust connector needs more glue info. 
        // Strategy: Use a 1D shape (ObjType=2) and roughly position it. Visio might auto-route.
        // Actually, precise connection is complex. 
        // Let's rely on Visio's behavior for Master=1 (Dynamic Connector). 
        // We set BeginX/EndX roughly.
        // Actually for VDX, correct way is <XForm1D> and <Cell> with Formula referencing correct IDs.
        
        // Simplified: Just listing it as a connector might not auto-connect visually without formulas.
        // Let's dump a basic connection formula.
        
        return $@"
        <Shape ID='{id}' Type='Shape' Master='1'>
            <XForm>
                <PinX>0</PinX><PinY>0</PinY><Width>1</Width><Height>1</Height>
            </XForm>
            <XForm1D>
                <BeginX F='PAR(PNT(Sheet.{fromId}!PinX,Sheet.{fromId}!PinY))'>0</BeginX>
                <BeginY F='PAR(PNT(Sheet.{fromId}!PinX,Sheet.{fromId}!PinY))'>0</BeginY>
                <EndX F='PAR(PNT(Sheet.{toId}!PinX,Sheet.{toId}!PinY))'>0</EndX>
                <EndY F='PAR(PNT(Sheet.{toId}!PinX,Sheet.{toId}!PinY))'>0</EndY>
            </XForm1D>
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
