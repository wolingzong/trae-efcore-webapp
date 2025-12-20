using System;
using System.IO;
using EfCoreWebApp.Tests.Utils;

class Program {
    static void Main() {
        try {
            var featurePath = "Features/product_management.feature";
            var vsdxPath = "debug_output.vsdx";
            if (!File.Exists(featurePath)) {
                // Try finding it
                featurePath = Path.Combine(Directory.GetCurrentDirectory(), "efcore-webapp.Tests", "Features", "product_management.feature");
            }
            if (!File.Exists(featurePath)) {
                Console.WriteLine($"Feature file not found at {featurePath}");
                return;
            }

            Console.WriteLine($"Generating VSDX to {vsdxPath}...");
            VsdxReportGenerator.GenerateDiagram(vsdxPath, featurePath);
            Console.WriteLine("Generation complete.");
            
            if (File.Exists(vsdxPath)) {
                Console.WriteLine($"File exists. Size: {new FileInfo(vsdxPath).Length} bytes");
            } else {
                Console.WriteLine("File NOT created.");
            }
        } catch (Exception ex) {
            Console.WriteLine($"Error: {ex}");
        }
    }
}
