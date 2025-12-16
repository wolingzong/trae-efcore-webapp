using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using EfCoreWebApp.Tests.Utils;

namespace EfCoreWebApp.Tests;

public class ProductFeatureTests
{
    [Fact]
    public async Task 商品一覧画面を見る_ヘッダー表示とPDF保存()
    {
        var client = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

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
        await BrowserScreenshot.TakeScreenshotAsync("http://localhost:5000/products", screenshotFile);
        
        // 拡張PDF レポート生成 (スクリーンショット埋込み対応)
        var pdfFile = Path.Combine(dir, "acceptance-report.pdf");
        var featureFilePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "Features", "product_management.feature");
        EnhancedPdfReport.GenerateTestReport(pdfFile, featureFilePath, screenshotFile, "PASS");
        
        // CSV レポート生成 (Excel互換)
        var featureFile = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "Features", "product_management.feature");
        var csvFile = Path.Combine(dir, "test-report.csv");
        ExcelReport.GenerateFromFeature(featureFile, csvFile, "PASS", screenshotFile);
        
        // 真のExcel形式のファイル生成 (test-specimen.xlsx) - 複数シート対応
        var excelFile = Path.Combine(dir, "test-specimen.xlsx");
        await ExcelReportGenerator.GenerateTestReport(excelFile, featureFile, screenshotFile, "PASS");
    }
}
