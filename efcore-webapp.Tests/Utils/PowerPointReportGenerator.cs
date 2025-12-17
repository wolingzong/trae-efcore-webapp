using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace EfCoreWebApp.Tests.Utils;

public static class PowerPointReportGenerator
{
    public static void GenerateTestReport(string pptPath, string featureFilePath, string screenshotPath, string testResult = "PASS")
    {
        var featureContent = File.ReadAllText(featureFilePath);
        var scenarios = ParseFeatureFile(featureContent);
        
        using var presentationDocument = PresentationDocument.Create(pptPath, PresentationDocumentType.Presentation);
        var presentationPart = presentationDocument.AddPresentationPart();
        presentationPart.Presentation = new Presentation();
        
        // Create slide master and layout parts
        var slideMasterPart = presentationPart.AddNewPart<SlideMasterPart>();
        var slideLayoutPart = slideMasterPart.AddNewPart<SlideLayoutPart>();
        
        // Initialize slide master with comprehensive structure for WPS compatibility
        var slideMasterTree = new ShapeTree(
            new P.NonVisualGroupShapeProperties(
                new P.NonVisualDrawingProperties { Id = 1U, Name = "" },
                new P.NonVisualGroupShapeDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()),
            new GroupShapeProperties(new A.TransformGroup(
                new A.Offset { X = 0L, Y = 0L },
                new A.Extents { Cx = 0L, Cy = 0L },
                new A.ChildOffset { X = 0L, Y = 0L },
                new A.ChildExtents { Cx = 0L, Cy = 0L }
            ))
        );
        
        // Add textStyles to slide master (required by WPS)
        var textStyles = new P.TextStyles(
            new P.TitleStyle(
                new A.Level1ParagraphProperties {
                    Alignment = A.TextAlignmentTypeValues.Center
                }
            ),
            new P.BodyStyle(
                new A.Level1ParagraphProperties {
                    LeftMargin = 342900,
                    Indent = -342900,
                    Alignment = A.TextAlignmentTypeValues.Left
                }
            ),
            new P.OtherStyle(
                new A.Level1ParagraphProperties {
                    Alignment = A.TextAlignmentTypeValues.Left
                }
            )
        );
        
        slideMasterPart.SlideMaster = new SlideMaster(
            new CommonSlideData(slideMasterTree),
            new P.ColorMap { 
                Background1 = A.ColorSchemeIndexValues.Light1,
                Text1 = A.ColorSchemeIndexValues.Dark1,
                Background2 = A.ColorSchemeIndexValues.Light2,
                Text2 = A.ColorSchemeIndexValues.Dark2,
                Accent1 = A.ColorSchemeIndexValues.Accent1,
                Accent2 = A.ColorSchemeIndexValues.Accent2,
                Accent3 = A.ColorSchemeIndexValues.Accent3,
                Accent4 = A.ColorSchemeIndexValues.Accent4,
                Accent5 = A.ColorSchemeIndexValues.Accent5,
                Accent6 = A.ColorSchemeIndexValues.Accent6,
                Hyperlink = A.ColorSchemeIndexValues.Hyperlink,
                FollowedHyperlink = A.ColorSchemeIndexValues.FollowedHyperlink
            },
            new P.SlideLayoutIdList(
                new P.SlideLayoutId { Id = 2147483649U, RelationshipId = "rId1" }
            ),
            textStyles
        );
        
        slideMasterPart.AddPart(slideLayoutPart);
        
        // Initialize slide layout
        slideLayoutPart.SlideLayout = new SlideLayout(
            new CommonSlideData(new ShapeTree(
                new P.NonVisualGroupShapeProperties(
                    new P.NonVisualDrawingProperties { Id = 1U, Name = "" },
                    new P.NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(new A.TransformGroup())
            )),
            new P.ColorMapOverride(new A.MasterColorMapping()));
        
        var slideIdList = new SlideIdList();
        var slideMasterIdList = new SlideMasterIdList(
            new SlideMasterId { Id = 2147483648U, RelationshipId = presentationPart.GetIdOfPart(slideMasterPart) });
        
        // Add default text style (required by WPS and some PowerPoint versions)
        var defaultTextStyle = new P.DefaultTextStyle(
            new A.DefaultParagraphProperties(),
            new A.Level1ParagraphProperties {
                Alignment = A.TextAlignmentTypeValues.Left,
                DefaultTabSize = 914400,
                RightToLeft = false,
                EastAsianLineBreak = true,
                LatinLineBreak = false,
                Height = true
            });
        
        presentationPart.Presentation.SlideMasterIdList = slideMasterIdList;
        presentationPart.Presentation.SlideIdList = slideIdList;
        presentationPart.Presentation.SlideSize = new SlideSize { Cx = 9144000, Cy = 6858000 };
        presentationPart.Presentation.NotesSize = new NotesSize { Cx = 6858000, Cy = 9144000 };
        presentationPart.Presentation.DefaultTextStyle = defaultTextStyle;
        
        uint slideId = 256U;
        
        // スライド1: タイトルスライド
        CreateTitleSlide(presentationPart, slideLayoutPart, slideIdList, ref slideId, scenarios, testResult);
        
        // スライド2: テスト結果サマリー
        CreateSummarySlide(presentationPart, slideLayoutPart, slideIdList, ref slideId, scenarios, testResult);
        
        // スライド3: スクリーンショット
        if (File.Exists(screenshotPath))
        {
            CreateScreenshotSlide(presentationPart, slideLayoutPart, slideIdList, ref slideId, screenshotPath);
        }
        
        // スライド4: システム情報
        CreateSystemInfoSlide(presentationPart, slideLayoutPart, slideIdList, ref slideId);
        
        presentationPart.Presentation.Save();
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
    
    private static void CreateTitleSlide(PresentationPart presentationPart, SlideLayoutPart slideLayoutPart, SlideIdList slideIdList, ref uint slideId, List<ScenarioInfo> scenarios, string testResult)
    {
        var slidePart = presentationPart.AddNewPart<SlidePart>();
        slidePart.Slide = new Slide(new CommonSlideData(new ShapeTree()));
        slidePart.AddPart(slideLayoutPart);
        
        var slideIdInstance = new SlideId { Id = slideId++, RelationshipId = presentationPart.GetIdOfPart(slidePart) };
        slideIdList.AppendChild(slideIdInstance);
        
        var shapeTree = slidePart.Slide.CommonSlideData.ShapeTree;
        
        // タイトル
        AddTextBox(shapeTree, 1, 
            "商品管理システム", 
            1000000, 1000000, 8000000, 1500000, 
            44, true, "00008B", "center");
        
        // サブタイトル
        AddTextBox(shapeTree, 2, 
            "テスト実行報告書 (PowerPoint版)", 
            1000000, 2800000, 8000000, 800000, 
            32, false, "808080", "center");
        
        // 結果バッジ
        var resultText = testResult == "PASS" ? "✓ テスト合格" : "✗ テスト不合格";
        var resultColor = testResult == "PASS" ? "008000" : "FF0000";
        AddTextBox(shapeTree, 3, 
            resultText, 
            3000000, 4500000, 4000000, 1000000, 
            36, true, resultColor, "center");
        
        // 実行日時
        AddTextBox(shapeTree, 4, 
            $"実行日時: {DateTime.Now:yyyy年MM月dd日 HH:mm:ss}", 
            2000000, 6000000, 6000000, 500000, 
            20, false, "000000", "center");
        
        slidePart.Slide.Save();
    }
    
    private static void CreateSummarySlide(PresentationPart presentationPart, SlideLayoutPart slideLayoutPart, SlideIdList slideIdList, ref uint slideId, List<ScenarioInfo> scenarios, string testResult)
    {
        var slidePart = presentationPart.AddNewPart<SlidePart>();
        slidePart.Slide = new Slide(new CommonSlideData(new ShapeTree()));
        slidePart.AddPart(slideLayoutPart);
        
        var slideIdInstance = new SlideId { Id = slideId++, RelationshipId = presentationPart.GetIdOfPart(slidePart) };
        slideIdList.AppendChild(slideIdInstance);
        
        var shapeTree = slidePart.Slide.CommonSlideData.ShapeTree;
        
        // タイトル
        AddTextBox(shapeTree, 1, 
            "テスト結果サマリー", 
            500000, 500000, 9000000, 1000000, 
            36, true, "00008B", "left");
        
        // サマリー情報
        var summaryText = $@"実行情報

総合結果: {(testResult == "PASS" ? "✓ 合格" : "✗ 不合格")}
総シナリオ数: {scenarios.Count}
総ステップ数: {scenarios.Sum(s => s.Steps.Count)}
成功率: {(testResult == "PASS" ? "100%" : "0%")}

テスト環境: GitHub Actions
.NETバージョン: 9.0
データベース: SQL Server / SQLite";
        
        AddTextBox(shapeTree, 2, 
            summaryText, 
            800000, 2000000, 8400000, 5000000, 
            20, false, "000000", "left");
        
        slidePart.Slide.Save();
    }
    
    private static void CreateScreenshotSlide(PresentationPart presentationPart, SlideLayoutPart slideLayoutPart, SlideIdList slideIdList, ref uint slideId, string screenshotPath)
    {
        var slidePart = presentationPart.AddNewPart<SlidePart>();
        slidePart.Slide = new Slide(new CommonSlideData(new ShapeTree()));
        slidePart.AddPart(slideLayoutPart);
        
        var slideIdInstance = new SlideId { Id = slideId++, RelationshipId = presentationPart.GetIdOfPart(slidePart) };
        slideIdList.AppendChild(slideIdInstance);
        
        var shapeTree = slidePart.Slide.CommonSlideData.ShapeTree;
        
        // タイトル
        AddTextBox(shapeTree, 1, 
            "テスト実行スクリーンショット", 
            500000, 500000, 9000000, 1000000, 
            36, true, "00008B", "left");
        
        // スクリーンショット情報
        AddTextBox(shapeTree, 2, 
            $"キャプチャ日時: {DateTime.Now:yyyy年MM月dd日 HH:mm:ss}\nページ: 商品一覧 (http://localhost:5000/products)", 
            800000, 1600000, 8400000, 600000, 
            16, false, "000000", "left");
        
        // 画像を追加
        try
        {
            var imagePart = slidePart.AddImagePart(ImagePartType.Png);
            using (var stream = new FileStream(screenshotPath, FileMode.Open))
            {
                imagePart.FeedData(stream);
            }
            
            AddImage(shapeTree, 3, slidePart.GetIdOfPart(imagePart), 1000000, 2500000, 8000000, 4500000);
        }
        catch (Exception ex)
        {
            AddTextBox(shapeTree, 3, 
                $"スクリーンショット読込エラー: {ex.Message}", 
                1000000, 3000000, 8000000, 1000000, 
                16, false, "FF0000", "left");
        }
        
        slidePart.Slide.Save();
    }
    
    private static void CreateSystemInfoSlide(PresentationPart presentationPart, SlideLayoutPart slideLayoutPart, SlideIdList slideIdList, ref uint slideId)
    {
        var slidePart = presentationPart.AddNewPart<SlidePart>();
        slidePart.Slide = new Slide(new CommonSlideData(new ShapeTree()));
        slidePart.AddPart(slideLayoutPart);
        
        var slideIdInstance = new SlideId { Id = slideId++, RelationshipId = presentationPart.GetIdOfPart(slidePart) };
        slideIdList.AppendChild(slideIdInstance);
        
        var shapeTree = slidePart.Slide.CommonSlideData.ShapeTree;
        
        // タイトル
        AddTextBox(shapeTree, 1, 
            "システム情報", 
            500000, 500000, 9000000, 1000000, 
            36, true, "00008B", "left");
        
        // システム情報
        var systemInfoText = @"テスト環境詳細

• テスト環境: GitHub Actions (Ubuntu)
• .NETバージョン: 9.0
• データベース: SQL Server / SQLite
• ブラウザ: Chromium (ヘッドレス)
• 実行時間: 約2-3分

テストフレームワーク
• xUnit.net 2.7.1
• PuppeteerSharp 19.0.0 (ブラウザ自動化)
• EPPlus 7.0.0 (Excel生成)
• iText7 8.0.2 (PDF生成)
• DocumentFormat.OpenXml 3.0.0 (Word/PPT生成)";
        
        AddTextBox(shapeTree, 2, 
            systemInfoText, 
            800000, 1800000, 8400000, 5200000, 
            18, false, "000000", "left");
        
        slidePart.Slide.Save();
    }
    
    private static void AddTextBox(ShapeTree shapeTree, uint id, string text, long x, long y, long width, long height, int fontSize, bool bold, string color, string align)
    {
        var shape = new P.Shape();
        
        var nvSpPr = new P.NonVisualShapeProperties(
            new P.NonVisualDrawingProperties { Id = id, Name = $"TextBox{id}" },
            new P.NonVisualShapeDrawingProperties(new A.ShapeLocks { NoGrouping = true }),
            new ApplicationNonVisualDrawingProperties(new PlaceholderShape()));
        
        var spPr = new P.ShapeProperties(
            new A.Transform2D(
                new A.Offset { X = x, Y = y },
                new A.Extents { Cx = width, Cy = height }));
        
        var txBody = new P.TextBody(
            new A.BodyProperties(),
            new A.ListStyle());
        
        var paragraph = new A.Paragraph();
        
        var paragraphProperties = new A.ParagraphProperties();
        if (align == "center")
        {
            paragraphProperties.Alignment = A.TextAlignmentTypeValues.Center;
        }
        paragraph.AppendChild(paragraphProperties);
        
        var run = new A.Run();
        var runProperties = new A.RunProperties { Language = "ja-JP", FontSize = fontSize * 100 };
        
        if (bold)
        {
            runProperties.Bold = true;
        }
        
        runProperties.AppendChild(new A.SolidFill(new A.RgbColorModelHex { Val = color }));
        
        run.AppendChild(runProperties);
        run.AppendChild(new A.Text(text));
        
        paragraph.AppendChild(run);
        txBody.AppendChild(paragraph);
        
        shape.AppendChild(nvSpPr);
        shape.AppendChild(spPr);
        shape.AppendChild(txBody);
        
        shapeTree.AppendChild(shape);
    }
    
    private static void AddImage(ShapeTree shapeTree, uint id, string relationshipId, long x, long y, long width, long height)
    {
        var picture = new P.Picture();
        
        var nvPicPr = new P.NonVisualPictureProperties(
            new P.NonVisualDrawingProperties { Id = id, Name = $"Picture{id}" },
            new P.NonVisualPictureDrawingProperties(new A.PictureLocks { NoChangeAspect = false }),
            new ApplicationNonVisualDrawingProperties());
        
        var blipFill = new P.BlipFill(
            new A.Blip { Embed = relationshipId },
            new A.Stretch(new A.FillRectangle()));
        
        var spPr = new P.ShapeProperties(
            new A.Transform2D(
                new A.Offset { X = x, Y = y },
                new A.Extents { Cx = width, Cy = height }),
            new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle });
        
        picture.AppendChild(nvPicPr);
        picture.AppendChild(blipFill);
        picture.AppendChild(spPr);
        
        shapeTree.AppendChild(picture);
    }
    
    private class ScenarioInfo
    {
        public string Name { get; set; } = "";
        public List<string> Steps { get; set; } = new List<string>();
    }
}
