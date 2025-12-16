using System.Text;
using System.Net.Http;
using SkiaSharp;

namespace EfCoreWebApp.Tests.Utils;

public static class ExcelReport
{
    public static void GenerateFromFeature(string featureFilePath, string outputPath, string testResult = "PASS", string? screenshotPath = null)
    {
        var featureContent = File.ReadAllText(featureFilePath);
        var lines = featureContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        var feature = "";
        var scenarios = new List<ScenarioInfo>();
        var currentScenario = new ScenarioInfo();
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            if (trimmedLine.StartsWith("機能:"))
            {
                feature = trimmedLine.Substring(3).Trim();
            }
            else if (trimmedLine.StartsWith("シナリオ:"))
            {
                if (!string.IsNullOrEmpty(currentScenario.Name))
                {
                    scenarios.Add(currentScenario);
                }
                currentScenario = new ScenarioInfo
                {
                    Name = trimmedLine.Substring(4).Trim()
                };
            }
            else if (trimmedLine.StartsWith("前提") || trimmedLine.StartsWith("もし") || 
                     trimmedLine.StartsWith("ならば") || trimmedLine.StartsWith("かつ"))
            {
                currentScenario.Steps.Add(trimmedLine);
            }
        }
        
        if (!string.IsNullOrEmpty(currentScenario.Name))
        {
            scenarios.Add(currentScenario);
        }
        
        GenerateExcelReport(outputPath, feature, scenarios, testResult, screenshotPath);
    }
    
    public static async Task<string> TakeScreenshotAsync(string url, string outputPath)
    {
        try
        {
            using var client = new HttpClient();
            var response = await client.GetAsync(url);
            var html = await response.Content.ReadAsStringAsync();
            
            // 簡単なスクリーンショット生成（テキストベース）
            var screenshotPath = Path.ChangeExtension(outputPath, ".png");
            CreateTextScreenshot(html, screenshotPath, url);
            return screenshotPath;
        }
        catch
        {
            return "";
        }
    }
    
    private static void CreateTextScreenshot(string html, string screenshotPath, string url)
    {
        var width = 800;
        var height = 600;
        
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        
        // 背景を白に
        canvas.Clear(SKColors.White);
        
        var paint = new SKPaint
        {
            Color = SKColors.Black,
            TextSize = 14,
            IsAntialias = true,
            Typeface = SKTypeface.Default
        };
        
        var titlePaint = new SKPaint
        {
            Color = SKColors.DarkBlue,
            TextSize = 18,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };
        
        // タイトル描画
        canvas.DrawText($"Screenshot: {url}", 20, 30, titlePaint);
        canvas.DrawText($"Captured: {DateTime.Now:yyyy/MM/dd HH:mm:ss}", 20, 55, paint);
        
        // HTMLの一部を描画
        var lines = html.Split('\n').Take(20).ToArray();
        float y = 90;
        foreach (var line in lines)
        {
            var displayLine = line.Length > 80 ? line.Substring(0, 80) + "..." : line;
            canvas.DrawText(displayLine, 20, y, paint);
            y += 20;
            if (y > height - 50) break;
        }
        
        // 枠線描画
        var borderPaint = new SKPaint
        {
            Color = SKColors.Gray,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2
        };
        canvas.DrawRect(10, 10, width - 20, height - 20, borderPaint);
        
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(screenshotPath);
        data.SaveTo(stream);
    }
    
    private static void GenerateExcelReport(string outputPath, string feature, List<ScenarioInfo> scenarios, string testResult, string? screenshotPath = null)
    {
        // CSV形式でExcel互換ファイル生成
        var csv = new StringBuilder();
        
        // ヘッダー行
        csv.AppendLine("テスト報告書");
        csv.AppendLine($"機能,{feature}");
        csv.AppendLine($"実行日時,{DateTime.Now:yyyy/MM/dd HH:mm:ss}");
        csv.AppendLine($"総合結果,{testResult}");
        if (!string.IsNullOrEmpty(screenshotPath))
        {
            csv.AppendLine($"スクリーンショット,{Path.GetFileName(screenshotPath)}");
        }
        csv.AppendLine();
        
        // テスト詳細ヘッダー
        csv.AppendLine("シナリオ,ステップ,結果,備考,スクリーンショット");
        
        foreach (var scenario in scenarios)
        {
            for (int i = 0; i < scenario.Steps.Count; i++)
            {
                var scenarioName = i == 0 ? scenario.Name : "";
                var stepResult = testResult == "PASS" ? "OK" : "NG";
                var screenshot = (i == 0 && !string.IsNullOrEmpty(screenshotPath)) ? Path.GetFileName(screenshotPath) : "";
                csv.AppendLine($"{scenarioName},{scenario.Steps[i]},{stepResult},,{screenshot}");
            }
            csv.AppendLine(); // 空行でシナリオを区切る
        }
        
        // 統計情報
        csv.AppendLine("統計情報");
        csv.AppendLine($"総シナリオ数,{scenarios.Count}");
        csv.AppendLine($"総ステップ数,{scenarios.Sum(s => s.Steps.Count)}");
        csv.AppendLine($"成功率,{(testResult == "PASS" ? "100%" : "0%")}");
        
        // 添付ファイル情報
        if (!string.IsNullOrEmpty(screenshotPath))
        {
            csv.AppendLine();
            csv.AppendLine("添付ファイル");
            csv.AppendLine($"スクリーンショット,{Path.GetFileName(screenshotPath)}");
        }
        
        File.WriteAllText(outputPath, csv.ToString(), Encoding.UTF8);
    }
    
    private class ScenarioInfo
    {
        public string Name { get; set; } = "";
        public List<string> Steps { get; set; } = new List<string>();
    }
}