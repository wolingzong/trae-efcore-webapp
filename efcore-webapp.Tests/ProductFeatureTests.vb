Imports System
Imports System.IO
Imports System.Threading.Tasks
Imports Xunit
Imports Microsoft.Playwright
Imports ClosedXML.Excel

Namespace EfCoreWebApp.Tests
  Public Class ProductFeatureTests
    <Fact>
    Public Async Function 受入シナリオ_商品一覧_Excel_スクリーンショット() As Task
      Dim baseUrl = New Uri("http://localhost:5000")
      Using pw = Await Playwright.CreateAsync()
        Dim browser = Await pw.Chromium.LaunchAsync(New BrowserTypeLaunchOptions With {.Headless = True})
        Dim context = Await browser.NewContextAsync()
        Dim page = Await context.NewPageAsync()

        Dim homeResp = Await page.GotoAsync(baseUrl.ToString())
        Assert.True(homeResp.Ok)

        Dim prodResp = Await page.GotoAsync(New Uri(baseUrl, "/products").ToString())
        Assert.True(prodResp.Ok)
        Dim h1 = Await page.TextContentAsync("h1")
        Assert.Contains("Products List", h1)

        Dim outDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestResults", DateTime.Now.ToString("yyyyMMdd"))
        Directory.CreateDirectory(outDir)
        Dim screenshotPath = Path.Combine(outDir, "products.png")
        Await page.ScreenshotAsync(New PageScreenshotOptions With {.Path = screenshotPath, .FullPage = True})

        Dim excelPath = Path.Combine(outDir, "test-specimen.xlsx")
        Using wb As New XLWorkbook()
          Dim ws = wb.Worksheets.Add("試様")
          ws.Cell(1, 1).Value = "No."
          ws.Cell(1, 2).Value = "テストプロジェクト"
          ws.Cell(1, 3).Value = "テスト項目"
          ws.Cell(1, 4).Value = "テスト詳細"
          ws.Cell(1, 5).Value = "期待結果"
          ws.Cell(1, 6).Value = "判定"
          ws.Cell(1, 7).Value = "実施日"
          ws.Cell(1, 8).Value = "エビデンス"
          ws.Row(1).Style.Font.Bold = True

          ws.Cell(2, 1).Value = "1"
          ws.Cell(2, 2).Value = "商品管理"
          ws.Cell(2, 3).Value = "商品一覧画面を見る"
          ws.Cell(2, 4).Value = "前提 ホームページを表示する" & vbCrLf & "もし 商品一覧をリクエストする"
          ws.Cell(2, 5).Value = "ならば ""Products List"" ヘッダーが表示されること" & vbCrLf & "かつ 受入レポートをPDFとして保存する"
          ws.Cell(2, 6).Value = "OK"
          ws.Cell(2, 7).Value = "2025/12/15"
          Dim evidenceSheet = wb.Worksheets.Add("エビデンス")
          Dim pic = evidenceSheet.AddPicture(screenshotPath)
          pic.MoveTo(evidenceSheet.Cell(1, 1))
          pic.WithSize(800, 450)
          ws.Cell(2, 8).FormulaA1 = "=HYPERLINK(""#'エビデンス'!A1"",""View"")"
          ws.Cell(2, 8).Style.Font.FontColor = ClosedXML.Excel.XLColor.Blue
          ws.Cell(2, 8).Style.Font.SetUnderline(ClosedXML.Excel.XLFontUnderlineValues.Single)

          ws.Cell(2, 4).Style.Alignment.SetWrapText(True)
          ws.Cell(2, 5).Style.Alignment.SetWrapText(True)
          ws.Column(1).Width = 6
          ws.Column(2).Width = 14
          ws.Column(3).Width = 20
          ws.Column(4).Width = 30
          ws.Column(5).Width = 30
          ws.Column(6).Width = 8
          ws.Column(7).Width = 12
          ws.Column(8).Width = 12
          ws.Row(2).AdjustToContents()

          Assert.True(evidenceSheet.Pictures.Count > 0)
          wb.SaveAs(excelPath)
        End Using

        Await browser.CloseAsync()
      End Using
    End Function
  End Class
End Namespace
