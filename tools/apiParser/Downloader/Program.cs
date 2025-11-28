
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

internal class Program
{
    // app downloads
    // https://cloudmedia-docs.unity3d.com/docscloudstorage/<languagecode>/<version>/UnityDocumentation.zip
    // Language code: en, ja, kr, and cn
    // Versions: 2023.2, 6000.0, 6000.1, 6000.2, 6000.3
    // like "https://cloudmedia-docs.unity3d.com/docscloudstorage/ja/2020.3/UnityDocumentation.zip"
    // after downloading all the zip-s, extract them to get the structure like below:
    // Documentation-(<version>)
    //   Documentation
    //     en
    //     ja
    //     kr
    //     cn

    private static readonly string[] Languages = { "en", "ja", "kr", "cn" };
    private static readonly string[] Versions = { "2023.2", "6000.0", "6000.1", "6000.2", "6000.3" };

    private const string BaseUrl = "https://cloudmedia-docs.unity3d.com/docscloudstorage";

    private static async Task<int> Main(string[] args)
    {
        try
        {
            var outRoot = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
                ? args[0]
                : Path.Combine(Environment.CurrentDirectory, "Docs");

            Directory.CreateDirectory(outRoot);

            Console.WriteLine($"Output root: {outRoot}");

            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromMinutes(30);

            foreach (var version in Versions)
            {
                var versionFolder = Path.Combine(outRoot, $"Documentation-{version}");
                var documentationFolder = Path.Combine(versionFolder, "Documentation");
                Directory.CreateDirectory(documentationFolder);

                foreach (var lang in Languages)
                {
                    var langFolder = Path.Combine(documentationFolder, lang);
                    if (IsAlreadyExtracted(langFolder))
                    {
                        Console.WriteLine($"Skip: already extracted {version} {lang}");
                        continue;
                    }

                    // Ensure base folders exist
                    Directory.CreateDirectory(versionFolder);
                    Directory.CreateDirectory(documentationFolder);

                    var url = $"{BaseUrl}/{lang}/{version}/UnityDocumentation.zip";
                    var zipPath = Path.Combine(versionFolder, $"UnityDocumentation-{version}-{lang}.zip");

                    Console.WriteLine($"Downloading {url}");
                    var ok = await DownloadWithRetryAsync(http, url, zipPath, retries: 3);
                    if (!ok)
                    {
                        Console.WriteLine($"WARN: Failed to download {url}. Skipping.");
                        SafeDeleteIfExists(zipPath);
                        continue;
                    }

                    try
                    {
                        // Each zip already contains Documentation/<langCode>/...
                        // Extract to version root so all languages merge into a single Documentation folder
                        ZipFile.ExtractToDirectory(zipPath, versionFolder, overwriteFiles: true);
                        Console.WriteLine($"Extracted {lang} for {version} into {documentationFolder}");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"ERROR: Failed to extract {zipPath} to {versionFolder}: {e.Message}");
                    }
                    finally
                    {
                        // Keep the zips by default; uncomment next line to remove after extraction
                        // SafeDeleteIfExists(zipPath);
                    }
                }
            }

            Console.WriteLine("Done");
            return 0;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Fatal error: {e}");
            return 1;
        }
    }

    private static bool IsAlreadyExtracted(string langFolder)
    {
        try
        {
            if (!Directory.Exists(langFolder)) return false;
            // Heuristic: extracted folder should contain an index or at least some files
            return Directory.EnumerateFileSystemEntries(langFolder).GetEnumerator().MoveNext();
        }
        catch
        {
            return false;
        }
    }

    private static async Task<bool> DownloadWithRetryAsync(HttpClient http, string url, string destination, int retries)
    {
        for (var attempt = 1; attempt <= retries; attempt++)
        {
            try
            {
                using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Attempt {attempt}: HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
                    continue;
                }

                await using var stream = await response.Content.ReadAsStreamAsync();
                await using var file = File.Create(destination);
                await stream.CopyToAsync(file);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Attempt {attempt} failed: {e.Message}");
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(30, attempt * 3)));
            }
        }

        return false;
    }

    private static void SafeDeleteIfExists(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // ignore
        }
    }

    private static void EmptyDirectory(string path)
    {
        if (!Directory.Exists(path)) return;
        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            try { File.Delete(file); } catch { }
        }
        foreach (var dir in Directory.EnumerateDirectories(path, "*", SearchOption.AllDirectories))
        {
            try { Directory.Delete(dir, true); } catch { }
        }
    }
}