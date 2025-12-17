using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace EfCoreWebApp.Tests.Utils;

public static class WordReportGenerator
{
    public static void GenerateTestReport(string wordPath, string featureFilePath, string screenshotPath, string testResult = "PASS")
    {
        var featureContent = File.ReadAllText(featureFilePath);
        var scenarios = ParseFeatureFile(featureContent);
        
        using var wordDocument = WordprocessingDocument.Create(wordPath, WordprocessingDocumentType.Document);
        var mainPart = wordDocument.AddMainDocumentPart();
        mainPart.Document = new Document();
        var body = mainPart.Document.AppendChild(new Body());
        
        // タイトル
        AddTitle(body, "商品管理システム - テスト実行報告書");
        AddSubtitle(body, "受入テストレポート (Word版)");
        
        // 実行情報テーブル
        AddHeading(body, "実行情報", 2);
        var infoTable = CreateInfoTable(scenarios, testResult);
        body.AppendChild(infoTable);
        
        AddParagraph(body, "");
        
        // テスト結果詳細 (ブックマーク付き)
        var detailHeading = new Paragraph();
        var detailBookmarkStart = new BookmarkStart { Id = "100", Name = "test-results" };
        var detailBookmarkEnd = new BookmarkEnd { Id = "100" };
        
        detailHeading.AppendChild(detailBookmarkStart);
        var detailRun = detailHeading.AppendChild(new Run());
        var detailRunProperties = detailRun.AppendChild(new RunProperties());
        detailRunProperties.AppendChild(new Bold());
        detailRunProperties.AppendChild(new FontSize { Val = "28" });
        detailRun.AppendChild(new Text("テスト結果詳細"));
        detailHeading.AppendChild(detailBookmarkEnd);
        
        var detailParagraphProperties = detailHeading.InsertAt(new ParagraphProperties(), 0);
        detailParagraphProperties.AppendChild(new SpacingBetweenLines { Before = "200", After = "200" });
        
        body.AppendChild(detailHeading);
        var hasScreenshot = File.Exists(screenshotPath);
        var testTable = CreateTestResultsTable(scenarios, testResult, hasScreenshot);
        body.AppendChild(testTable);
        
        AddParagraph(body, "");
        
        // スクリーンショット (ブックマーク付き)
        if (File.Exists(screenshotPath))
        {
            var screenshotHeading = new Paragraph();
            var bookmarkStart = new BookmarkStart { Id = "200", Name = "screenshot" };
            var bookmarkEnd = new BookmarkEnd { Id = "200" };
            
            screenshotHeading.AppendChild(bookmarkStart);
            var run = screenshotHeading.AppendChild(new Run());
            var runProperties = run.AppendChild(new RunProperties());
            runProperties.AppendChild(new Bold());
            runProperties.AppendChild(new FontSize { Val = "28" });
            run.AppendChild(new Text("スクリーンショット"));
            screenshotHeading.AppendChild(bookmarkEnd);
            
            var paragraphProperties = screenshotHeading.InsertAt(new ParagraphProperties(), 0);
            paragraphProperties.AppendChild(new SpacingBetweenLines { Before = "200", After = "200" });
            
            body.AppendChild(screenshotHeading);
            
            AddParagraph(body, $"キャプチャ日時: {DateTime.Now:yyyy年MM月dd日 HH:mm:ss}");
            AddParagraph(body, "ページ: 商品一覧 (http://localhost:5000/products)");
            AddImage(mainPart, body, screenshotPath);
            
            // 戻りリンクを追加
            var returnPara = new Paragraph();
            var returnHyperlink = new Hyperlink { Anchor = "test-results", History = true };
            var returnRun = returnHyperlink.AppendChild(new Run());
            var returnProps = returnRun.AppendChild(new RunProperties());
            returnProps.AppendChild(new Color { Val = "0000FF" });
            returnProps.AppendChild(new Underline { Val = UnderlineValues.Single });
            returnRun.AppendChild(new Text("← テスト結果に戻る"));
            returnPara.AppendChild(returnHyperlink);
            body.AppendChild(returnPara);
            
            AddParagraph(body, "");
        }
        
        // システム情報
        AddHeading(body, "システム情報", 2);
        AddBullet(body, "テスト環境: GitHub Actions (Ubuntu)");
        AddBullet(body, ".NETバージョン: 9.0");
        AddBullet(body, "データベース: SQL Server / SQLite");
        AddBullet(body, "ブラウザ: Chromium (ヘッドレス)");
        AddBullet(body, "実行時間: 約2-3分");
        
        AddParagraph(body, "");
        AddParagraph(body, $"生成日時: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        
        mainPart.Document.Save();
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
    
    private static void AddTitle(Body body, string text)
    {
        var paragraph = body.AppendChild(new Paragraph());
        var run = paragraph.AppendChild(new Run());
        var runProperties = run.AppendChild(new RunProperties());
        runProperties.AppendChild(new Bold());
        runProperties.AppendChild(new FontSize { Val = "32" });
        runProperties.AppendChild(new Color { Val = "00008B" });
        run.AppendChild(new Text(text));
        
        var paragraphProperties = paragraph.InsertAt(new ParagraphProperties(), 0);
        paragraphProperties.AppendChild(new Justification { Val = JustificationValues.Center });
        paragraphProperties.AppendChild(new SpacingBetweenLines { After = "400" });
    }
    
    private static void AddSubtitle(Body body, string text)
    {
        var paragraph = body.AppendChild(new Paragraph());
        var run = paragraph.AppendChild(new Run());
        var runProperties = run.AppendChild(new RunProperties());
        runProperties.AppendChild(new FontSize { Val = "24" });
        runProperties.AppendChild(new Color { Val = "808080" });
        run.AppendChild(new Text(text));
        
        var paragraphProperties = paragraph.InsertAt(new ParagraphProperties(), 0);
        paragraphProperties.AppendChild(new Justification { Val = JustificationValues.Center });
        paragraphProperties.AppendChild(new SpacingBetweenLines { After = "300" });
    }
    
    private static void AddHeading(Body body, string text, int level)
    {
        var paragraph = body.AppendChild(new Paragraph());
        var run = paragraph.AppendChild(new Run());
        var runProperties = run.AppendChild(new RunProperties());
        runProperties.AppendChild(new Bold());
        runProperties.AppendChild(new FontSize { Val = level == 2 ? "28" : "24" });
        run.AppendChild(new Text(text));
        
        var paragraphProperties = paragraph.InsertAt(new ParagraphProperties(), 0);
        paragraphProperties.AppendChild(new SpacingBetweenLines { Before = "200", After = "200" });
    }
    
    private static void AddParagraph(Body body, string text)
    {
        var paragraph = body.AppendChild(new Paragraph());
        var run = paragraph.AppendChild(new Run());
        run.AppendChild(new Text(text));
    }
    
    private static void AddBullet(Body body, string text)
    {
        var paragraph = body.AppendChild(new Paragraph());
        var paragraphProperties = paragraph.AppendChild(new ParagraphProperties());
        
        var numberingProperties = paragraphProperties.AppendChild(new NumberingProperties());
        numberingProperties.AppendChild(new NumberingLevelReference { Val = 0 });
        numberingProperties.AppendChild(new NumberingId { Val = 1 });
        
        var run = paragraph.AppendChild(new Run());
        run.AppendChild(new Text("• " + text));
    }
    
    private static Table CreateInfoTable(List<ScenarioInfo> scenarios, string testResult)
    {
        var table = new Table();
        
        var tableProperties = new TableProperties();
        tableProperties.AppendChild(new TableBorders(
            new TopBorder { Val = BorderValues.Single, Size = 4 },
            new BottomBorder { Val = BorderValues.Single, Size = 4 },
            new LeftBorder { Val = BorderValues.Single, Size = 4 },
            new RightBorder { Val = BorderValues.Single, Size = 4 },
            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
            new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
        ));
        tableProperties.AppendChild(new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct });
        table.AppendChild(tableProperties);
        
        // 行を追加
        AddTableRow(table, "実行日時:", DateTime.Now.ToString("yyyy年MM月dd日 HH:mm:ss"), false);
        AddTableRow(table, "総合結果:", testResult == "PASS" ? "✓ 合格" : "✗ 不合格", testResult == "PASS");
        AddTableRow(table, "総シナリオ数:", scenarios.Count.ToString(), false);
        AddTableRow(table, "総ステップ数:", scenarios.Sum(s => s.Steps.Count).ToString(), false);
        AddTableRow(table, "成功率:", testResult == "PASS" ? "100%" : "0%", false);
        
        return table;
    }
    
    private static void AddTableRow(Table table, string label, string value, bool isSuccess)
    {
        var row = new TableRow();
        
        // ラベルセル
        var labelCell = new TableCell();
        var labelPara = labelCell.AppendChild(new Paragraph());
        var labelRun = labelPara.AppendChild(new Run());
        var labelProps = labelRun.AppendChild(new RunProperties());
        labelProps.AppendChild(new Bold());
        labelRun.AppendChild(new Text(label));
        row.AppendChild(labelCell);
        
        // 値セル
        var valueCell = new TableCell();
        var valuePara = valueCell.AppendChild(new Paragraph());
        var valueRun = valuePara.AppendChild(new Run());
        
        if (isSuccess || value.Contains("合格") || value.Contains("不合格"))
        {
            var valueProps = valueRun.AppendChild(new RunProperties());
            valueProps.AppendChild(new Bold());
            valueProps.AppendChild(new Color { Val = value.Contains("合格") ? "008000" : "FF0000" });
        }
        
        valueRun.AppendChild(new Text(value));
        row.AppendChild(valueCell);
        
        table.AppendChild(row);
    }
    
    private static Table CreateTestResultsTable(List<ScenarioInfo> scenarios, string testResult, bool hasScreenshot)
    {
        var table = new Table();
        
        var tableProperties = new TableProperties();
        tableProperties.AppendChild(new TableBorders(
            new TopBorder { Val = BorderValues.Single, Size = 4 },
            new BottomBorder { Val = BorderValues.Single, Size = 4 },
            new LeftBorder { Val = BorderValues.Single, Size = 4 },
            new RightBorder { Val = BorderValues.Single, Size = 4 },
            new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
            new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }
        ));
        tableProperties.AppendChild(new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct });
        table.AppendChild(tableProperties);
        
