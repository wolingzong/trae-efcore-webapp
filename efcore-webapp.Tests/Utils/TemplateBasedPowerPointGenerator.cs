using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;

namespace EfCoreWebApp.Tests.Utils;

public static class TemplateBasedPowerPointGenerator
{
    public static void GenerateTestReport(string pptPath, string featureFilePath, string screenshotPath, string testResult = "PASS")
    {
        // For now, create a minimal but valid PPTX that WPS can open
        // We'll use the simplest possible OpenXML structure
        
        var featureContent = File.ReadAllText(featureFilePath);
        var scenarios = ParseFeatureFile(featureContent);
        
        using var presentationDocument = PresentationDocument.Create(pptPath, PresentationDocumentType.Presentation);
        var presentationPart = presentationDocument.AddPresentationPart();
        
        // Create the absolute minimum structure that WPS accepts
        CreateMinimalPresentation(presentationPart, scenarios, testResult, screenshotPath);
        
        presentationPart.Presentation.Save();
    }
    
    private static void CreateMinimalPresentation(PresentationPart presentationPart, List<ScenarioInfo> scenarios, string testResult, string screenshotPath)
    {
        presentationPart.Presentation = new Presentation(
            new SlideIdList(),
            new SlideSize { Cx = 9144000, Cy = 6858000 }
        );
        
        var slideIdList = presentationPart.Presentation.SlideIdList!;
        uint slideId = 256U;
        
        // Create 4 simple slides without master/layout (WPS minimal mode)
        CreateSimpleSlide(presentationPart, slideIdList, ref slideId, 
            $"商品管理システム\nテスト実行報告書\n\n{(testResult == "PASS" ? "✓ テスト合格" : "✗ テスト不合格")}\n{DateTime.Now:yyyy年MM月dd日}");
        
        CreateSimpleSlide(presentationPart, slideIdList, ref slideId,
            $"テスト結果サマリー\n\n総合結果: {(testResult == "PASS" ? "合格" : "不合格")}\n総シナリオ数: {scenarios.Count}\n総ステップ数: {scenarios.Sum(s => s.Steps.Count)}\n\nテスト環境: GitHub Actions\n.NET: 9.0");
        
        if (File.Exists(screenshotPath))
        {
            CreateImageSlide(presentationPart, slideIdList, ref slideId, "スクリーンショット", screenshotPath);
        }
        
        CreateSimpleSlide(presentationPart, slideIdList, ref slideId,
            "システム情報\n\n・テスト環境: GitHub Actions (Ubuntu)\n・.NETバージョン: 9.0\n・データベース: SQL Server / SQLite\n・ブラウザ: Chromium\n・実行時間: 約2-3分");
    }
    
    private static void CreateSimpleSlide(PresentationPart presentationPart, SlideIdList slideIdList, ref uint slideId, string text)
    {
        var slidePart = presentationPart.AddNewPart<SlidePart>();
        slidePart.Slide = new Slide(
            new CommonSlideData(
                new ShapeTree(
                    new P.NonVisualGroupShapeProperties(
                        new P.NonVisualDrawingProperties { Id = 1U, Name = "" },
                        new P.NonVisualGroupShapeDrawingProperties(),
                        new ApplicationNonVisualDrawingProperties()),
                    new GroupShapeProperties(),
                    // Add a text shape
                    new P.Shape(
                        new P.NonVisualShapeProperties(
                            new P.NonVisualDrawingProperties { Id = 2U, Name = "Title" },
                            new P.NonVisualShapeDrawingProperties(new A.ShapeLocks { NoGrouping = true }),
                            new ApplicationNonVisualDrawingProperties()),
                        new P.ShapeProperties(
                            new A.Transform2D(
                                new A.Offset { X = 457200L, Y = 274638L },
                                new A.Extents { Cx = 8229600L, Cy = 6086862L })),
                        new P.TextBody(
                            new A.BodyProperties(),
                            new A.ListStyle(),
                            new A.Paragraph(
                                new A.Run(
                                    new A.RunProperties { Language = "ja-JP", FontSize = 2400 },
                                    new A.Text(text)
                                )
                            )
                        )
                    )
                )
            )
        );
        
        slideIdList.AppendChild(new SlideId { Id = slideId++, RelationshipId = presentationPart.GetIdOfPart(slidePart) });
        slidePart.Slide.Save();
    }
    
    private static void CreateImageSlide(PresentationPart presentationPart, SlideIdList slideIdList, ref uint slideId, string title, string imagePath)
    {
        var slidePart = presentationPart.AddNewPart<SlidePart>();
        
        var shapeTree = new ShapeTree(
            new P.NonVisualGroupShapeProperties(
                new P.NonVisualDrawingProperties { Id = 1U, Name = "" },
                new P.NonVisualGroupShapeDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()),
            new GroupShapeProperties()
        );
        
        // Add title
        shapeTree.AppendChild(new P.Shape(
            new P.NonVisualShapeProperties(
                new P.NonVisualDrawingProperties { Id = 2U, Name = "Title" },
                new P.NonVisualShapeDrawingProperties(new A.ShapeLocks { NoGrouping = true }),
                new ApplicationNonVisualDrawingProperties()),
            new P.ShapeProperties(
                new A.Transform2D(
                    new A.Offset { X = 457200L, Y = 274638L },
                    new A.Extents { Cx = 8229600L, Cy = 1143000L })),
            new P.TextBody(
                new A.BodyProperties(),
                new A.ListStyle(),
                new A.Paragraph(
                    new A.Run(
                        new A.RunProperties { Language = "ja-JP", FontSize = 2800 },
                        new A.Text(title)
                    )
                )
            )
        ));
        
        // Add image
        try
        {
            var imagePart = slidePart.AddImagePart(ImagePartType.Png);
            using (var stream = new FileStream(imagePath, FileMode.Open))
            {
                imagePart.FeedData(stream);
            }
            
            shapeTree.AppendChild(new P.Picture(
                new P.NonVisualPictureProperties(
                    new P.NonVisualDrawingProperties { Id = 3U, Name = "Picture" },
                    new P.NonVisualPictureDrawingProperties(new A.PictureLocks { NoChangeAspect = false }),
                    new ApplicationNonVisualDrawingProperties()),
                new P.BlipFill(
                    new A.Blip { Embed = slidePart.GetIdOfPart(imagePart) },
                    new A.Stretch(new A.FillRectangle())),
                new P.ShapeProperties(
                    new A.Transform2D(
                        new A.Offset { X = 1000000L, Y = 2000000L },
                        new A.Extents { Cx = 8000000L, Cy = 4500000L }),
                    new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })
            ));
        }
        catch
        {
            // If image fails, just skip it
        }
        
        slidePart.Slide = new Slide(new CommonSlideData(shapeTree));
        slideIdList.AppendChild(new SlideId { Id = slideId++, RelationshipId = presentationPart.GetIdOfPart(slidePart) });
        slidePart.Slide.Save();
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
    
    private class ScenarioInfo
    {
        public string Name { get; set; } = "";
        public List<string> Steps { get; set; } = new List<string>();
    }
}
