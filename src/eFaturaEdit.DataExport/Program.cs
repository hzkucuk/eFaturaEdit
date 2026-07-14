using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using eFaturaEdit;

namespace eFaturaEdit.DataExport;

/// <summary>
/// eFaturaEdit.Core POCO verilerini Tauri/SvelteKit frontend için
/// TypeScript-friendly JSON dosyalarına dönüştürür.
///
/// <para>
/// Kullanım:
/// <code>dotnet run --project src/eFaturaEdit.DataExport -- &lt;output-dir&gt;</code>
/// Varsayılan çıktı dizini: <c>./data/</c>
/// </para>
/// </summary>
internal static class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // Türkçe karakterler (ı, ğ, ü, ş, ö, ç, İ) escape edilmesin
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    private static int Main(string[] args)
    {
        string outputDir = args.Length > 0 ? args[0] : "data";
        outputDir = Path.GetFullPath(outputDir);
        Directory.CreateDirectory(outputDir);

        Console.WriteLine($"eFaturaEdit.DataExport → {outputDir}");
        Console.WriteLine();

        try
        {
            ExportSnippets(outputDir);
            ExportSamples(outputDir);
            ExportCompletion(outputDir);
            ExportSkills(outputDir);
            ExportManifest(outputDir);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"HATA: {ex.Message}");
            Console.Error.WriteLine(ex);
            return 1;
        }

        Console.WriteLine();
        Console.WriteLine("Tüm veriler başarıyla dışa aktarıldı.");
        return 0;
    }

    private static void ExportSnippets(string outputDir)
    {
        // Dictionary → Array (JS-friendly, iteration için)
        var snippets = XsltSnippets.Elements.Values
            .Select(s => new SnippetJson(
                Key: s.Key,
                Category: s.Category,
                SubCategory: s.SubCategory,
                DisplayName: s.DisplayName,
                Description: s.Description,
                IconText: s.IconText,
                XsltCode: s.XsltCode,
                DragDataString: s.DragDataString))
            .ToArray();

        string path = Path.Combine(outputDir, "snippets.json");
        File.WriteAllText(path, JsonSerializer.Serialize(snippets, JsonOptions));
        Console.WriteLine($"  ✓ snippets.json      ({snippets.Length} snippet, {new FileInfo(path).Length / 1024} KB)");
    }

    private static void ExportSamples(string outputDir)
    {
        var samples = new SamplesJson(
            SamplesRelativePath: UblTrSamples.SamplesRelativePath,
            Groups: UblTrSamples.Groups
                .Select(g => new SampleGroupJson(
                    CategoryName: g.CategoryName,
                    Entries: g.Entries
                        .Select(e => new SampleEntryJson(e.FileName, e.DisplayName))
                        .ToArray()))
                .ToArray());

        string path = Path.Combine(outputDir, "samples.json");
        File.WriteAllText(path, JsonSerializer.Serialize(samples, JsonOptions));
        int totalEntries = samples.Groups.Sum(g => g.Entries.Length);
        Console.WriteLine($"  ✓ samples.json       ({samples.Groups.Length} grup, {totalEntries} örnek XML)");
    }

    private static void ExportCompletion(string outputDir)
    {
        var completion = new CompletionJson(
            XsltTags: XsltCompletionCatalog.XsltTags
                .Select(i => new CompletionItemJson(i.Text, i.Description, i.ImageIndex))
                .ToArray(),
            XPathPaths: XsltCompletionCatalog.XPathPaths
                .Select(i => new CompletionItemJson(i.Text, i.Description, i.ImageIndex))
                .ToArray());

        string path = Path.Combine(outputDir, "completion.json");
        File.WriteAllText(path, JsonSerializer.Serialize(completion, JsonOptions));
        Console.WriteLine($"  ✓ completion.json    ({completion.XsltTags.Length} XSLT tag, {completion.XPathPaths.Length} XPath öneri)");
    }

    private static void ExportSkills(string outputDir)
    {
        var skills = AiSkillCatalog.All
            .Select(s => new AiSkillJson(
                Id: s.Id,
                Category: s.Category,
                DisplayName: s.DisplayName,
                Description: s.Description,
                Prompt: s.Prompt))
            .ToArray();

        string path = Path.Combine(outputDir, "skills.json");
        File.WriteAllText(path, JsonSerializer.Serialize(skills, JsonOptions));
        int promptChars = skills.Sum(s => s.Prompt.Length);
        Console.WriteLine($"  ✓ skills.json        ({skills.Length} yetenek, {promptChars} karakter prompt)");
    }

    private static void ExportManifest(string outputDir)
    {
        var manifest = new ManifestJson(
            Version: "2.33.1",
            GeneratedAt: DateTime.UtcNow.ToString("O"),
            Source: "eFaturaEdit.Core",
            Files: new[] { "snippets.json", "samples.json", "completion.json", "skills.json" });

        string path = Path.Combine(outputDir, "manifest.json");
        File.WriteAllText(path, JsonSerializer.Serialize(manifest, JsonOptions));
        Console.WriteLine($"  ✓ manifest.json");
    }
}

// ─── JSON DTO'lar (camelCase serialize edilir) ──────────────────────────────

internal record SnippetJson(
    string Key,
    string Category,
    string? SubCategory,
    string DisplayName,
    string Description,
    string IconText,
    string XsltCode,
    string DragDataString);

internal record SamplesJson(
    string SamplesRelativePath,
    SampleGroupJson[] Groups);

internal record SampleGroupJson(
    string CategoryName,
    SampleEntryJson[] Entries);

internal record SampleEntryJson(
    string FileName,
    string DisplayName);

internal record CompletionJson(
    CompletionItemJson[] XsltTags,
    CompletionItemJson[] XPathPaths);

internal record CompletionItemJson(
    string Text,
    string Description,
    int ImageIndex);

internal record AiSkillJson(
    string Id,
    string Category,
    string DisplayName,
    string Description,
    string Prompt);

internal record ManifestJson(
    string Version,
    string GeneratedAt,
    string Source,
    string[] Files);
