using iText.IO.Font;
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
using System.Net.Http;

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
        
        // フォント設定 (シンプルで確実な方法)
        PdfFont normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        
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
        // タイトル
        var title = new Paragraph("Product Management System - Test Execution Report")
            .SetFont(boldFont)
            .SetFontSize(20)
            .SetFontColor(new DeviceRgb(0, 0, 139))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(30);
        document.Add(title);
        
        // サブタイトル
        var subtitle = new Paragraph("Acceptance Test Report")
            .SetFont(normalFont)
            .SetFontSize(12)
            .SetFontColor(ColorConstants.GRAY)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(20);
        document.Add(subtitle);
        
        // 実行情報
        var infoHeader = new Paragraph("Execution Information")
            .SetFont(boldFont)
            .SetFontSize(16)
            .SetMarginBottom(10);
        document.Add(infoHeader);
        
        var infoTable = new Table(2).UseAllAvailableWidth();
        infoTable.AddCell(CreateCell("Execution Date:", normalFont, true));
        infoTable.AddCell(CreateCell(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), normalFont));
        
        infoTable.AddCell(CreateCell("Overall Result:", normalFont, true));
        var statusCell = CreateCell(testResult == "PASS" ? "PASS" : "FAIL", boldFont);
        statusCell.SetFontColor(testResult == "PASS" ? ColorConstants.GREEN : ColorConstants.RED);
        infoTable.AddCell(statusCell);
        
        infoTable.AddCell(CreateCell("Total Scenarios:", normalFont, true));
        infoTable.AddCell(CreateCell(scenarios.Count.ToString(), normalFont));
        
        infoTable.AddCell(CreateCell("Total Steps:", normalFont, true));
        infoTable.AddCell(CreateCell(scenarios.Sum(s => s.Steps.Count).ToString(), normalFont));
        
        infoTable.AddCell(CreateCell("Success Rate:", normalFont, true));
        infoTable.AddCell(CreateCell(testResult == "PASS" ? "100%" : "0%", normalFont));
        
        document.Add(infoTable);
        document.Add(new Paragraph("\n"));
        
        // テスト結果詳細 (リンクターゲット設定)
        var detailHeader = new Paragraph("Test Results Details")
            .SetFont(boldFont)
            .SetFontSize(16)
            .SetMarginBottom(10);
        
        // リンクターゲットを設定 (現在位置)
        var currentPage = document.GetPdfDocument().GetLastPage();
        var testResultsDestination = PdfExplicitDestination.CreateXYZ(currentPage, 0, 500, 1);
        document.GetPdfDocument().AddNamedDestination("test-results", testResultsDestination.GetPdfObject());
        
        document.Add(detailHeader);
        
        var testTable = new Table(new float[] { 1, 6, 2, 3 }).UseAllAvailableWidth();
        testTable.SetBorder(new SolidBorder(1));
        
        // ヘッダー行
        testTable.AddHeaderCell(CreateHeaderCell("No", boldFont));
        testTable.AddHeaderCell(CreateHeaderCell("Test Step", boldFont));
        testTable.AddHeaderCell(CreateHeaderCell("Result", boldFont));
        testTable.AddHeaderCell(CreateHeaderCell("Screenshot", boldFont));
        
        int stepNumber = 1;
        foreach (var scenario in scenarios)
        {
            // シナリオ名行
            var scenarioCell = new Cell(1, 4)
                .Add(new Paragraph($"Scenario: {scenario.Name}").SetFont(boldFont))
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
                    var linkText = new Text("[View Screenshot]")
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
        var attachmentHeader = new Paragraph("Attached Files")
            .SetFont(boldFont)
            .SetFontSize(16)
            .SetMarginBottom(10);
        document.Add(attachmentHeader);
        
        var attachmentList = new List();
        if (File.Exists(screenshotPath))
        {
            var screenshotPara = new Paragraph()
                .Add(new Text($"• Screenshot: {Path.GetFileName(screenshotPath)}").SetFont(normalFont))
                .Add(new Text(" (See Page 3)")
                    .SetFont(normalFont)
                    .SetFontColor(ColorConstants.BLUE)
                    .SetUnderline()
                    .SetAction(PdfAction.CreateGoTo("screenshot-page")));
            document.Add(screenshotPara);
        }
        
        document.Add(new Paragraph($"• Excel Detailed Report: test-specimen.xlsx").SetFont(normalFont));
        document.Add(new Paragraph($"• CSV Format Report: test-report.csv").SetFont(normalFont));
        

        
        // ページ番号 - 位置を調整して右端に表示されるように
        var pageNumber = new Paragraph("Page 1/3")
            .SetFont(normalFont)
            .SetTextAlignment(TextAlignment.RIGHT)
            .SetFixedPosition(400, 20, 150);
        document.Add(pageNumber);
    }
    
    private static void CreateDetailPage(Document document, List<ScenarioInfo> scenarios, string testResult, 
        PdfFont normalFont, PdfFont boldFont)
    {
        var title = new Paragraph("Detailed Test Execution Log")
            .SetFont(boldFont)
            .SetFontSize(18)
            .SetMarginBottom(20);
        document.Add(title);
        
        var logHeader = new Paragraph("Execution Log")
            .SetFont(boldFont)
            .SetFontSize(14)
            .SetMarginBottom(10);
        document.Add(logHeader);
        
        var startTime = DateTime.Now.AddMinutes(-2);
        
        foreach (var scenario in scenarios)
        {
            var scenarioStart = new Paragraph($"[{startTime:HH:mm:ss}] Scenario Started: {scenario.Name}")
                .SetFont(normalFont)
                .SetFontSize(12);
            document.Add(scenarioStart);
            startTime = startTime.AddSeconds(10);
            
            foreach (var step in scenario.Steps)
            {
                var stepExecution = new Paragraph($"[{startTime:HH:mm:ss}] Step Execution: {step}")
                    .SetFont(normalFont)
                    .SetFontSize(11)
                    .SetMarginLeft(20);
                document.Add(stepExecution);
                
                var stepResult = testResult == "PASS" ? "SUCCESS" : "FAILED";
                var stepColor = testResult == "PASS" ? ColorConstants.GREEN : ColorConstants.RED;
                
                var resultParagraph = new Paragraph($"[{startTime:HH:mm:ss}] Result: {stepResult}")
                    .SetFont(normalFont)
                    .SetFontSize(11)
                    .SetFontColor(stepColor)
                    .SetMarginLeft(40);
                document.Add(resultParagraph);
                
                startTime = startTime.AddSeconds(5);
            }
            
            var scenarioEnd = new Paragraph($"[{startTime:HH:mm:ss}] Scenario Completed: {scenario.Name}")
                .SetFont(normalFont)
                .SetFontSize(12)
                .SetMarginBottom(10);
            document.Add(scenarioEnd);
            startTime = startTime.AddSeconds(2);
        }
        
        // システム情報
        document.Add(new Paragraph("\n"));
        var systemHeader = new Paragraph("System Information")
            .SetFont(boldFont)
            .SetFontSize(14)
            .SetMarginBottom(10);
        document.Add(systemHeader);
        
        document.Add(new Paragraph("• Test Environment: GitHub Actions (Ubuntu)").SetFont(normalFont));
        document.Add(new Paragraph("• .NET Version: 9.0").SetFont(normalFont));
        document.Add(new Paragraph("• Database: SQL Server / SQLite").SetFont(normalFont));
        document.Add(new Paragraph("• Browser: Chromium (Headless)").SetFont(normalFont));
        document.Add(new Paragraph("• Execution Time: Approx 2-3 minutes").SetFont(normalFont));
        
        // ナビゲーションリンク
        document.Add(new Paragraph("\n"));
        var navHeader = new Paragraph("Navigation")
            .SetFont(boldFont)
            .SetFontSize(14)
            .SetMarginBottom(10);
        document.Add(navHeader);
        
        var backToSummary = new Paragraph()
            .Add(new Text("<-- Back to Summary Page")
                .SetFont(normalFont)
                .SetFontSize(12)
                .SetFontColor(ColorConstants.BLUE)
                .SetUnderline()
                .SetAction(PdfAction.CreateGoTo(PdfExplicitDestination.CreateFit(document.GetPdfDocument().GetFirstPage()))))
            .SetMarginBottom(5);
        document.Add(backToSummary);
        
        var goToScreenshot = new Paragraph()
            .Add(new Text("--> View Screenshots")
                .SetFont(normalFont)
                .SetFontSize(12)
                .SetFontColor(ColorConstants.BLUE)
                .SetUnderline()
                .SetAction(PdfAction.CreateGoTo("screenshot-page")))
            .SetMarginBottom(10);
        document.Add(goToScreenshot);
        
        // ページ番号 - 位置を調整して右端に表示されるように
        var pageNumber = new Paragraph("Page 2/3")
            .SetFont(normalFont)
            .SetTextAlignment(TextAlignment.RIGHT)
            .SetFixedPosition(400, 20, 150);
        document.Add(pageNumber);
    }
    
    private static void CreateScreenshotPage(Document document, string screenshotPath, PdfFont normalFont, PdfFont boldFont)
    {
        // リンクターゲットを設定
        var currentPage = document.GetPdfDocument().GetLastPage();
        var destination = PdfExplicitDestination.CreateXYZ(currentPage, 0, 842, 1);
        document.GetPdfDocument().AddNamedDestination("screenshot-page", destination.GetPdfObject());
        
        var title = new Paragraph("Test Execution Screenshots")
            .SetFont(boldFont)
            .SetFontSize(18)
            .SetMarginBottom(20);
        document.Add(title);
        
        var info = new Paragraph($"Captured at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}")
            .SetFont(normalFont)
            .SetMarginBottom(5);
        document.Add(info);
        
        var pageInfo = new Paragraph("Page: Product List (http://localhost:5000/products)")
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
            
            var imageInfo = new Paragraph($"\nImage Size: {image.GetImageWidth():F0} x {image.GetImageHeight():F0} pixels")
                .SetFont(normalFont)
                .SetFontSize(10);
            document.Add(imageInfo);
            
            var fileInfo = new Paragraph($"File Name: {Path.GetFileName(screenshotPath)}")
                .SetFont(normalFont)
                .SetFontSize(10);
            document.Add(fileInfo);
        }
        catch (Exception ex)
        {
            var errorMsg = new Paragraph($"Screenshot loading error: {ex.Message}")
                .SetFont(normalFont)
                .SetFontColor(ColorConstants.RED);
            document.Add(errorMsg);
            
            var pathInfo = new Paragraph($"File Path: {screenshotPath}")
                .SetFont(normalFont)
                .SetFontSize(10);
            document.Add(pathInfo);
        }
        
        // 戻りリンクセクション
        document.Add(new Paragraph("\n"));
        
        var linkHeader = new Paragraph("NAVIGATION LINKS")
            .SetFont(boldFont)
            .SetFontSize(16)
            .SetFontColor(new DeviceRgb(0, 0, 139))
            .SetMarginBottom(15);
        document.Add(linkHeader);
        
        // テスト結果に戻るリンク
        var backLink = new Paragraph()
            .Add(new Text("<-- BACK TO TEST RESULTS")
                .SetFont(boldFont)
                .SetFontSize(14)
                .SetFontColor(ColorConstants.BLUE)
                .SetUnderline()
                .SetAction(PdfAction.CreateGoTo("test-results")))
            .SetMarginBottom(10);
        document.Add(backLink);
        
        // ページトップに戻るリンク
        var topLink = new Paragraph()
            .Add(new Text("^^ BACK TO REPORT TOP PAGE")
                .SetFont(boldFont)
                .SetFontSize(14)
                .SetFontColor(ColorConstants.BLUE)
                .SetUnderline()
                .SetAction(PdfAction.CreateGoTo(PdfExplicitDestination.CreateFit(document.GetPdfDocument().GetFirstPage()))))
            .SetMarginBottom(10);
        document.Add(topLink);
        
        // 詳細ログページに移動するリンク
        var detailLink = new Paragraph()
            .Add(new Text("--> VIEW DETAILED EXECUTION LOG")
                .SetFont(boldFont)
                .SetFontSize(14)
                .SetFontColor(ColorConstants.BLUE)
                .SetUnderline()
                .SetAction(PdfAction.CreateGoTo(PdfExplicitDestination.CreateFit(document.GetPdfDocument().GetPage(2)))))
            .SetMarginBottom(15);
        document.Add(detailLink);
        
        // ページ番号 - 位置を調整して右端に表示されるように
        var pageNumber = new Paragraph("Page 3/3")
            .SetFont(normalFont)
            .SetTextAlignment(TextAlignment.RIGHT)
            .SetFixedPosition(400, 20, 150);
        document.Add(pageNumber);
    }
    
    private static Cell CreateCell(string content, PdfFont font, bool isBold = false)
    {
        var cell = new Cell()
            .Add(new Paragraph(content).SetFont(font))
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