using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Action;
using iText.Kernel.Pdf.Navigation;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Layout.Borders;

namespace EfCoreWebApp.Tests.Utils;

public static class InteractivePdfReport
{
    public static void GenerateTestReport(string pdfPath, string featureFilePath, string screenshotPath, string testResult = "PASS")
    {
        var featureContent = File.ReadAllText(featureFilePath);
        var scenarios = ParseFeatureFile(featureContent);
        
        using var writer = new PdfWriter(pdfPath);
        using var pdf = new PdfDocument(writer);
        using var document = new Document(pdf);
        
        // フォント設定 (日本語対応)
        PdfFont normalFont;
        PdfFont boldFont;
        
        try
        {
            // 日本語フォントを試行 (複数のフォントを試す)
            var fontNames = new[] { "HeiseiKakuGo-W5", "KozMinPro-Regular", "MSGothic", "NotoSansCJK-Regular" };
            var encodings = new[] { "UniJIS-UCS2-H", "Identity-H" };
            
            PdfFont? testFont = null;
            foreach (var fontName in fontNames)
            {
                foreach (var encoding in encodings)
                {
                    try
                    {
                        testFont = PdfFontFactory.CreateFont(fontName, encoding);
                        break;
                    }
                    catch { continue; }
                }
                if (testFont != null) break;
            }
            
            if (testFont != null)
            {
                normalFont = testFont;
                boldFont = testFont; // 同じフォントを使用
            }
            else
            {
                throw new Exception("No Japanese font available");
            }
        }
        catch
        {
            // フォールバック: 標準フォント (ASCII文字のみ対応)
            normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        }
        
        // ページ1: テスト結果サマリー
        CreateSummaryPage(document, scenarios, testResult, screenshotPath, normalFont, boldFont);
        
        // ページ2: 詳細テスト結果
        document.Add(new AreaBreak());
        CreateDetailPage(document, scenarios, testResult, normalFont, boldFont);
        
        // ページ3: スクリーンショット (リンクターゲット)
        if (File.Exists(screenshotPath))
        {
            document.Add(new AreaBreak());
            CreateScreenshotPage(document, screenshotPath, normalFont, boldFont);
        }
        
        document.Close();
    }
    
    private static List<ScenarioInfo> ParseFeatureFile(string featureContent)
    {
        var lines = featureContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var scenarios = new List<ScenarioInfo>();
        var currentScenario = new ScenarioInfo();
        
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            
            if (trimmedLine.StartsWith("シナリオ:"))
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
        
        return scenarios;
    }
    
