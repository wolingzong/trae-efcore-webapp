using PuppeteerSharp;

namespace EfCoreWebApp.Tests.Utils;

public static class BrowserScreenshot
{
    public static async Task<string> TakeScreenshotAsync(string url, string outputPath)
    {
        try
        {
            // Puppeteer のブラウザをダウンロード（初回のみ）
            await new BrowserFetcher().DownloadAsync();
            
            // ブラウザを起動
            using var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Headless = true,
                Args = new[] { "--no-sandbox", "--disable-setuid-sandbox" }
            });
            
            // 新しいページを作成
            using var page = await browser.NewPageAsync();
            
            // ビューポートサイズを設定
            await page.SetViewportAsync(new ViewPortOptions
            {
                Width = 1200,
                Height = 800
            });
            
            // ページに移動
            await page.GoToAsync(url, new NavigationOptions
            {
                WaitUntil = new[] { WaitUntilNavigation.Networkidle0 },
                Timeout = 30000
            });
            
            // 少し待機してページが完全に読み込まれるのを待つ
            await Task.Delay(2000);
            
            // スクリーンショットを撮影
            await page.ScreenshotAsync(outputPath, new ScreenshotOptions
            {
                FullPage = true,
                Type = ScreenshotType.Png
            });
            
            return outputPath;
        }
        catch (Exception ex)
        {
            // エラーが発生した場合は、フォールバック用の画像を生成
            Console.WriteLine($"Screenshot error: {ex.Message}");
            return await CreateFallbackScreenshot(url, outputPath, ex.Message);
        }
    }
    
    private static async Task<string> CreateFallbackScreenshot(string url, string outputPath, string errorMessage)
    {
        try
        {
            // HTTP クライアントでページ内容を取得
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);
            var response = await client.GetAsync(url);
            var html = await response.Content.ReadAsStringAsync();
            
            // SkiaSharp を使用してフォールバック画像を生成
            var width = 1200;
            var height = 800;
            
            using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(width, height));
            var canvas = surface.Canvas;
            
            // 背景を白に
            canvas.Clear(SkiaSharp.SKColors.White);
            
            var paint = new SkiaSharp.SKPaint
            {
                Color = SkiaSharp.SKColors.Black,
                TextSize = 16,
                IsAntialias = true,
                Typeface = SkiaSharp.SKTypeface.Default
            };
            
            var titlePaint = new SkiaSharp.SKPaint
            {
                Color = SkiaSharp.SKColors.DarkBlue,
                TextSize = 24,
                IsAntialias = true,
                Typeface = SkiaSharp.SKTypeface.FromFamilyName("Arial", SkiaSharp.SKFontStyle.Bold)
            };
            
            var errorPaint = new SkiaSharp.SKPaint
            {
                Color = SkiaSharp.SKColors.Red,
                TextSize = 14,
                IsAntialias = true
            };
            
            // タイトル描画
            canvas.DrawText($"Page Screenshot: {url}", 20, 40, titlePaint);
            canvas.DrawText($"Captured: {DateTime.Now:yyyy/MM/dd HH:mm:ss}", 20, 70, paint);
            
            // エラー情報
            canvas.DrawText($"Note: Browser screenshot failed, showing page content", 20, 100, errorPaint);
            canvas.DrawText($"Error: {errorMessage}", 20, 120, errorPaint);
            
            // HTMLの一部を描画
            var lines = html.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).Take(30).ToArray();
            float y = 160;
            foreach (var line in lines)
            {
                var cleanLine = line.Trim();
                if (cleanLine.Length > 100) cleanLine = cleanLine.Substring(0, 100) + "...";
                
                // HTMLタグを色分け
                var linePaint = cleanLine.Contains("<") && cleanLine.Contains(">") ? 
                    new SkiaSharp.SKPaint { Color = SkiaSharp.SKColors.DarkGreen, TextSize = 12, IsAntialias = true } : 
                    new SkiaSharp.SKPaint { Color = SkiaSharp.SKColors.Black, TextSize = 12, IsAntialias = true };
                
                canvas.DrawText(cleanLine, 20, y, linePaint);
                y += 18;
                if (y > height - 50) break;
            }
            
            // 枠線描画
            var borderPaint = new SkiaSharp.SKPaint
            {
                Color = SkiaSharp.SKColors.Gray,
                Style = SkiaSharp.SKPaintStyle.Stroke,
                StrokeWidth = 2
            };
            canvas.DrawRect(10, 10, width - 20, height - 20, borderPaint);
            
            using var image = surface.Snapshot();
            using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite(outputPath);
            data.SaveTo(stream);
            
            return outputPath;
        }
        catch
        {
            // 最終的なフォールバック - 空の画像
            return await CreateEmptyScreenshot(outputPath);
        }
    }
    
    private static async Task<string> CreateEmptyScreenshot(string outputPath)
    {
        var width = 1200;
        var height = 600;
        
        using var surface = SkiaSharp.SKSurface.Create(new SkiaSharp.SKImageInfo(width, height));
        var canvas = surface.Canvas;
        
        canvas.Clear(SkiaSharp.SKColors.LightGray);
        
        var paint = new SkiaSharp.SKPaint
        {
            Color = SkiaSharp.SKColors.Black,
            TextSize = 24,
            IsAntialias = true,
            Typeface = SkiaSharp.SKTypeface.Default
        };
        
        canvas.DrawText("Screenshot not available", width / 2 - 150, height / 2, paint);
        canvas.DrawText($"Generated: {DateTime.Now:yyyy/MM/dd HH:mm:ss}", width / 2 - 120, height / 2 + 40, paint);
        
        using var image = surface.Snapshot();
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);
        
        return outputPath;
    }
}