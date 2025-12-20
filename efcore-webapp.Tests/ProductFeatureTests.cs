using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using EfCoreWebApp.Tests.Utils;

namespace EfCoreWebApp.Tests;

public class ProductFeatureTests : IClassFixture<TestServerFixture>
{
    private readonly TestServerFixture _server;

    public ProductFeatureTests(TestServerFixture server)
    {
        _server = server;
    }
    
    [Fact]
    public async Task 商品一覧画面を見る_ヘッダー表示とPDF保存()
    {
        var baseUrl = _server.BaseUrl;
        var client = new HttpClient { BaseAddress = new Uri(baseUrl) };

        var home = await client.GetAsync("/");
        home.EnsureSuccessStatusCode();

        var resp = await client.GetAsync("/products");
        resp.EnsureSuccessStatusCode();
        var html = await resp.Content.ReadAsStringAsync();
        Assert.Contains("<h1>Products List</h1>", html);

        var dir = Path.Combine(Directory.GetCurrentDirectory(), "TestResults", DateTime.Now.ToString("yyyyMMdd"));
        Directory.CreateDirectory(dir);
        
        // 真のブラウザスクリーンショット生成
        var screenshotFile = Path.Combine(dir, "products-screenshot.png");
        await BrowserScreenshot.TakeScreenshotAsync($"{baseUrl}/products", screenshotFile);
        
        // インタラクティブPDF レポート生成 (クリック可能リンク対応)
        var pdfFile = Path.Combine(dir, "acceptance-report.pdf");
        var featureFilePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "Features", "product_management.feature");
        InteractivePdfReport.GenerateTestReport(pdfFile, featureFilePath, screenshotFile, "PASS");
        
        // CSV レポート生成 (Excel互換)
        var featureFile = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "Features", "product_management.feature");
        var csvFile = Path.Combine(dir, "test-report.csv");
        ExcelReport.GenerateFromFeature(featureFile, csvFile, "PASS", screenshotFile);
        
        // 真のExcel形式のファイル生成 (test-specimen.xlsx) - 複数シート対応
        var excelFile = Path.Combine(dir, "test-specimen.xlsx");
        await ExcelReportGenerator.GenerateTestReport(excelFile, featureFile, screenshotFile, "PASS");
        
        // Word レポート生成 (.docx形式)
        var wordFile = Path.Combine(dir, "test-report.docx");
        WordReportGenerator.GenerateTestReport(wordFile, featureFile, screenshotFile, "PASS");
        
        // PowerPoint レポート生成 (.pptx形式) - WPS互換性向上版
        var pptFile = Path.Combine(dir, "test-report.pptx");
        TemplateBasedPowerPointGenerator.GenerateTestReport(pptFile, featureFile, screenshotFile, "PASS");
        
        // Mermaid 階層図生成 (.mmd形式)
        var mermaidFile = Path.Combine(dir, "test-hierarchy.mmd");
        await MermaidDiagramGenerator.GenerateDiagramAsync(mermaidFile, featureFile);
        
        // Visio (Native VDX) 階層図生成
        var vdxFile = Path.Combine(dir, "test-hierarchy.vdx");
        VisioReportGenerator.GenerateDiagram(vdxFile, featureFile);
        
        // Output as VSDX (OpenXML Package)
        var vsdxFile = Path.Combine(dir, "test-hierarchy.vsdx");
        VsdxReportGenerator.GenerateDiagram(vsdxFile, featureFile);

        // Output as Draw.io (mxGraphModel) - Request User Format
        var drawioFile = Path.Combine(dir, "test-hierarchy.drawio");
        DrawIoReportGenerator.GenerateDiagram(drawioFile, featureFile);
        
        // Output XML (Copy of Draw.io as requested)
        var xmlFile = Path.Combine(dir, "test-hierarchy.xml");
        File.Copy(drawioFile, xmlFile, true);
        
        // 動画記録 (Playwright)
        var videoFile = Path.Combine(dir, "acceptance-test.mp4");
        await VideoRecorder.RecordAcceptanceScenarioAsync(baseUrl, videoFile);
    }

    [Fact]
    public void VerifyVsdxIntegrity()
    {
        // Setup
        var featureFile = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "Features", "product_management.feature");
        var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "TestResults", "IntegrityCheck");
        Directory.CreateDirectory(outputDir);
        var vsdxFile = Path.Combine(outputDir, "check.vsdx");
        
        // Generate
        VsdxReportGenerator.GenerateDiagram(vsdxFile, featureFile);
        
        // Verify Size
        var info = new FileInfo(vsdxFile);
        Assert.True(info.Length > 0, $"VSDX file is empty: {vsdxFile}");
        
        // Verify Zip Structure (Attempt Extract)
        var extractDir = Path.Combine(outputDir, $"extracted_{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}");
        
        // Ensure clean extraction directory
        if (Directory.Exists(extractDir))
        {
            try
            {
                Directory.Delete(extractDir, true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Failed to clean extraction directory: {ex.Message}");
            }
        }
        
        try
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(vsdxFile, extractDir);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Failed to unzip generated VSDX: {ex.Message}");
        }
        
        Assert.True(File.Exists(Path.Combine(extractDir, "[Content_Types].xml")), "Content Types missing");
        Assert.True(Directory.Exists(Path.Combine(extractDir, "visio")), "visio folder missing");
    }
}
