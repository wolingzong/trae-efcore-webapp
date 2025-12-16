using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;
using TechTalk.SpecFlow;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace EfCoreWebApp.Tests.Steps;

[Binding]
public class ProductSteps
{
    private readonly WebApplicationFactory<Program> _factory = new();
    private readonly ScenarioContext _scenarioContext;
    private HttpClient _client = default!;
    private HttpResponseMessage _response = default!;
    private string _content = string.Empty;

    public ProductSteps(ScenarioContext scenarioContext)
    {
        _scenarioContext = scenarioContext;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    [Given(@"ホームページを表示する")]
    public async Task GivenDisplayHomePage()
    {
        _client = _factory.CreateClient();
        _response = await _client.GetAsync("/");
        _response.EnsureSuccessStatusCode();
    }

    [When(@"商品一覧をリクエストする")]
    public async Task WhenRequestProducts()
    {
        _response = await _client.GetAsync("/products");
        _response.EnsureSuccessStatusCode();
        _content = await _response.Content.ReadAsStringAsync();
    }

    [Then(@"""Products List"" ヘッダーが表示されること")]
    public void ThenHeaderShouldBeVisible()
    {
        Assert.Contains("<h1>Products List</h1>", _content);
    }

    [Then(@"受入レポートをPDFとして保存する")]
    public void ThenSaveAcceptanceReportAsPdf()
    {
        var path = Path.Combine(Directory.GetCurrentDirectory(), "TestResults", DateTime.Now.ToString("yyyyMMdd"));
        Directory.CreateDirectory(path);
        var file = Path.Combine(path, "acceptance-report.pdf");

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.Content().Column(col =>
                {
                    col.Item().Text("受入レポート").FontSize(20);
                    col.Item().Text("シナリオ: 商品一覧画面を見る");
                    col.Item().Text("結果: ヘッダー表示確認済み");
                });
            });
        }).GeneratePdf(file);
    }
}