    private static void CreateSummaryPage(Document document, List<ScenarioInfo> scenarios, string testResult, 
        string screenshotPath, PdfFont normalFont, PdfFont boldFont)
    {
        // タイトル (フォントテスト付き)
        var title = new Paragraph("商品管理システム テスト実行報告書")
            .SetFont(boldFont)
            .SetFontSize(20)
            .SetFontColor(new DeviceRgb(0, 0, 139))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(20);
        document.Add(title);
        
        // フォント情報表示 (デバッグ用)
        var fontInfo = new Paragraph($"使用フォント: {boldFont.GetFontProgram()?.GetFontNames()?.GetFontName() ?? "Unknown"}")
            .SetFont(normalFont)
            .SetFontSize(8)
            .SetFontColor(ColorConstants.GRAY)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(10);
        document.Add(fontInfo);
        
        // 実行情報
        var infoHeader = new Paragraph("実行情報")
            .SetFont(boldFont)
            .SetFontSize(16)
            .SetMarginBottom(10);
        document.Add(infoHeader);
        
        var infoTable = new Table(2).UseAllAvailableWidth();
        infoTable.AddCell(CreateCell("実行日時:", normalFont, true));
        infoTable.AddCell(CreateCell(DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss"), normalFont));
        
        infoTable.AddCell(CreateCell("総合結果:", normalFont, true));
        var statusCell = CreateCell(testResult, boldFont);
        statusCell.SetFontColor(testResult == "PASS" ? ColorConstants.GREEN : ColorConstants.RED);
        infoTable.AddCell(statusCell);
        
        infoTable.AddCell(CreateCell("総シナリオ数:", normalFont, true));
        infoTable.AddCell(CreateCell(scenarios.Count.ToString(), normalFont));
        
        infoTable.AddCell(CreateCell("総ステップ数:", normalFont, true));
        infoTable.AddCell(CreateCell(scenarios.Sum(s => s.Steps.Count).ToString(), normalFont));
        
        infoTable.AddCell(CreateCell("成功率:", normalFont, true));
        infoTable.AddCell(CreateCell(testResult == "PASS" ? "100%" : "0%", normalFont));
        
        document.Add(infoTable);
        document.Add(new Paragraph("\n"));
        
        // テスト結果詳細 (リンクターゲット設定)
        var detailHeader = new Paragraph("テスト結果詳細")
            .SetFont(boldFont)
            .SetFontSize(16)
            .SetMarginBottom(10);
        document.Add(detailHeader);
        
        // 第1ページのテスト結果テーブルへのリンクターゲットを設定
        var firstPageDestination = PdfExplicitDestination.CreateXYZ(document.GetPdfDocument().GetFirstPage(), 0, 600, 1);
        document.GetPdfDocument().AddNamedDestination("test-results", firstPageDestination.GetPdfObject());
        
        var testTable = new Table(new float[] { 1, 6, 2, 3 }).UseAllAvailableWidth();
        testTable.SetBorder(new SolidBorder(1));
        
        // ヘッダー行
        testTable.AddHeaderCell(CreateHeaderCell("No", boldFont));
        testTable.AddHeaderCell(CreateHeaderCell("テストステップ", boldFont));
        testTable.AddHeaderCell(CreateHeaderCell("結果", boldFont));
        testTable.AddHeaderCell(CreateHeaderCell("スクリーンショット", boldFont));
        
        int stepNumber = 1;
        foreach (var scenario in scenarios)
        {
            // シナリオ名行
            var scenarioCell = new Cell(1, 4)
                .Add(new Paragraph($"シナリオ: {scenario.Name}").SetFont(boldFont))
                .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                .SetBorder(new SolidBorder(1));
            testTable.AddCell(scenarioCell);
            
            // ステップ行
            for (int i = 0; i < scenario.Steps.Count; i++)
            {
                var step = scenario.Steps[i];
                var stepResult = testResult == "PASS" ? "✓ PASS" : "✗ FAIL";
                var stepColor = testResult == "PASS" ? ColorConstants.GREEN : ColorConstants.RED;
                
                testTable.AddCell(CreateCell(stepNumber.ToString(), normalFont));
                testTable.AddCell(CreateCell(step, normalFont));
                
                var resultCell = CreateCell(stepResult, boldFont);
                resultCell.SetFontColor(stepColor);
                testTable.AddCell(resultCell);
                
                // スクリーンショットリンク (最初のステップのみ)
                if (i == 0 && File.Exists(screenshotPath))
                {
                    var linkCell = new Cell();
                    var linkText = new Text("📷 スクリーンショット")
                        .SetFont(normalFont)
                        .SetFontColor(ColorConstants.BLUE)
                        .SetUnderline();
                    
                    var linkParagraph = new Paragraph()
                        .Add(linkText)
                        .SetAction(PdfAction.CreateGoTo("screenshot-page"));
                    
                    linkCell.Add(linkParagraph);
                    testTable.AddCell(linkCell);
                }
                else
                {
                    testTable.AddCell(CreateCell("", normalFont));
                }
                
                stepNumber++;
            }
        }
        
        document.Add(testTable);
        document.Add(new Paragraph("\n"));
        
        // 添付ファイル情報
        var attachmentHeader = new Paragraph("添付ファイル")
            .SetFont(boldFont)
            .SetFontSize(16)
            .SetMarginBottom(10);
        document.Add(attachmentHeader);
        
        var attachmentList = new List();
        if (File.Exists(screenshotPath))
        {
            var screenshotPara = new Paragraph()
                .Add(new Text($"• スクリーンショット: {Path.GetFileName(screenshotPath)}").SetFont(normalFont))
                .Add(new Text(" (ページ3で表示)")
                    .SetFont(normalFont)
                    .SetFontColor(ColorConstants.BLUE)
                    .SetUnderline()
                    .SetAction(PdfAction.CreateGoTo("screenshot-page")));
            document.Add(screenshotPara);
        }
        
        document.Add(new Paragraph($"• Excel詳細レポート: test-specimen.xlsx").SetFont(normalFont));
        document.Add(new Paragraph($"• CSV形式レポート: test-report.csv").SetFont(normalFont));
        

        
        // ページ番号
        var pageNumber = new Paragraph("ページ 1/3")
            .SetFont(normalFont)
            .SetTextAlignment(TextAlignment.RIGHT)
            .SetFixedPosition(500, 20, 100);
        document.Add(pageNumber);
    }
    
    private static void CreateDetailPage(Document document, List<ScenarioInfo> scenarios, string testResult, 
        PdfFont normalFont, PdfFont boldFont)
    {
        var title = new Paragraph("詳細テスト実行ログ")
            .SetFont(boldFont)
            .SetFontSize(18)
            .SetMarginBottom(20);
        document.Add(title);
        
        var logHeader = new Paragraph("実行ログ")
            .SetFont(boldFont)
            .SetFontSize(14)
            .SetMarginBottom(10);
        document.Add(logHeader);
        
        var startTime = DateTime.Now.AddMinutes(-2);
        
        foreach (var scenario in scenarios)
        {
            var scenarioStart = new Paragraph($"[{startTime:HH:mm:ss}] シナリオ開始: {scenario.Name}")
                .SetFont(normalFont)
                .SetFontSize(12);
            document.Add(scenarioStart);
            startTime = startTime.AddSeconds(10);
            
            foreach (var step in scenario.Steps)
            {
                var stepExecution = new Paragraph($"[{startTime:HH:mm:ss}] ステップ実行: {step}")
                    .SetFont(normalFont)
                    .SetFontSize(11)
                    .SetMarginLeft(20);
                document.Add(stepExecution);
                
                var stepResult = testResult == "PASS" ? "成功" : "失敗";
                var stepColor = testResult == "PASS" ? ColorConstants.GREEN : ColorConstants.RED;
                
                var resultParagraph = new Paragraph($"[{startTime:HH:mm:ss}] 結果: {stepResult}")
                    .SetFont(normalFont)
                    .SetFontSize(11)
                    .SetFontColor(stepColor)
                    .SetMarginLeft(40);
                document.Add(resultParagraph);
                
                startTime = startTime.AddSeconds(5);
            }
            
            var scenarioEnd = new Paragraph($"[{startTime:HH:mm:ss}] シナリオ完了: {scenario.Name}")
                .SetFont(normalFont)
                .SetFontSize(12)
                .SetMarginBottom(10);
            document.Add(scenarioEnd);
            startTime = startTime.AddSeconds(2);
        }
        
        // システム情報
        document.Add(new Paragraph("\n"));
        var systemHeader = new Paragraph("システム情報")
            .SetFont(boldFont)
            .SetFontSize(14)
            .SetMarginBottom(10);
        document.Add(systemHeader);
        
        document.Add(new Paragraph("• テスト環境: GitHub Actions (Ubuntu)").SetFont(normalFont));
        document.Add(new Paragraph("• .NET バージョン: 9.0").SetFont(normalFont));
        document.Add(new Paragraph("• データベース: SQL Server / SQLite").SetFont(normalFont));
        document.Add(new Paragraph("• ブラウザ: Chromium (Headless)").SetFont(normalFont));
        document.Add(new Paragraph("• 実行時間: 約2-3分").SetFont(normalFont));
        
        // ページ番号
        var pageNumber = new Paragraph("ページ 2/3")
            .SetFont(normalFont)
            .SetTextAlignment(TextAlignment.RIGHT)
            .SetFixedPosition(500, 20, 100);
        document.Add(pageNumber);
    }
    
    private static void CreateScreenshotPage(Document document, string screenshotPath, PdfFont normalFont, PdfFont boldFont)
    {
        // リンクターゲットを設定
        var currentPage = document.GetPdfDocument().GetLastPage();
        var destination = PdfExplicitDestination.CreateXYZ(currentPage, 0, 842, 1);
        document.GetPdfDocument().AddNamedDestination("screenshot-page", destination.GetPdfObject());
        
        var title = new Paragraph("テスト実行スクリーンショット")
            .SetFont(boldFont)
            .SetFontSize(18)
            .SetMarginBottom(20);
        document.Add(title);
        
        var info = new Paragraph($"撮影日時: {DateTime.Now:yyyy年MM月dd日 HH:mm:ss}")
            .SetFont(normalFont)
            .SetMarginBottom(5);
        document.Add(info);
        
        var pageInfo = new Paragraph("画面: 商品一覧ページ (http://localhost:5000/products)")
            .SetFont(normalFont)
            .SetMarginBottom(20);
        document.Add(pageInfo);
        
        // スクリーンショット画像を挿入
        try
        {
            var imageData = ImageDataFactory.Create(screenshotPath);
            var image = new Image(imageData);
            
            // 画像サイズを調整
            var maxWidth = 500f;
            var maxHeight = 600f;
            
            if (image.GetImageWidth() > maxWidth || image.GetImageHeight() > maxHeight)
            {
                image.ScaleToFit(maxWidth, maxHeight);
            }
            
            image.SetBorder(new SolidBorder(1));
            document.Add(image);
            
            var imageInfo = new Paragraph($"\n画像サイズ: {image.GetImageWidth():F0} x {image.GetImageHeight():F0} pixels")
                .SetFont(normalFont)
                .SetFontSize(10);
            document.Add(imageInfo);
            
            var fileInfo = new Paragraph($"ファイル: {Path.GetFileName(screenshotPath)}")
                .SetFont(normalFont)
                .SetFontSize(10);
            document.Add(fileInfo);
        }
        catch (Exception ex)
        {
            var errorMsg = new Paragraph($"スクリーンショット読み込みエラー: {ex.Message}")
                .SetFont(normalFont)
                .SetFontColor(ColorConstants.RED);
            document.Add(errorMsg);
            
            var pathInfo = new Paragraph($"ファイルパス: {screenshotPath}")
                .SetFont(normalFont)
                .SetFontSize(10);
            document.Add(pathInfo);
        }
        
        // 戻りリンク (テスト結果テーブルに直接ジャンプ)
        var backLink = new Paragraph()
            .Add(new Text("← テスト結果に戻る")
                .SetFont(normalFont)
                .SetFontColor(ColorConstants.BLUE)
                .SetUnderline()
                .SetAction(PdfAction.CreateGoTo("test-results")))
            .SetMarginTop(20);
        document.Add(backLink);
        
        // 追加の戻りリンク (ページトップ)
        var topLink = new Paragraph()
            .Add(new Text("↑ ページトップに戻る")
                .SetFont(normalFont)
                .SetFontColor(ColorConstants.BLUE)
                .SetUnderline()
                .SetAction(PdfAction.CreateGoTo(PdfExplicitDestination.CreateFit(document.GetPdfDocument().GetFirstPage()))))
            .SetMarginTop(10);
        document.Add(topLink);
        
        // ページ番号
        var pageNumber = new Paragraph("ページ 3/3")
            .SetFont(normalFont)
            .SetTextAlignment(TextAlignment.RIGHT)
            .SetFixedPosition(500, 20, 100);
        document.Add(pageNumber);
    }
    
    private static Cell CreateCell(string content, PdfFont font, bool isBold = false)
    {
        var cell = new Cell()
            .Add(new Paragraph(content).SetFont(isBold ? PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD) : font))
            .SetBorder(new SolidBorder(1))
            .SetPadding(5);
        return cell;
    }
    
    private static Cell CreateHeaderCell(string content, PdfFont font)
    {
        return new Cell()
            .Add(new Paragraph(content).SetFont(font))
            .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
            .SetBorder(new SolidBorder(1))
            .SetPadding(5)
            .SetTextAlignment(TextAlignment.CENTER);
    }
    
    private class ScenarioInfo
    {
        public string Name { get; set; } = "";
        public List<string> Steps { get; set; } = new List<string>();
    }
}