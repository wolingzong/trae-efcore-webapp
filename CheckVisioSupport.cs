using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using System;

class Program {
    static void Main() {
        // Try to verify if Visio related classes exist
        // Note: OpenXml SDK usually handles DOCX, XLSX, PPTX. 
        // VSDX usage is rare/unsupported in the main SDK usually.
        // But let's check for 'VsdxDocument' or similar.
        Console.WriteLine("Checking types...");
    }
}
