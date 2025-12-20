using System.Diagnostics;

namespace EfCoreWebApp.Tests.Utils;

public static class MermaidDiagramGenerator
{
    public static async Task GenerateDiagramAsync(string mermaidPath, string featureFilePath)
    {
        var featureContent = File.ReadAllText(featureFilePath);
        var feature = ParseFeatureFile(featureContent);
        
        var mermaidCode = GenerateMermaidCode(feature);
        File.WriteAllText(mermaidPath, mermaidCode);
        
        // Generate HTML (User requested)
        var htmlPath = Path.ChangeExtension(mermaidPath, ".html");
        await GenerateHtmlAsync(mermaidCode, htmlPath);
        
        // Convert to SVG and JPG (Visio and Image) using Playwright
        // We use the generated HTML file as the source
        var svgPath = Path.ChangeExtension(mermaidPath, ".svg");
        var jpgPath = Path.ChangeExtension(mermaidPath, ".jpg");
        await ConvertToArtifactsAsync(htmlPath, svgPath, jpgPath);
    }
    
    private static async Task GenerateHtmlAsync(string mermaidCode, string htmlPath)
    {
        var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""UTF-8"">
    <title>Test Hierarchy Diagram</title>
    <style>
        body {{ background-color: white; }} /* Ensure white background for JPG */
    </style>
</head>
<body>
    <div class=""mermaid"">
{mermaidCode}
    </div>
    <script type=""module"">
        import mermaid from 'https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.esm.min.mjs';
        mermaid.initialize({{ startOnLoad: true }});
    </script>
</body>
</html>";
        await File.WriteAllTextAsync(htmlPath, htmlContent);
    }
    
    private static async Task ConvertToArtifactsAsync(string htmlPath, string svgPath, string jpgPath)
    {
        try 
        {
            using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new Microsoft.Playwright.BrowserTypeLaunchOptions { Headless = true });
            
            // Set a large viewport to ensure everything renders
            var page = await browser.NewPageAsync(new Microsoft.Playwright.BrowserNewPageOptions
            {
                ViewportSize = new Microsoft.Playwright.ViewportSize { Width = 1920, Height = 1080 } 
            });
            
            // Open the local HTML file
            await page.GotoAsync($"file://{htmlPath}");
            
            // Wait for mermaid to generate the SVG and ensure it has dimensions
            await page.WaitForSelectorAsync(".mermaid svg");
            
            // Small delay to ensure layout is stable (Mermaid sometimes reflows)
            await page.WaitForTimeoutAsync(1000); 

            // 1. Extract the SVG content
            var svgContent = await page.Locator(".mermaid").InnerHTMLAsync();
            await File.WriteAllTextAsync(svgPath, svgContent);
            
            // 2. Capture JPG Screenshot
            // We select the mermaid div to take a crop of just the diagram
            // Ensure background is white for JPG
            await page.Locator(".mermaid").ScreenshotAsync(new Microsoft.Playwright.LocatorScreenshotOptions
            {
                Path = jpgPath,
                Type = Microsoft.Playwright.ScreenshotType.Jpeg,
                Quality = 100,
                OmitBackground = false 
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Diagram conversion failed: {ex.Message}");
        }
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
        
        // Remove backticks to return pure Mermaid syntax
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
                    var stepShapeStart = GetStepShapeStart(stepType);
                    var stepShapeEnd = GetStepShapeEnd(stepShapeStart);
                    var stepText = TruncateStep(step, 40);
                    
                    sb.AppendLine($"    {stepId}{stepShapeStart}\"{EscapeMermaid(stepText)}\"{stepShapeEnd}");
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
        
        // Convert Legend to Comments (to avoid syntax errors)
        sb.AppendLine();
        sb.AppendLine("    %% 凡例");
        sb.AppendLine("    %% 青色 (四角): 機能 (Feature)");
        sb.AppendLine("    %% 緑色 (四角): シナリオ (Scenario)");
        sb.AppendLine("    %% オレンジ色: ステップ (Steps)");
        
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
    
    private static string GetStepShapeStart(string stepType)
    {
        return stepType switch
        {
            "given" => "(",      // 丸角
            "when" => "{",       // ひし形
            "then" => "{{",      // 六角形
            _ => "["             // 四角
        };
    }

    private static string GetStepShapeEnd(string startShape)
    {
        return startShape switch
        {
            "(" => ")",
            "{" => "}",
            "{{" => "}}",
            "[" => "]",
            _ => "]"
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
