// See https://aka.ms/new-console-template for more information

using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;


var buildDirectory = args[0];
var sdk = args[1];

Console.WriteLine($"Working folder: {buildDirectory}");
var path = sdk;
var configuration = "Release";

if (path == null)
{
    Console.Error.WriteLine("Fleet.Backend Sdk path is not found");
    return -1;
}

Console.Error.WriteLine($"Sdk path = {path}");

GenerateNugetConfig(path);
GenerateDotNetSdkPath(path);

var buildCode = RunBuild(@"C:\workspace\resharper-unity\fleet\resharper-unity-fleet.sln", configuration, false);
if (buildCode != 0)
    return buildCode;


var resultPluginDirectory = Path.Combine(buildDirectory, "build", "fleet.dotnet.unity");
if (Directory.Exists(resultPluginDirectory))
    Directory.Delete(resultPluginDirectory, true);

Directory.CreateDirectory(resultPluginDirectory);

var backendFilesToCopy = new List<string>()
{
    "JetBrains.ReSharper.Plugins.Unity.dll",
    "JetBrains.ReSharper.Plugins.Unity.Fleet.dll",
    "JetBrains.ReSharper.Plugins.Json.dll",
    "JetBrains.ReSharper.Plugins.Yaml.dll"
};

var resultBackendDirectory = Path.Combine(resultPluginDirectory, "backend");
Directory.CreateDirectory(resultBackendDirectory);

var originBackendPath = Path.Combine(buildDirectory, "..", "resharper", "build", "Unity", "bin", configuration, "net472");
foreach (var fileToCopy in backendFilesToCopy)
{
    var originFile = Path.Combine(originBackendPath, fileToCopy);
    if (!File.Exists(originFile))
    {
        Console.WriteLine($"Expected \"{originFile}\" to exist");
        return -1;
    }
    File.Copy(originFile, Path.Combine(resultBackendDirectory, fileToCopy), true);   
}

// final
var archiveFile = resultPluginDirectory + ".zip";
if (File.Exists(archiveFile))
    File.Delete(archiveFile);
ZipFile.CreateFromDirectory(resultPluginDirectory, archiveFile);



string? FindDotnet()
{
    var components = Environment.GetEnvironmentVariable("PATH").Split(Path.PathSeparator);

    foreach (var component in components)
    {
        var dotnet = Path.Combine(component, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dotnet.exe" : "dotnet");
        if (File.Exists(dotnet))
            return dotnet;
    }

    Console.Error.Write("Could not find dotnet");
    return null;
}

int RunBuild(string slnFile, string buildConfiguration, bool warningsAsErrors)
{
    var dotnet = FindDotnet();
    if (dotnet == null)
    {
        return -1;
    }

    var arguments = new List<string>()
    {
        "build",
        $"\"{slnFile}\"",
        $"/p:Configuration={buildConfiguration}",
        $"/p:TreatWarningsAsErrors={warningsAsErrors}",
        $"/\"bl:{Path.GetFileName(slnFile) + ".binlog"}\"",
        "/nologo"

    };


    var processInfo = new ProcessStartInfo(dotnet, String.Join(" ", arguments));
    var process = Process.Start(processInfo);
    process.WaitForExit();

    return process.ExitCode;
}

void GenerateNugetConfig(string path)
{
    var directory = Path.Combine(buildDirectory, "..");
    if (!Directory.Exists(directory))
        Directory.CreateDirectory(directory);
            
    File.WriteAllText(Path.Combine(directory,  "NuGet.Config"),
        $""" 
        <?xml version="1.0" encoding="utf-8"?>
        <configuration>
            <packageSources>
                <clear />
                <add key="local-fleet-dotnet-sdk" value="{path}" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
            </packageSources>
        </configuration>
        """, Encoding.UTF8);
}


void GenerateDotNetSdkPath(string path)
{
    var directory = Path.Combine(buildDirectory, "..", "resharper", "resharper", "build", "generated");
    if (!Directory.Exists(directory))
        Directory.CreateDirectory(directory);
    
    File.WriteAllText(Path.Combine(directory, "DotNetSdkPath.generated.props"),
        $""" 
        <Project>
          <PropertyGroup>
            <DotNetSdkPath>{path}</DotNetSdkPath>
          </PropertyGroup>
        </Project>
        """, Encoding.UTF8);
}

return 0;