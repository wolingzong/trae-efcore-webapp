using Microsoft.Playwright;
using System;
using System.IO;
using System.Threading.Tasks;

namespace EfCoreWebApp.Tests.Utils;

public static class VideoRecorder
{
    public static async Task RecordAcceptanceScenarioAsync(string baseUrl, string outputVideoPath)
    {
        // Playwrightのインストール（初回実行時のみ必要）
        using var playwright = await Playwright.CreateAsync();
        
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true
        });

        // ビデオ録画用の設定を含むコンテキストを作成
        // 一時ディレクトリに録画し、後で移動する
        var tempVideoDir = Path.Combine(Path.GetTempPath(), "playwright_videos_" + Guid.NewGuid());
        Directory.CreateDirectory(tempVideoDir);

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            RecordVideoDir = tempVideoDir,
            RecordVideoSize = new RecordVideoSize { Width = 1280, Height = 720 },
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 }
        });

        var page = await context.NewPageAsync();

        try
        {
            // ユーザーシナリオの実行（動画として記録される）
            
            // 1. ホームページにアクセス
            await page.GotoAsync(baseUrl);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(1000); // 視覚的な確認のための待機

            // 2. Productsリンクをクリック
            await page.ClickAsync("a[href='/products']");
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            await Task.Delay(2000); // 画面遷移後の待機

            // 3. 商品一覧を確認（スクロールなどの動作を入れるとより良い）
            await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
            await Task.Delay(1000);
        }
        finally
        {
            // コンテキストを閉じるとビデオファイルが保存される
            await context.CloseAsync();
            
            // 保存されたビデオファイルを取得して移動/変換
            var videoPage = await page.Video!.PathAsync();
            if (File.Exists(videoPage))
            {
                // 出力先ディレクトリがない場合は作成
                var outputDir = Path.GetDirectoryName(outputVideoPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                // 既存ファイルがあれば削除
                if (File.Exists(outputVideoPath))
                {
                    File.Delete(outputVideoPath);
                }

            // 拡張子によって処理を分岐
            if (outputVideoPath.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase))
            {
                await ConvertWebMToMp4(videoPage, outputVideoPath);
            }
            else
            {
                // そのまま移動 (.webmなど)
                File.Move(videoPage, outputVideoPath);
            }
        
            // 一時ディレクトリのクリーンアップ
            if (Directory.Exists(tempVideoDir))
            {
                try { Directory.Delete(tempVideoDir, true); } catch { }
            }
        }
        }
    }

    private static async Task ConvertWebMToMp4(string inputWebm, string outputMp4)
    {
        // プロジェクトルートにある ffmpeg を探す
        // テスト実行時のカレントディレクトリは通常 bin/Debug/netX.X/TestResults/{GUID} または bin/Debug/netX.X
        // しかし、ffmpegは efcore-webapp.Tests/ffmpeg にダウンロードした。
        
        // 1. カレントディレクトリの親を辿って ffmpeg を探す
        var currentDir = Directory.GetCurrentDirectory();
        string ffmpegExecutable = "ffmpeg"; // デフォルトはパス上のもの
        
        // 探索ロジック: binフォルダから遡ってプロジェクトルートを探す
        var searchDir = currentDir;
        for (int i = 0; i < 5; i++)
        {
            var candidate = Path.Combine(searchDir, "ffmpeg");
            if (File.Exists(candidate))
            {
                ffmpegExecutable = candidate;
                break;
            }
            var parent = Directory.GetParent(searchDir);
            if (parent == null) break;
            searchDir = parent.FullName;
        }

        // CreateNoWindow = true so we don't pop up a terminal
        var processInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = ffmpegExecutable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        processInfo.ArgumentList.Add("-i");
        processInfo.ArgumentList.Add(inputWebm);
        // Using standard H.264/AAC for maximum compatibility
        processInfo.ArgumentList.Add("-c:v");
        processInfo.ArgumentList.Add("libx264");
        processInfo.ArgumentList.Add("-c:a");
        processInfo.ArgumentList.Add("aac");
        processInfo.ArgumentList.Add("-y"); // Overwrite output
        processInfo.ArgumentList.Add(outputMp4);

        using var process = System.Diagnostics.Process.Start(processInfo);
        if (process != null)
        {
            await process.WaitForExitAsync();
            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                var output = await process.StandardOutput.ReadToEndAsync();
                throw new Exception($"FFmpeg conversion failed.\nBinary: {ffmpegExecutable}\nError: {error}\nOutput: {output}");
            }
        }
    }
}
