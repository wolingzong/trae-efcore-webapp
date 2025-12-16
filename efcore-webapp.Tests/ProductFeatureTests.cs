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
        
        // PDF レポート生成
        var pdfFile = Path.Combine(dir, "acceptance-report.pdf");
        var pdfContent = new List<string>
        {
            "テスト実行報告書",
            "===================",
            "",
            "機能: 商品管理",
            "実行日時: " + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
            "",
            "シナリオ: 商品一覧画面を見る",
            "  前提 ホームページを表示する ✓",
            "  もし 商品一覧をリクエストする ✓", 
            "  ならば \"Products List\" ヘッダーが表示されること ✓",
            "  かつ 受入レポートをPDFとして保存する ✓",
            "",
            "テスト結果: PASS",
            "実行時間: " + DateTime.Now.ToString("HH:mm:ss"),
            "",
            "添付ファイル:",
            "- スクリーンショット: " + Path.GetFileName(screenshotFile),
            "- Excel報告書: test-specimen.xlsx"
        };
        
        PdfReport.Save(pdfFile, pdfContent);
        
        // CSV レポート生成 (Excel互換)
        var featureFile = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "Features", "product_management.feature");
        var csvFile = Path.Combine(dir, "test-report.csv");
        ExcelReport.GenerateFromFeature(featureFile, csvFile, "PASS", screenshotFile);
        
        // 真のExcel形式のファイル生成 (test-specimen.xlsx) - 複数シート対応
        var excelFile = Path.Combine(dir, "test-specimen.xlsx");
        await ExcelReportGenerator.GenerateTestReport(excelFile, featureFile, screenshotFile, "PASS");
    }
}
