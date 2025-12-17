namespace EfCoreWebApp.Tests.Utils;

public static class MermaidDiagramGenerator
{
    public static void GenerateDiagram(string mermaidPath, string featureFilePath)
    {
        var featureContent = File.ReadAllText(featureFilePath);
        var feature = ParseFeatureFile(featureContent);
        
        var mermaidCode = GenerateMermaidCode(feature);
        File.WriteAllText(mermaidPath, mermaidCode);
    }
    
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
                if (!string.IsNullOrEmpty(currentScenario.Name))
                {
                    feature.Scenarios.Add(currentScenario);
                }
                currentScenario = new ScenarioInfo
                {
                    Name = trimmedLine.Contains(":")
                        ? trimmedLine.Substring(trimmedLine.IndexOf(':') + 1).Trim()
                        : trimmedLine
                };
            }
            else if (trimmedLine.StartsWith("前提") || trimmedLine.StartsWith("もし") || 
                     trimmedLine.StartsWith("ならば") || trimmedLine.StartsWith("かつ") ||
                     trimmedLine.StartsWith("Given") || trimmedLine.StartsWith("When") ||
                     trimmedLine.StartsWith("Then") || trimmedLine.StartsWith("And"))
            {
                currentScenario.Steps.Add(trimmedLine);
            }
        }
        
        if (!string.IsNullOrEmpty(currentScenario.Name))
        {
            feature.Scenarios.Add(currentScenario);
        }
        
        return feature;
    }
    
    private static string GenerateMermaidCode(FeatureInfo feature)
    {
        var sb = new System.Text.StringBuilder();
        
        sb.AppendLine("```mermaid");
        sb.AppendLine("graph TD");
        sb.AppendLine($"    Feature[\"{EscapeMermaid(feature.Name)}\"]");
        sb.AppendLine("    ");
        sb.AppendLine("    %% シナリオ");
        
        for (int i = 0; i < feature.Scenarios.Count; i++)
        {
            var scenario = feature.Scenarios[i];
            var scenarioId = $"Scenario{i + 1}";
            
            sb.AppendLine($"    {scenarioId}[\"{EscapeMermaid(scenario.Name)}\"]");
            sb.AppendLine($"    Feature --> {scenarioId}");
            
            // ステップ
            if (scenario.Steps.Count > 0)
            {
                sb.AppendLine($"    ");
                sb.AppendLine($"    %% {scenario.Name} のステップ");
                
                for (int j = 0; j < scenario.Steps.Count; j++)
                {
                    var step = scenario.Steps[j];
                    var stepId = $"Step{i + 1}_{j + 1}";
                    
                    // ステップのタイプによって形を変える
                    var stepType = GetStepType(step);
                    var stepShape = GetStepShape(stepType);
                    var stepText = TruncateStep(step, 40);
                    
                    sb.AppendLine($"    {stepId}{stepShape}\"{EscapeMermaid(stepText)}\"{stepShape.Replace("[", "]")}");
                    sb.AppendLine($"    {scenarioId} --> {stepId}");
                }
                sb.AppendLine();
            }
        }
        
        // スタイリング
        sb.AppendLine("    %% スタイル定義");
        sb.AppendLine("    classDef featureClass fill:#4472C4,stroke:#2E5090,color:#fff,stroke-width:3px");
        sb.AppendLine("    classDef scenarioClass fill:#70AD47,stroke:#507E32,color:#fff,stroke-width:2px");
        sb.AppendLine("    classDef stepClass fill:#FFC000,stroke:#C09000,color:#000,stroke-width:1px");
        sb.AppendLine("    ");
        sb.AppendLine("    class Feature featureClass");
        
        for (int i = 0; i < feature.Scenarios.Count; i++)
        {
            sb.Append($"    class Scenario{i + 1} scenarioClass");
            sb.AppendLine();
        }
        
        for (int i = 0; i < feature.Scenarios.Count; i++)
        {
            var scenario = feature.Scenarios[i];
            for (int j = 0; j < scenario.Steps.Count; j++)
            {
                sb.Append($"    class Step{i + 1}_{j + 1} stepClass");
                sb.AppendLine();
            }
        }
        
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## 凡例");
        sb.AppendLine();
        sb.AppendLine("- **青色 (四角)**: 機能 (Feature)");
        sb.AppendLine("- **緑色 (四角)**: シナリオ (Scenario)");
        sb.AppendLine("- **オレンジ色 (丸角四角/ひし形/六角形)**: ステップ (Steps)");
        sb.AppendLine("  - 丸角四角: Given/前提 (事前条件)");
        sb.AppendLine("  - ひし形: When/もし (アクション)");
        sb.AppendLine("  - 六角形: Then/ならば (検証)");
        
        return sb.ToString();
    }
    
    private static string EscapeMermaid(string text)
    {
        // Mermaidで特殊文字をエスケープ
        return text
            .Replace("\"", "'")
            .Replace("[", "(")
            .Replace("]", ")")
            .Replace("{", "(")
            .Replace("}", ")");
    }
    
    private static string GetStepType(string step)
    {
        if (step.StartsWith("前提") || step.StartsWith("Given"))
            return "given";
        if (step.StartsWith("もし") || step.StartsWith("When"))
            return "when";
        if (step.StartsWith("ならば") || step.StartsWith("Then"))
            return "then";
        return "and";
    }
    
    private static string GetStepShape(string stepType)
    {
        return stepType switch
        {
            "given" => "[",      // 丸角四角
            "when" => "{",       // ひし形
            "then" => "{{",      // 六角形
            _ => "["             // デフォルト: 丸角四角
        };
    }
    
    private static string TruncateStep(string step, int maxLength)
    {
        if (step.Length <= maxLength)
            return step;
        
        return step.Substring(0, maxLength - 3) + "...";
    }
    
    private class FeatureInfo
    {
        public string Name { get; set; } = "商品管理機能";
        public List<ScenarioInfo> Scenarios { get; set; } = new List<ScenarioInfo>();
    }
    
    private class ScenarioInfo
    {
        public string Name { get; set; } = "";
        public List<string> Steps { get; set; } = new List<string>();
    }
}
