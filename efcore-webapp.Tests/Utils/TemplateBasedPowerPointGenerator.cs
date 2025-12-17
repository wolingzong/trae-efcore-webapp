using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml;
using A = DocumentFormat.OpenXml.Drawing;
using P = DocumentFormat.OpenXml.Presentation;
using System.IO.Compression;

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
        presentationDocument.Dispose(); // Close document before accessing as zip
        
        // Post-process to fix relationships for WPS compatibility
        FixPowerPointRelationships(pptPath);
    }

    private static void FixPowerPointRelationships(string pptPath)
    {
        // WPS Office compatibility fix: Ensure relationships use relative paths
        // OpenXML SDK tends to generate absolute paths (Target="/ppt/slides/slide1.xml")
        // which WPS Presentation fails to resolve correctly in some cases.
        // We manually rewrite them to be relative (Target="slides/slide1.xml").
        
        try 
        {
            using var archive = ZipFile.Open(pptPath, ZipArchiveMode.Update);
            var entry = archive.GetEntry("ppt/_rels/presentation.xml.rels");
            if (entry != null)
            {
                string content;
                using (var stream = entry.Open())
                using (var reader = new StreamReader(stream))
                {
                    content = reader.ReadToEnd();
                }

                // Replace absolute paths starting with /ppt/ with relative paths
                if (content.Contains("Target=\"/ppt/"))
                {
                    content = content.Replace("Target=\"/ppt/", "Target=\"");
                    
                    entry.Delete();
                    var newEntry = archive.CreateEntry("ppt/_rels/presentation.xml.rels");
                    using (var stream = newEntry.Open())
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(content);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to apply WPS compatibility fix: {ex.Message}");
        }
    }
    
    private static void CreateMinimalPresentation(PresentationPart presentationPart, List<ScenarioInfo> scenarios, string testResult, string screenshotPath)
    {
        // 1. Create Slide Master, Layout, and Theme (Required by WPS)
        var slideMasterPart = presentationPart.AddNewPart<SlideMasterPart>();
        CreateTheme(slideMasterPart);
        var slideLayoutPart = slideMasterPart.AddNewPart<SlideLayoutPart>();
        
        // Setup Master content
        slideMasterPart.SlideMaster = new SlideMaster(
            new CommonSlideData(new ShapeTree(
                new P.NonVisualGroupShapeProperties(
                    new P.NonVisualDrawingProperties { Id = 1U, Name = "" },
                    new P.NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(new A.TransformGroup())
            )),
            new P.ColorMap { Background1 = A.ColorSchemeIndexValues.Light1, Text1 = A.ColorSchemeIndexValues.Dark1, Background2 = A.ColorSchemeIndexValues.Light2, Text2 = A.ColorSchemeIndexValues.Dark2, Accent1 = A.ColorSchemeIndexValues.Accent1, Accent2 = A.ColorSchemeIndexValues.Accent2, Accent3 = A.ColorSchemeIndexValues.Accent3, Accent4 = A.ColorSchemeIndexValues.Accent4, Accent5 = A.ColorSchemeIndexValues.Accent5, Accent6 = A.ColorSchemeIndexValues.Accent6, Hyperlink = A.ColorSchemeIndexValues.Hyperlink, FollowedHyperlink = A.ColorSchemeIndexValues.FollowedHyperlink },
            new P.SlideLayoutIdList(new P.SlideLayoutId { Id = 2147483649U, RelationshipId = slideMasterPart.GetIdOfPart(slideLayoutPart) }),
            new P.TextStyles(
                new P.TitleStyle(), new P.BodyStyle(), new P.OtherStyle()
            )
        );

        // Setup Layout content
        slideLayoutPart.SlideLayout = new SlideLayout(
            new CommonSlideData(new ShapeTree(
                new P.NonVisualGroupShapeProperties(
                    new P.NonVisualDrawingProperties { Id = 1U, Name = "" },
                    new P.NonVisualGroupShapeDrawingProperties(),
                    new ApplicationNonVisualDrawingProperties()),
                new GroupShapeProperties(new A.TransformGroup())
            )),
            new P.ColorMapOverride(new A.MasterColorMapping()));

        // 2. Setup Presentation with Master Reference
        presentationPart.Presentation = new Presentation(
            new SlideMasterIdList(new SlideMasterId { Id = 2147483648U, RelationshipId = presentationPart.GetIdOfPart(slideMasterPart) }),
            new SlideIdList(),
            new SlideSize { Cx = 9144000, Cy = 6858000 },
            new NotesSize { Cx = 6858000, Cy = 6858000 },
            new P.DefaultTextStyle(
                new A.DefaultParagraphProperties(),
                new A.Level1ParagraphProperties { Alignment = A.TextAlignmentTypeValues.Left, DefaultTabSize = 914400, RightToLeft = false, EastAsianLineBreak = true, LatinLineBreak = false, Height = true },
                new A.Level2ParagraphProperties { Alignment = A.TextAlignmentTypeValues.Left, DefaultTabSize = 914400, RightToLeft = false, EastAsianLineBreak = true, LatinLineBreak = false, Height = true },
                new A.Level3ParagraphProperties { Alignment = A.TextAlignmentTypeValues.Left, DefaultTabSize = 914400, RightToLeft = false, EastAsianLineBreak = true, LatinLineBreak = false, Height = true },
                new A.Level4ParagraphProperties { Alignment = A.TextAlignmentTypeValues.Left, DefaultTabSize = 914400, RightToLeft = false, EastAsianLineBreak = true, LatinLineBreak = false, Height = true },
                new A.Level5ParagraphProperties { Alignment = A.TextAlignmentTypeValues.Left, DefaultTabSize = 914400, RightToLeft = false, EastAsianLineBreak = true, LatinLineBreak = false, Height = true },
                new A.Level6ParagraphProperties { Alignment = A.TextAlignmentTypeValues.Left, DefaultTabSize = 914400, RightToLeft = false, EastAsianLineBreak = true, LatinLineBreak = false, Height = true },
                new A.Level7ParagraphProperties { Alignment = A.TextAlignmentTypeValues.Left, DefaultTabSize = 914400, RightToLeft = false, EastAsianLineBreak = true, LatinLineBreak = false, Height = true },
                new A.Level8ParagraphProperties { Alignment = A.TextAlignmentTypeValues.Left, DefaultTabSize = 914400, RightToLeft = false, EastAsianLineBreak = true, LatinLineBreak = false, Height = true },
                new A.Level9ParagraphProperties { Alignment = A.TextAlignmentTypeValues.Left, DefaultTabSize = 914400, RightToLeft = false, EastAsianLineBreak = true, LatinLineBreak = false, Height = true }
            )
        );
        
        var slideIdList = presentationPart.Presentation.SlideIdList!;
        uint slideId = 256U;
        
        // --- 3. Pre-create Screenshot Slide Part if needed (to allow linking) ---
        SlidePart? screenshotSlidePart = null;
        if (File.Exists(screenshotPath))
        {
            screenshotSlidePart = presentationPart.AddNewPart<SlidePart>();
            CreateImageSlideContent(presentationPart, screenshotSlidePart, slideLayoutPart, "スクリーンショット", screenshotPath);
        }

        // --- 4. Create Slides Sequence ---
        
        // 4.1 Title Slide
        CreateSimpleSlide(presentationPart, slideLayoutPart, slideIdList, ref slideId, 
            $"商品管理システム\nテスト実行報告書\n\n{(testResult == "PASS" ? "✓ テスト合格" : "✗ テスト不合格")}\n{DateTime.Now:yyyy年MM月dd日}");
        
        // 4.2 Summary Slide
        CreateSimpleSlide(presentationPart, slideLayoutPart, slideIdList, ref slideId,
            $"テスト結果サマリー\n\n総合結果: {(testResult == "PASS" ? "合格" : "不合格")}\n総シナリオ数: {scenarios.Count}\n総ステップ数: {scenarios.Sum(s => s.Steps.Count)}\n\nテスト環境: GitHub Actions\n.NET: 9.0");
        
        // 4.3 Detail Slides (Iterate Scenarios)
        foreach (var scenario in scenarios)
        {
           CreateScenarioDetailSlide(presentationPart, slideLayoutPart, slideIdList, ref slideId, scenario, screenshotSlidePart);
        }

        // 4.4 Screenshot Slide (Append to ID list now)
        if (screenshotSlidePart != null)
        {
             slideIdList.AppendChild(new SlideId { Id = slideId++, RelationshipId = presentationPart.GetIdOfPart(screenshotSlidePart) });
        }
        
        // 4.5 System Info Slide
        CreateSimpleSlide(presentationPart, slideLayoutPart, slideIdList, ref slideId,
            "システム情報\n\n・テスト環境: GitHub Actions (Ubuntu)\n・.NETバージョン: 9.0\n・データベース: SQL Server / SQLite\n・ブラウザ: Chromium\n・実行時間: 約2-3分");
    }

    private static void CreateTheme(SlideMasterPart slideMasterPart)
    {
        ThemePart themePart = slideMasterPart.AddNewPart<ThemePart>();
        themePart.Theme = new A.Theme(
            new A.ThemeElements(
                new A.ColorScheme(
                    new A.Dark1Color(new A.SystemColor { Val = A.SystemColorValues.WindowText, LastColor = "000000" }),
                    new A.Light1Color(new A.SystemColor { Val = A.SystemColorValues.Window, LastColor = "FFFFFF" }),
                    new A.Dark2Color(new A.RgbColorModelHex { Val = "1F497D" }),
                    new A.Light2Color(new A.RgbColorModelHex { Val = "EEECE1" }),
                    new A.Accent1Color(new A.RgbColorModelHex { Val = "4F81BD" }),
                    new A.Accent2Color(new A.RgbColorModelHex { Val = "C0504D" }),
                    new A.Accent3Color(new A.RgbColorModelHex { Val = "9BBB59" }),
                    new A.Accent4Color(new A.RgbColorModelHex { Val = "8064A2" }),
                    new A.Accent5Color(new A.RgbColorModelHex { Val = "4BACC6" }),
                    new A.Accent6Color(new A.RgbColorModelHex { Val = "F79646" }),
                    new A.Hyperlink(new A.RgbColorModelHex { Val = "0000FF" }),
                    new A.FollowedHyperlinkColor(new A.RgbColorModelHex { Val = "800080" }))
                { Name = "Office" },
                new A.FontScheme(
                    new A.MajorFont(
                        new A.LatinFont { Typeface = "Calibri" },
                        new A.EastAsianFont { Typeface = "" },
                        new A.ComplexScriptFont { Typeface = "" }),
                    new A.MinorFont(
                        new A.LatinFont { Typeface = "Calibri" },
                        new A.EastAsianFont { Typeface = "" },
                        new A.ComplexScriptFont { Typeface = "" })
                )
                { Name = "Office" },
                new A.FormatScheme(
                    new A.FillStyleList(new A.SolidFill(new A.SchemeColor { Val = A.SchemeColorValues.PhColor })),
                    new A.LineStyleList(new A.Outline(new A.SolidFill(new A.SchemeColor { Val = A.SchemeColorValues.PhColor }))),
                    new A.EffectStyleList(new A.EffectStyle(new A.EffectList()))
                )
                { Name = "Office" }
            ),
            new A.ObjectDefaults(),
            new A.ExtraColorSchemeList()
        ) { Name = "Office Theme" };
    }
    
    private static void CreateSimpleSlide(PresentationPart presentationPart, SlideLayoutPart layoutPart, SlideIdList slideIdList, ref uint slideId, string text)
    {
        var slidePart = presentationPart.AddNewPart<SlidePart>();
        slidePart.AddPart(layoutPart); // Critical: Link to Layout
        
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
            ),
            new P.ColorMapOverride(new A.MasterColorMapping())
        );
        
        slideIdList.AppendChild(new SlideId { Id = slideId++, RelationshipId = presentationPart.GetIdOfPart(slidePart) });
        slidePart.Slide.Save();
    }
    
    private static void CreateScenarioDetailSlide(PresentationPart presentationPart, SlideLayoutPart layoutPart, SlideIdList slideIdList, ref uint slideId, ScenarioInfo scenario, SlidePart? screenshotPart)
    {
        var slidePart = presentationPart.AddNewPart<SlidePart>();
        slidePart.AddPart(layoutPart);
        
        var shapeTree = new ShapeTree(
            new P.NonVisualGroupShapeProperties(
                new P.NonVisualDrawingProperties { Id = 1U, Name = "" },
                new P.NonVisualGroupShapeDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()),
            new GroupShapeProperties());

        // Title
        AddTextBox(shapeTree, 2, $"シナリオ: {scenario.Name}", 457200, 274638, 8229600, 1143000, 2800, true);

        // Steps Body
        long yOffset = 1600000;
        foreach (var step in scenario.Steps)
        {
            // Add step text
            AddTextBox(shapeTree, 3, step, 457200, yOffset, 6000000, 400000, 1800, false);
            
            // Add "[View Screenshot]" link if screenshot exists
            if (screenshotPart != null)
            {
                // Create relationship
                var relId = slidePart.AddHyperlinkRelationship(new Uri( "#" + screenshotPart.Uri.ToString().Split('/').Last(), UriKind.Relative), true).Id;
                 
                // Note: Internal linking in OpenXML is complex. 
                // We'll use a standardized method similar to how Word links operate but for PPT.
                // Or simpler: Just a text box with a click action.
                
                var shape = new P.Shape(
                    new P.NonVisualShapeProperties(
                        new P.NonVisualDrawingProperties { Id = 4U, Name = "Link" },
                        new P.NonVisualShapeDrawingProperties(new A.ShapeLocks { NoGrouping = true }),
                        new ApplicationNonVisualDrawingProperties()),
                    new P.ShapeProperties(
                        new A.Transform2D(
                            new A.Offset { X = 7000000L, Y = yOffset },
                            new A.Extents { Cx = 1500000L, Cy = 400000L }),
                        new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle },
                        new A.SolidFill(new A.RgbColorModelHex { Val = "FFFFFF" }), // White background
                         new A.Outline(new A.NoFill())
                    ),
                    new P.TextBody(
                        new A.BodyProperties(),
                        new A.ListStyle(),
                        new A.Paragraph(
                            new A.Run(
                                new A.RunProperties(
                                    new A.SolidFill(new A.RgbColorModelHex { Val = "0000FF" })
                                ) { Language = "ja-JP", FontSize = 1600, Underline = A.TextUnderlineValues.Single },
                                new A.Text("[画面を表示]"),
                                new A.HyperlinkOnClick { Id = relId, Action = "ppaction://hlinksldjump" }
                            )
                        )
                    )
                );
                shapeTree.AppendChild(shape);
            }
            
            yOffset += 500000;
        }

        slidePart.Slide = new Slide(
            new CommonSlideData(shapeTree),
            new P.ColorMapOverride(new A.MasterColorMapping())
        );
        slideIdList.AppendChild(new SlideId { Id = slideId++, RelationshipId = presentationPart.GetIdOfPart(slidePart) });
        slidePart.Slide.Save();
    }

    private static void CreateImageSlideContent(PresentationPart presentationPart, SlidePart slidePart, SlideLayoutPart layoutPart, string title, string imagePath)
    {
        slidePart.AddPart(layoutPart);
        
        var shapeTree = new ShapeTree(
            new P.NonVisualGroupShapeProperties(
                new P.NonVisualDrawingProperties { Id = 1U, Name = "" },
                new P.NonVisualGroupShapeDrawingProperties(),
                new ApplicationNonVisualDrawingProperties()),
            new GroupShapeProperties()
        );
        
        // Add title
        AddTextBox(shapeTree, 2, title, 457200, 274638, 8229600, 1143000, 2800, true);
        
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
            
            // Add "Back" link (Action: Last Slide Viewed)
            // ppaction://hlinklastslide is the standard action for "Last Slide Viewed"
            var backRelId = slidePart.AddHyperlinkRelationship(new Uri("ppaction://hlinklastslide", UriKind.Absolute), true).Id;
            
            var backShape = new P.Shape(
                new P.NonVisualShapeProperties(
                    new P.NonVisualDrawingProperties { Id = 4U, Name = "BackLink" },
                    new P.NonVisualShapeDrawingProperties(new A.ShapeLocks { NoGrouping = true }),
                    new ApplicationNonVisualDrawingProperties()),
                new P.ShapeProperties(
                    new A.Transform2D(
                        new A.Offset { X = 1000000L, Y = 1000000L }, // Positioned above image
                        new A.Extents { Cx = 3000000L, Cy = 500000L }),
                    new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle },
                    new A.NoFill()
                ),
                new P.TextBody(
                    new A.BodyProperties(),
                    new A.ListStyle(),
                    new A.Paragraph(
                        new A.Run(
                            new A.RunProperties(
                                new A.SolidFill(new A.RgbColorModelHex { Val = "0000FF" })
                            ) { Language = "ja-JP", FontSize = 1600, Underline = A.TextUnderlineValues.Single },
                            new A.Text("← 元の画面に戻る"),
                            new A.HyperlinkOnClick { Id = backRelId, Action = "ppaction://hlinklastslide" }
                        )
                    )
                )
            );
            shapeTree.AppendChild(backShape);
        }
        catch
        {
            // If image fails, just skip it
        }
        
        slidePart.Slide = new Slide(
            new CommonSlideData(shapeTree),
            new P.ColorMapOverride(new A.MasterColorMapping())
        );
        
        slidePart.Slide.Save();
    }
    
    // Helper to replace AddTextBox (since it was missing in snippet or implied)
    private static void AddTextBox(ShapeTree shapeTree, uint id, string text, long x, long y, long cx, long cy, int fontSize, bool isBold, string color = "000000", string align = "left")
    {
         var shape = new P.Shape(
             new P.NonVisualShapeProperties(
                 new P.NonVisualDrawingProperties { Id = id, Name = "TextBox" },
                 new P.NonVisualShapeDrawingProperties(new A.ShapeLocks { NoGrouping = true }),
                 new ApplicationNonVisualDrawingProperties()),
             new P.ShapeProperties(
                 new A.Transform2D(
                     new A.Offset { X = x, Y = y },
                     new A.Extents { Cx = cx, Cy = cy }),
                 new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle },
                 new A.NoFill()
             ),
             new P.TextBody(
                 new A.BodyProperties(),
                 new A.ListStyle(),
                 new A.Paragraph(
                     new A.ParagraphProperties { Alignment = align == "center" ? A.TextAlignmentTypeValues.Center : A.TextAlignmentTypeValues.Left },
                     new A.Run(
                         new A.RunProperties(
                            new A.SolidFill(new A.RgbColorModelHex { Val = color })
                         ) { Language = "ja-JP", FontSize = fontSize, Bold = isBold },
                         new A.Text(text)
                     )
                 )
             )
         );
         shapeTree.AppendChild(shape);
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