        // ヘッダー行
        var headerRow = new TableRow();
        AddHeaderCell(headerRow, "No");
        AddHeaderCell(headerRow, "テストステップ");
        AddHeaderCell(headerRow, "結果");
        if (hasScreenshot)
        {
            AddHeaderCell(headerRow, "スクリーンショット");
        }
        table.AppendChild(headerRow);
        
        int stepNumber = 1;
        foreach (var scenario in scenarios)
        {
            // シナリオ名行
            var scenarioRow = new TableRow();
            var scenarioCell = new TableCell();
            var cellProps = scenarioCell.AppendChild(new TableCellProperties());
            cellProps.AppendChild(new GridSpan { Val = hasScreenshot ? 4 : 3 });
            cellProps.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Fill = "D3D3D3" });
            
            var scenarioPara = scenarioCell.AppendChild(new Paragraph());
            var scenarioRun = scenarioPara.AppendChild(new Run());
            var scenarioRunProps = scenarioRun.AppendChild(new RunProperties());
            scenarioRunProps.AppendChild(new Bold());
            scenarioRun.AppendChild(new Text($"シナリオ: {scenario.Name}"));
            scenarioRow.AppendChild(scenarioCell);
            table.AppendChild(scenarioRow);
            
            // ステップ行
            for (int i = 0; i < scenario.Steps.Count; i++)
            {
                var step = scenario.Steps[i];
                var stepRow = new TableRow();
                
                var numCell = new TableCell();
                numCell.AppendChild(new Paragraph(new Run(new Text(stepNumber.ToString()))));
                stepRow.AppendChild(numCell);
                
                var stepCell = new TableCell();
                stepCell.AppendChild(new Paragraph(new Run(new Text(step))));
                stepRow.AppendChild(stepCell);
                
                var resultCell = new TableCell();
                var resultPara = resultCell.AppendChild(new Paragraph());
                var resultRun = resultPara.AppendChild(new Run());
                var resultProps = resultRun.AppendChild(new RunProperties());
                resultProps.AppendChild(new Bold());
                resultProps.AppendChild(new Color { Val = testResult == "PASS" ? "008000" : "FF0000" });
                resultRun.AppendChild(new Text(testResult == "PASS" ? "✓ 合格" : "✗ 不合格"));
                stepRow.AppendChild(resultCell);
                
                // スクリーンショットリンク列 (最初のステップのみ)
                if (hasScreenshot)
                {
                    if (i == 0)
                    {
                        var linkCell = new TableCell();
                        var linkPara = linkCell.AppendChild(new Paragraph());
                        var hyperlink = new Hyperlink { Anchor = "screenshot", History = true };
                        var linkRun = hyperlink.AppendChild(new Run());
                        var linkProps = linkRun.AppendChild(new RunProperties());
                        linkProps.AppendChild(new Color { Val = "0000FF" });
                        linkProps.AppendChild(new Underline { Val = UnderlineValues.Single });
                        linkRun.AppendChild(new Text("[画面を表示]"));
                        linkPara.AppendChild(hyperlink);
                        stepRow.AppendChild(linkCell);
                    }
                    else
                    {
                        stepRow.AppendChild(new TableCell());
                    }
                }
                
                table.AppendChild(stepRow);
                stepNumber++;
            }
        }
        
        return table;
    }
    
    private static void AddHeaderCell(TableRow row, string text)
    {
        var cell = new TableCell();
        var cellProps = cell.AppendChild(new TableCellProperties());
        cellProps.AppendChild(new Shading { Val = ShadingPatternValues.Clear, Fill = "D3D3D3" });
        
        var para = cell.AppendChild(new Paragraph());
        var paraProps = para.InsertAt(new ParagraphProperties(), 0);
        paraProps.AppendChild(new Justification { Val = JustificationValues.Center });
        
        var run = para.AppendChild(new Run());
        var runProps = run.AppendChild(new RunProperties());
        runProps.AppendChild(new Bold());
        run.AppendChild(new Text(text));
        
        row.AppendChild(cell);
    }
    
    private static void AddImage(MainDocumentPart mainPart, Body body, string imagePath)
    {
        try
        {
            var imagePart = mainPart.AddImagePart(ImagePartType.Png);
            
            using (var stream = new FileStream(imagePath, FileMode.Open))
            {
                imagePart.FeedData(stream);
            }
            
            var paragraph = body.AppendChild(new Paragraph());
            var run = paragraph.AppendChild(new Run());
            
            // 画像のサイズを設定 (EMUs単位)
            var widthEmus = 5000000L;  // 約13cm
            var heightEmus = 3000000L; // 約8cm
            
            var drawing = new Drawing(
                new DW.Inline(
                    new DW.Extent { Cx = widthEmus, Cy = heightEmus },
                    new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                    new DW.DocProperties { Id = 1U, Name = "Screenshot" },
                    new DW.NonVisualGraphicFrameDrawingProperties(
                        new A.GraphicFrameLocks { NoChangeAspect = true }),
                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties { Id = 0U, Name = "Screenshot.png" },
                                    new PIC.NonVisualPictureDrawingProperties()),
                                new PIC.BlipFill(
                                    new A.Blip { Embed = mainPart.GetIdOfPart(imagePart) },
                                    new A.Stretch(new A.FillRectangle())),
                                new PIC.ShapeProperties(
                                    new A.Transform2D(
                                        new A.Offset { X = 0L, Y = 0L },
                                        new A.Extents { Cx = widthEmus, Cy = heightEmus }),
                                    new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle }))
                        )
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" })
                )
                {
                    DistanceFromTop = 0U,
                    DistanceFromBottom = 0U,
                    DistanceFromLeft = 0U,
                    DistanceFromRight = 0U
                });
            
            run.AppendChild(drawing);
        }
        catch (Exception ex)
        {
            AddParagraph(body, $"スクリーンショット読込エラー: {ex.Message}");
        }
    }
    
    private class ScenarioInfo
    {
        public string Name { get; set; } = "";
        public List<string> Steps { get; set; } = new List<string>();
    }
}
