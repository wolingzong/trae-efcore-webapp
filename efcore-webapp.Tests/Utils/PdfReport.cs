using System.Text;
using SkiaSharp;

namespace EfCoreWebApp.Tests.Utils;

public static class PdfReport
{
    public static void Save(string path, IEnumerable<string> lines)
    {
        using var stream = File.Open(path, FileMode.Create, FileAccess.ReadWrite);
        using var document = SKDocument.CreatePdf(stream);
        var page = document.BeginPage(595, 842);
        var canvas = page;

        var typeface = ResolveTypeface();
        var paint = new SKPaint
        {
            Typeface = typeface,
            TextSize = 16,
            IsAntialias = true,
            Color = SKColors.Black
        };

        float x = 50;
        float y = 60;
        float lineHeight = 24;
        foreach (var line in lines)
        {
            canvas.DrawText(line ?? string.Empty, x, y, paint);
            y += lineHeight;
            if (y > 800)
            {
                document.EndPage();
                page = document.BeginPage(595, 842);
                canvas = page;
                y = 60;
            }
        }

        document.EndPage();
        document.Close();
    }

    public static void Save(string path, string title, string scenario, string result)
    {
        Save(path, new[] { title, scenario, result });
    }

    private static SKTypeface ResolveTypeface()
    {
        var families = new[]
        {
            "Hiragino Sans",
            "PingFang SC",
            "Noto Sans CJK JP",
            "Noto Sans JP",
            "Microsoft YaHei UI",
            "Arial Unicode MS",
            "Apple SD Gothic Neo",
            "System Font"
        };
        foreach (var name in families)
        {
            var tf = SKTypeface.FromFamilyName(name);
            if (tf != null) return tf;
        }
        return SKTypeface.Default;
    }
}
