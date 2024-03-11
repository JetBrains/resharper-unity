using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

if (args.Length != 1 || !Uri.TryCreate(args[0], UriKind.Absolute, out var fileUri))
{
    Console.Error.WriteLine("Should have a single parameter with builtin-shaders uri like https://download.unity3d.com/download_unity/19eeb3b320af/builtin_shaders-2023.2.12f1.zip");
    return -1;
}

var regex = new Regex("\\bShader\\s*\"([^\"]+)\"", RegexOptions.Compiled);

using var client = new HttpClient();
await using var stream = await client.GetStreamAsync(fileUri);
using var archive = new ZipArchive(stream);
var shaderNames = new List<string>();
foreach (var entry in archive.Entries.Where(it => it.Name.EndsWith(".shader")))
{
    using var reader = new StreamReader(entry.Open(), Encoding.UTF8);
    var shaderText = reader.ReadToEnd();
    var match = regex.Match(shaderText);
    if (!match.Success)
    {
        Console.WriteLine($"No shader name found in {entry.Name}");
        continue;
    }
    shaderNames.Add(match.Groups[1].Value);
}

Console.WriteLine("================");
shaderNames.Sort();
foreach (var shaderName in shaderNames)
{
    Console.WriteLine($"\"{shaderName}\",");
}
Console.WriteLine("=================");


return 0;