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
        var file = Path.Combine(dir, "acceptance-report.pdf");

        PdfReport.Save(file, new[]
        {
            "# language: ja",
            "機能: 商品管理",
            "",
            "シナリオ: 商品⼀覧画⾯を⾒る",
            "  前提 ホームページを表⽰する",
            "  もし 商品⼀覧をリクエストする",
            "  ならば \"Products List\" ヘッダーが表⽰されること",
            "  かつ 受⼊レポートをPDFとして保存する",
            "Products List",
            "1: qq - 4.00"
        });
    }
}
