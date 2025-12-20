using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace EfCoreWebApp.Tests.Utils;

public static class DrawIoReportGenerator
{
    public static void GenerateDiagram(string outputPath, string featureFilePath)
    {
        var featureContent = File.ReadAllText(featureFilePath);
        var feature = ParseFeatureFile(featureContent);

        // Calculate layout
        var mxFile = GenerateMxFileXml(feature);
        File.WriteAllText(outputPath, mxFile);
    }

    private static string GenerateMxFileXml(FeatureInfo feature)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<mxfile host=\"app.diagrams.net\" agent=\"Mozilla/5.0\" version=\"24.0.0\">");
        sb.AppendLine($"  <diagram name=\"Page-1\" id=\"{Guid.NewGuid()}\">");
        sb.AppendLine("    <mxGraphModel dx=\"1200\" dy=\"1200\" grid=\"1\" gridSize=\"10\" guides=\"1\" tooltips=\"1\" connect=\"1\" arrows=\"1\" fold=\"1\" page=\"1\" pageScale=\"1\" pageWidth=\"827\" pageHeight=\"1169\" math=\"0\" shadow=\"0\">");
        sb.AppendLine("      <root>");
        sb.AppendLine("        <mxCell id=\"0\" />");
        sb.AppendLine("        <mxCell id=\"1\" parent=\"0\" />");

        int idCounter = 2; // Start IDs from 2

        // --- 1. Root Feature Node (Top Center) ---
        // Center of page roughly X=400
        int rootX = 350;
        int rootY = 20;
        int rootW = 140;
        int rootH = 60;
        
        string rootId = $"cell-{idCounter++}";
        // Style: White box
        string rootStyle = "whiteSpace=wrap;strokeWidth=2;fillColor=#dae8fc;strokeColor=#6c8ebf;"; 
        sb.AppendLine(CreateVertexXml(rootId, feature.Name, rootX, rootY, rootW, rootH, rootStyle));

        // --- 2. Scenarios (Row) ---
        int scenarioY = 124;
        int scenarioGap = 200;
        int scenarioCount = feature.Scenarios.Count;
        int startX = 400 - ((scenarioCount * 200) / 2); // approximate centering

        for (int i = 0; i < scenarioCount; i++)
        {
            var scenario = feature.Scenarios[i];
            int currentX = startX + (i * scenarioGap);
            
            string scenarioId = $"cell-{idCounter++}";
            string scenarioStyle = "whiteSpace=wrap;strokeWidth=2;fillColor=#d5e8d4;strokeColor=#82b366;";
            sb.AppendLine(CreateVertexXml(scenarioId, scenario.Name, currentX, scenarioY, 180, 60, scenarioStyle));

            // Edge: Feature -> Scenario
            sb.AppendLine(CreateEdgeXml($"edge-{idCounter++}", rootId, scenarioId));

            // --- 3. Steps (Column below Scenario) ---
            int stepY = 220;
            string previousId = scenarioId;

            foreach (var step in scenario.Steps)
            {
                string stepId = $"cell-{idCounter++}";
                string stepStyle = "whiteSpace=wrap;strokeWidth=2;rounded=1;arcSize=20;";
                
                // Determine Shape/Color based on keyword
                if (step.StartsWith("前提") || step.StartsWith("Given")) {
                    stepStyle = "rounded=1;arcSize=20;strokeWidth=2;fillColor=#fff2cc;strokeColor=#d6b656;"; // Rounded Box
                }
                else if (step.StartsWith("もし") || step.StartsWith("When")) {
                    stepStyle = "rhombus;strokeWidth=2;whiteSpace=wrap;fillColor=#dae8fc;strokeColor=#6c8ebf;"; // Diamond
                }
                else if (step.StartsWith("ならば") || step.StartsWith("Then")) {
                    stepStyle = "shape=hexagon;perimeter=hexagonPerimeter2;fixedSize=1;strokeWidth=2;whiteSpace=wrap;fillColor=#ffe6cc;strokeColor=#d79b00;"; // Hexagon
                }

                // Adjust size for Diamond/Hexagon to be bigger if needed?
                int w = 200; 
                int h = 60;
                if (stepStyle.Contains("rhombus")) { h = 100; } // Diamond needs more height

                sb.AppendLine(CreateVertexXml(stepId, step, currentX, stepY, w, h, stepStyle));
                
                // Edge: Previous -> Step
                sb.AppendLine(CreateEdgeXml($"edge-{idCounter++}", previousId, stepId));

                previousId = stepId;
                stepY += (h + 40);
            }
        }

        sb.AppendLine("      </root>");
        sb.AppendLine("    </mxGraphModel>");
        sb.AppendLine("  </diagram>");
        sb.AppendLine("</mxfile>");

        return sb.ToString();
    }

    private static string CreateVertexXml(string id, string value, int x, int y, int w, int h, string style)
    {
        // XML Escape value
        value = value.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;").Replace(">", "&gt;");
        
        return $@"        <mxCell id=""{id}"" parent=""1"" style=""{style}"" value=""{value}"" vertex=""1"">
          <mxGeometry x=""{x}"" y=""{y}"" width=""{w}"" height=""{h}"" as=""geometry"" />
        </mxCell>";
    }

    private static string CreateEdgeXml(string id, string sourceId, string targetId)
    {
        return $@"        <mxCell id=""{id}"" parent=""1"" source=""{sourceId}"" target=""{targetId}"" edge=""1"" style=""curved=1;startArrow=none;endArrow=block;rounded=0;"">
          <mxGeometry relative=""1"" as=""geometry"">
             <Array as=""points"" />
          </mxGeometry>
        </mxCell>";
    }

    // Reuse parsing logic
    private static FeatureInfo ParseFeatureFile(string featureContent)
    {
         var lines = featureContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var feature = new FeatureInfo();
        var currentScenario = new ScenarioInfo();
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
             if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#") || trimmedLine.StartsWith("@")) continue;

            if (trimmedLine.StartsWith("機能:") || trimmedLine.StartsWith("Feature:"))
                feature.Name = trimmedLine.Contains(":") ? trimmedLine.Substring(trimmedLine.IndexOf(':') + 1).Trim() : "Feature";
            else if (trimmedLine.StartsWith("シナリオ:") || trimmedLine.StartsWith("Scenario:")) {
                if (!string.IsNullOrEmpty(currentScenario.Name)) feature.Scenarios.Add(currentScenario);
                currentScenario = new ScenarioInfo { Name = trimmedLine.Contains(":") ? trimmedLine.Substring(trimmedLine.IndexOf(':') + 1).Trim() : trimmedLine };
            }
            else if (trimmedLine.StartsWith("前提") || trimmedLine.StartsWith("Given") || 
                     trimmedLine.StartsWith("もし") || trimmedLine.StartsWith("When") || 
                     trimmedLine.StartsWith("ならば") || trimmedLine.StartsWith("Then") ||
                     trimmedLine.StartsWith("かつ") || trimmedLine.StartsWith("And"))
            {
                currentScenario.Steps.Add(trimmedLine);
            }
        }
        if (!string.IsNullOrEmpty(currentScenario.Name)) feature.Scenarios.Add(currentScenario);
        return feature;
    }

    private class FeatureInfo { public string Name { get; set; } = "Feature"; public List<ScenarioInfo> Scenarios { get; set; } = new List<ScenarioInfo>(); }
    private class ScenarioInfo { public string Name { get; set; } = ""; public List<string> Steps { get; set; } = new List<string>(); }
}
