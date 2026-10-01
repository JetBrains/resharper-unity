#nullable enable
using System;
using System.IO;
using System.Threading.Tasks;
using JetBrains.FormatRipper;
using JetBrains.FormatRipper.Elf;
using JetBrains.FormatRipper.FileExplorer;
using JetBrains.FormatRipper.MachO;
using JetBrains.FormatRipper.Pe;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration;

public static class UnityAssemblyScanner
{
    private const string UnityScriptingBackendSymbolName = "UnityScriptingBackend";

    // caching is done on the frontend side
    public static Task<UnityScriptingBackend> TryScan(string path)
    {
        try
        {
            return RdTask.Successful(Scan(path));
        }
        catch (Exception e)
        {
            return RdTask.Faulted<UnityScriptingBackend>(e);
        }
    }

    private static UnityScriptingBackend Scan(string path)
    {
        using var stream = File.OpenRead(path);
        var createDataStream = FileTypeExplorer.Detect(stream).FileType switch
            {
                FileType.Elf => TryGetElfSymbol(stream),
                FileType.MachO => TryGetMachOSymbol(stream),
                FileType.Pe => TryGetPeSymbol(stream),
                _ => null
            };
        if (createDataStream == null)
            return UnityScriptingBackend.Unknown;
        using var dataStream = createDataStream();
        return PeUtil.ReadStringZ(dataStream) switch
            {
                "CoreCLR" => UnityScriptingBackend.CoreCLR,
                "IL2CPP" => UnityScriptingBackend.IL2CPP,
                "Mono" => UnityScriptingBackend.Mono,
                _ => UnityScriptingBackend.Unknown,
            };
    }

    private static DelegateUtil.CreateStreamDelegate? TryGetElfSymbol(Stream stream) =>
        ElfUtil.TryGetDynamicSymbol(ElfFile.Parse(stream), UnityScriptingBackendSymbolName, out var symbol)
            ? symbol.CreateStream
            : null;

    private static DelegateUtil.CreateStreamDelegate? TryGetMachOSymbol(Stream stream)
    {
        foreach (var image in MachOFile.Parse(stream).Images)
            if (MachOUtil.TryGetSymbol(image, "_" + UnityScriptingBackendSymbolName, out var symbol))
                return symbol.CreateStream;

        return null;
    }

    private static DelegateUtil.CreateStreamDelegate? TryGetPeSymbol(Stream stream) =>
        PeUtil.TryGetExport(PeFile.Parse(stream), UnityScriptingBackendSymbolName, out var export)
            ? export.CreateStream
            : null;
}