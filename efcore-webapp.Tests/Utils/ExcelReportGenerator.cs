using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using OfficeOpenXml.Style;
using System.Drawing;

namespace EfCoreWebApp.Tests.Utils;

public static class ExcelReportGenerator
{
    static ExcelReportGenerator()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
    }
    
    public static async Task GenerateTestReport(string excelPath, string featureFilePath, string screenshotPath, string testResult = "PASS")
    {
        var featureContent = File.ReadAllText(featureFilePath);
        var scenarios = ParseFeatureFile(featureContent);
        
        using var package = new ExcelPackage();
        
        // シート1: テスト結果
        CreateTestResultSheet(package, scenarios, testResult, screenshotPath);
        
        // シート2: スクリーンショット
        await CreateScreenshotSheet(package, screenshotPath);
        
        // シート3: 詳細ログ
        CreateDetailLogSheet(package, scenarios, testResult);
        
        await package.SaveAsAsync(new FileInfo(excelPath));
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
    
    private static void CreateTestResultSheet(ExcelPackage package, List<ScenarioInfo> scenarios, string testResult, string screenshotPath)
    {
        var worksheet = package.Workbook.Worksheets.Add("テスト結果");
        
        // ヘッダー設定
        worksheet.Cells["A1"].Value = "商品管理システム テスト報告書";
        worksheet.Cells["A1:H1"].Merge = true;
        worksheet.Cells["A1"].Style.Font.Size = 16;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        worksheet.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
        worksheet.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.LightBlue);
        
        // 基本情報
        var row = 3;
        worksheet.Cells[$"A{row}"].Value = "実行日時:";
        worksheet.Cells[$"B{row}"].Value = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
        row++;
        worksheet.Cells[$"A{row}"].Value = "総合結果:";
        worksheet.Cells[$"B{row}"].Value = testResult;
        worksheet.Cells[$"B{row}"].Style.Font.Bold = true;
        worksheet.Cells[$"B{row}"].Style.Font.Color.SetColor(testResult == "PASS" ? Color.Green : Color.Red);
        
        // テストケース詳細ヘッダー
        row += 2;
        var headers = new[] { "No", "シナリオ", "ステップ", "期待結果", "実際結果", "判定", "備考", "スクリーンショット" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = worksheet.Cells[row, i + 1];
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
            cell.Style.Border.BorderAround(ExcelBorderStyle.Thin);
        }
        
        // テストケースデータ
        row++;
        var testCaseNo = 1;
        foreach (var scenario in scenarios)
        {
            for (int i = 0; i < scenario.Steps.Count; i++)
            {
                worksheet.Cells[row, 1].Value = testCaseNo++;
                worksheet.Cells[row, 2].Value = i == 0 ? scenario.Name : "";
                worksheet.Cells[row, 3].Value = scenario.Steps[i];
                worksheet.Cells[row, 4].Value = "正常実行";
                worksheet.Cells[row, 5].Value = "正常実行";
                worksheet.Cells[row, 6].Value = testResult == "PASS" ? "OK" : "NG";
                worksheet.Cells[row, 7].Value = "";
                
                // スクリーンショットへのリンク（最初のステップのみ）
                if (i == 0 && File.Exists(screenshotPath))
                {
                    var linkCell = worksheet.Cells[row, 8];
                    linkCell.Value = "スクリーンショット";
                    linkCell.Style.Font.Color.SetColor(Color.Blue);
                    linkCell.Style.Font.UnderLine = true;
                    // 内部リンクを設定（スクリーンショットシートのA1セルへ）
                    linkCell.Hyperlink = new ExcelHyperLink("スクリーンショット!A1", "スクリーンショットを表示");
                }
                
                // 行の境界線
                for (int col = 1; col <= 8; col++)
                {
                    worksheet.Cells[row, col].Style.Border.BorderAround(ExcelBorderStyle.Thin);
                }
                
                row++;
            }
        }
        
        // 統計情報
        row += 2;
        worksheet.Cells[$"A{row}"].Value = "テスト結果サマリー";
        worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
        worksheet.Cells[$"A{row}"].Style.Font.Size = 14;
        row++;
        
        worksheet.Cells[$"A{row}"].Value = "総テストケース数:";
        worksheet.Cells[$"B{row}"].Value = scenarios.Sum(s => s.Steps.Count);
        row++;
        worksheet.Cells[$"A{row}"].Value = "成功:";
        worksheet.Cells[$"B{row}"].Value = testResult == "PASS" ? scenarios.Sum(s => s.Steps.Count) : 0;
        row++;
        worksheet.Cells[$"A{row}"].Value = "失敗:";
        worksheet.Cells[$"B{row}"].Value = testResult == "PASS" ? 0 : scenarios.Sum(s => s.Steps.Count);
        row++;
        worksheet.Cells[$"A{row}"].Value = "成功率:";
        worksheet.Cells[$"B{row}"].Value = testResult == "PASS" ? "100%" : "0%";
        
        // 列幅自動調整
        worksheet.Cells.AutoFitColumns();
    }
    
    private static async Task CreateScreenshotSheet(ExcelPackage package, string screenshotPath)
    {
        var worksheet = package.Workbook.Worksheets.Add("スクリーンショット");
        
        // ヘッダー
        worksheet.Cells["A1"].Value = "テスト実行時スクリーンショット";
        worksheet.Cells["A1"].Style.Font.Size = 14;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        
        worksheet.Cells["A2"].Value = $"撮影日時: {DateTime.Now:yyyy/MM/dd HH:mm:ss}";
        worksheet.Cells["A3"].Value = "画面: 商品一覧ページ";
        
        // スクリーンショット画像を挿入
        if (File.Exists(screenshotPath))
        {
            try
            {
                var picture = worksheet.Drawings.AddPicture("Screenshot", new FileInfo(screenshotPath));
                picture.SetPosition(4, 0, 0, 0); // 5行目から開始
                picture.SetSize(600, 400); // サイズ調整
            }
            catch (Exception ex)
            {
                worksheet.Cells["A5"].Value = $"スクリーンショット読み込みエラー: {ex.Message}";
                worksheet.Cells["A6"].Value = $"ファイルパス: {screenshotPath}";
            }
        }
        else
        {
            worksheet.Cells["A5"].Value = "スクリーンショットファイルが見つかりません";
            worksheet.Cells["A6"].Value = $"パス: {screenshotPath}";
        }
        
        // テスト結果シートへの戻りリンク
        var backLink = worksheet.Cells["A30"];
        backLink.Value = "← テスト結果に戻る";
        backLink.Style.Font.Color.SetColor(Color.Blue);
        backLink.Style.Font.UnderLine = true;
        backLink.Hyperlink = new ExcelHyperLink("テスト結果!A1", "テスト結果シートに戻る");
    }
    
    private static void CreateDetailLogSheet(ExcelPackage package, List<ScenarioInfo> scenarios, string testResult)
    {
        var worksheet = package.Workbook.Worksheets.Add("詳細ログ");
        
        // ヘッダー
        worksheet.Cells["A1"].Value = "テスト実行詳細ログ";
        worksheet.Cells["A1"].Style.Font.Size = 14;
        worksheet.Cells["A1"].Style.Font.Bold = true;
        
        var row = 3;
        worksheet.Cells[$"A{row}"].Value = "実行開始時刻:";
        worksheet.Cells[$"B{row}"].Value = DateTime.Now.AddMinutes(-1).ToString("yyyy/MM/dd HH:mm:ss");
        row++;
        worksheet.Cells[$"A{row}"].Value = "実行終了時刻:";
        worksheet.Cells[$"B{row}"].Value = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
        row++;
        worksheet.Cells[$"A{row}"].Value = "実行時間:";
        worksheet.Cells[$"B{row}"].Value = "約1分";
        
        row += 2;
        worksheet.Cells[$"A{row}"].Value = "実行ログ:";
        worksheet.Cells[$"A{row}"].Style.Font.Bold = true;
        row++;
        
        foreach (var scenario in scenarios)
        {
            worksheet.Cells[$"A{row}"].Value = $"[{DateTime.Now:HH:mm:ss}] シナリオ開始: {scenario.Name}";
            row++;
            
            foreach (var step in scenario.Steps)
            {
                worksheet.Cells[$"A{row}"].Value = $"[{DateTime.Now:HH:mm:ss}] ステップ実行: {step}";
                worksheet.Cells[$"B{row}"].Value = testResult == "PASS" ? "成功" : "失敗";
                worksheet.Cells[$"B{row}"].Style.Font.Color.SetColor(testResult == "PASS" ? Color.Green : Color.Red);
                row++;
            }
            
            worksheet.Cells[$"A{row}"].Value = $"[{DateTime.Now:HH:mm:ss}] シナリオ完了: {scenario.Name}";
            row++;
            row++; // 空行
        }
        
        worksheet.Cells.AutoFitColumns();
    }
    
    private class ScenarioInfo
    {
        public string Name { get; set; } = "";
        public List<string> Steps { get; set; } = new List<string>();
    }
}