using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Rd.Tasks;
using JetBrains.Rider.Model.Unity.FrontendBackend;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Integration;

public static class UnityAssemblyScanner
{
    // Unity stamps this exported data symbol into every native runtime/editor binary.
    // Its value points at a NUL-terminated UTF-8 string naming the scripting backend, e.g. "Mono", "IL2CPP", "CoreCLR".
    private const string SymbolName = "UnityScriptingBackend";
    private static readonly byte[] ourElfPeName = Encoding.ASCII.GetBytes(SymbolName);
    // On Mach-O, C symbols are emitted with a leading underscore.
    private static readonly byte[] ourMachOName = Encoding.ASCII.GetBytes("_" + SymbolName);

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
        var size = new FileInfo(path).Length;
        if (size is < 4 or > int.MaxValue) return UnityScriptingBackend.Unknown;
        
        using var mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        using var view = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
        unsafe
        {
            byte* p = null;
            view.SafeMemoryMappedViewHandle.AcquirePointer(ref p);
            try
            {
                return ScanCore(new ReadOnlySpan<byte>(p, checked((int)view.Capacity)));
            }
            finally
            {
                view.SafeMemoryMappedViewHandle.ReleasePointer();
            }
        }
    }

    private static UnityScriptingBackend ScanCore(ReadOnlySpan<byte> data)
    {
        var stringOffset = -1;
        // PE: "MZ" DOS stub, then a "PE\0\0" signature at e_lfanew.
        if (data[0] == 0x4D && data[1] == 0x5A)
            stringOffset = ParsePe(data);
        // ELF: 0x7F 'E' 'L' 'F'
        else if (data[0] == 0x7F && data[1] == 0x45 && data[2] == 0x4C && data[3] == 0x46)
            stringOffset = ParseElf(data);
        else
        {
            // Mach-O magic is read little-endian; fat magics are the byte-swapped on-disk constants.
            var magic = BinaryPrimitives.ReadUInt32LittleEndian(data);
            if (magic == MachO64Magic) stringOffset = ParseMachOSlice(data, 0);
            else if (magic == FatMagic32 || magic == FatMagic64) stringOffset = ParseFat(data, magic);
        }

        if (stringOffset < 0) return UnityScriptingBackend.Unknown;
        return ReadCString(data, stringOffset) switch
        {
            "CoreCLR" => UnityScriptingBackend.CoreCLR,
            "IL2CPP" => UnityScriptingBackend.IL2CPP,
            "Mono" => UnityScriptingBackend.Mono,
            _ => UnityScriptingBackend.Unknown,
        };
    }

    // ---- PE ----

    private static int ParsePe(ReadOnlySpan<byte> data)
    {
        var eLfanew = (int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(0x3C));
        if (eLfanew == 0 || eLfanew + 24 > data.Length) return -1;

        var sig = data.Slice(eLfanew, 4);
        // "MZ" without a PE header (plain DOS stub) -> not a PE image.
        if (!(sig[0] == 'P' && sig[1] == 'E' && sig[2] == 0 && sig[3] == 0)) return -1;

        var coff = eLfanew + 4;
        var numSections = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(coff + 2));
        var sizeOptional = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(coff + 16));
        var optional = coff + 20;
        var optMagic = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(optional));

        int dataDirectories;
        if (optMagic == 0x10B) dataDirectories = optional + 96;       // PE32
        else if (optMagic == 0x20B) dataDirectories = optional + 112; // PE32+
        else return -1;

        // Data directory index 0 is the export table.
        var exportRva = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(dataDirectories));
        var exportSize = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(dataDirectories + 4));
        if (exportRva == 0 || numSections == 0) return -1;

        // Section headers (40 bytes each) follow the optional header.
        var sectionTable = optional + sizeOptional;
        var sections = new PeSection[numSections];
        for (var i = 0; i < numSections; i++)
        {
            var b = sectionTable + i * 40;
            sections[i] = new PeSection
            {
                VirtualSize = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(b + 8)),
                VirtualAddress = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(b + 12)),
                RawSize = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(b + 16)),
                RawPointer = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(b + 20)),
            };
        }

        var exportOffset = RvaToOffset(sections, exportRva);
        if (exportOffset < 0) return -1;

        // IMAGE_EXPORT_DIRECTORY layout.
        var numberOfNames = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(exportOffset + 24));
        var addressOfFunctions = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(exportOffset + 28));
        var addressOfNames = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(exportOffset + 32));
        var addressOfNameOrdinals = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(exportOffset + 36));
        if (numberOfNames == 0) return -1;

        var namesOffset = RvaToOffset(sections, addressOfNames);
        var ordinalsOffset = RvaToOffset(sections, addressOfNameOrdinals);
        var functionsOffset = RvaToOffset(sections, addressOfFunctions);
        if (namesOffset < 0 || ordinalsOffset < 0 || functionsOffset < 0) return -1;

        // Export names are stored in ascending ordinal byte order, so a binary search works;
        // fall back to a linear scan if the table is not sorted as expected.
        var index = BinarySearchExportName(data, sections, namesOffset, numberOfNames);
        if (index < 0) index = LinearSearchExportName(data, sections, namesOffset, numberOfNames);
        if (index < 0) return -1;

        var ordinal = BinaryPrimitives.ReadUInt16LittleEndian(data.Slice(ordinalsOffset + index * 2));
        var functionRva = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(functionsOffset + ordinal * 4));
        if (functionRva == 0) return -1;
        // An RVA that points back inside the export directory denotes a forwarder string, not data.
        if (functionRva >= exportRva && functionRva < exportRva + exportSize) return -1;

        return RvaToOffset(sections, functionRva);
    }

    private static int BinarySearchExportName(ReadOnlySpan<byte> data, PeSection[] sections, int namesOffset, uint count)
    {
        var lo = 0;
        var hi = (int)count - 1;
        while (lo <= hi)
        {
            var mid = lo + ((hi - lo) >> 1);
            var nameRva = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(namesOffset + mid * 4));
            var nameOffset = RvaToOffset(sections, nameRva);
            if (nameOffset < 0) return -1; // table looks broken; let the caller fall back to a linear scan.
            var cmp = CompareCStringAt(data, nameOffset, ourElfPeName);
            if (cmp == 0) return mid;
            if (cmp < 0) lo = mid + 1;
            else hi = mid - 1;
        }
        return -1;
    }

    private static int LinearSearchExportName(ReadOnlySpan<byte> data, PeSection[] sections, int namesOffset, uint count)
    {
        for (var i = 0; i < count; i++)
        {
            var nameRva = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(namesOffset + i * 4));
            var nameOffset = RvaToOffset(sections, nameRva);
            if (nameOffset < 0) continue;
            if (CompareCStringAt(data, nameOffset, ourElfPeName) == 0) return i;
        }
        return -1;
    }

    private struct PeSection
    {
        public uint VirtualSize;
        public uint VirtualAddress;
        public uint RawSize;
        public uint RawPointer;
    }

    private static int RvaToOffset(PeSection[] sections, uint rva)
    {
        foreach (var s in sections)
        {
            var span = s.VirtualSize != 0 ? s.VirtualSize : s.RawSize;
            if (rva >= s.VirtualAddress && rva < s.VirtualAddress + span)
            {
                var delta = rva - s.VirtualAddress;
                if (delta < s.RawSize) return (int)(s.RawPointer + delta);
                return -1; // Lives in the zero-filled tail (not backed by file data).
            }
        }
        return -1;
    }

    // ---- ELF ----

    private const uint SHT_SYMTAB = 2;
    private const uint SHT_DYNSYM = 11;
    private const uint SHT_NOBITS = 8;
    private const ulong SHF_ALLOC = 0x2;

    private static int ParseElf(ReadOnlySpan<byte> data)
    {
        var is64 = data[4] == 2;     // EI_CLASS: 1 = 32-bit, 2 = 64-bit
        var le = data[5] != 2;       // EI_DATA:  1 = little-endian, 2 = big-endian
        if (data[4] != 1 && data[4] != 2) return -1;

        var shoff = is64 ? R64(data, 40, le) : R32(data, 32, le);
        var shentsize = is64 ? R16(data, 58, le) : R16(data, 46, le);
        var shnum = is64 ? R16(data, 60, le) : R16(data, 48, le);
        // Section headers stripped; .dynsym lookup is not available here.
        if (shoff == 0 || shnum == 0 || shentsize == 0) return -1;

        var sht = data.Slice((int)shoff, shnum * shentsize);

        // Prefer the dynamic symbol table (exports survive stripping); fall back to .symtab.
        var symSection = FindElfSection(sht, shnum, shentsize, le, SHT_DYNSYM);
        if (symSection < 0) symSection = FindElfSection(sht, shnum, shentsize, le, SHT_SYMTAB);
        if (symSection < 0) return -1;

        var sym = ReadElfSection(sht, symSection, shentsize, le, is64);
        var strIndex = sym.Link;
        if (strIndex >= shnum) return -1;
        var str = ReadElfSection(sht, (int)strIndex, shentsize, le, is64);

        var symbols = data.Slice((int)sym.Offset, (int)sym.Size);
        var strings = data.Slice((int)str.Offset, (int)str.Size);

        var entrySize = is64 ? 24 : 16;
        if (sym.EntSize != 0) entrySize = (int)sym.EntSize;
        var symCount = entrySize == 0 ? 0 : symbols.Length / entrySize;

        for (var i = 0; i < symCount; i++)
        {
            var b = i * entrySize;
            uint stName;
            ulong stValue;
            ushort stShndx;
            if (is64)
            {
                stName = R32(symbols, b + 0, le);
                stShndx = R16(symbols, b + 6, le);
                stValue = R64(symbols, b + 8, le);
            }
            else
            {
                stName = R32(symbols, b + 0, le);
                stValue = R32(symbols, b + 4, le);
                stShndx = R16(symbols, b + 14, le);
            }

            if (stShndx == 0) continue; // SHN_UNDEF: imported, not exported by this object.
            if (!NameMatchesAt(strings, (int)stName, ourElfPeName)) continue;

            // st_value is a virtual address; map it back to a file offset via the section it lands in.
            return ElfVaddrToOffset(sht, shnum, shentsize, le, is64, stValue);
        }
        return -1;
    }

    private static int FindElfSection(ReadOnlySpan<byte> sht, ushort shnum, ushort shentsize, bool le, uint type)
    {
        for (var i = 0; i < shnum; i++)
            if (R32(sht, i * shentsize + 4, le) == type) return i;
        return -1;
    }

    private struct ElfSectionHeader
    {
        public uint Type;
        public ulong Flags;
        public ulong Addr;
        public ulong Offset;
        public ulong Size;
        public uint Link;
        public ulong EntSize;
    }

    private static ElfSectionHeader ReadElfSection(ReadOnlySpan<byte> sht, int index, ushort shentsize, bool le, bool is64)
    {
        var b = index * shentsize;
        var h = new ElfSectionHeader { Type = R32(sht, b + 4, le) };
        if (is64)
        {
            h.Flags = R64(sht, b + 8, le);
            h.Addr = R64(sht, b + 16, le);
            h.Offset = R64(sht, b + 24, le);
            h.Size = R64(sht, b + 32, le);
            h.Link = R32(sht, b + 40, le);
            h.EntSize = R64(sht, b + 56, le);
        }
        else
        {
            h.Flags = R32(sht, b + 8, le);
            h.Addr = R32(sht, b + 12, le);
            h.Offset = R32(sht, b + 16, le);
            h.Size = R32(sht, b + 20, le);
            h.Link = R32(sht, b + 24, le);
            h.EntSize = R32(sht, b + 36, le);
        }
        return h;
    }

    private static int ElfVaddrToOffset(ReadOnlySpan<byte> sht, ushort shnum, ushort shentsize, bool le, bool is64, ulong vaddr)
    {
        for (var i = 0; i < shnum; i++)
        {
            var h = ReadElfSection(sht, i, shentsize, le, is64);
            if (h.Type == SHT_NOBITS || (h.Flags & SHF_ALLOC) == 0 || h.Addr == 0 || h.Size == 0) continue;
            if (vaddr >= h.Addr && vaddr < h.Addr + h.Size) return (int)(h.Offset + (vaddr - h.Addr));
        }
        return -1;
    }

    // ---- Mach-O ----

    // Native (little-endian) reads of the on-disk magics.
    private const uint MachO64Magic = 0xFEEDFACF;
    private const uint FatMagic32 = 0xBEBAFECA; // on disk 0xCAFEBABE
    private const uint FatMagic64 = 0xBFBAFECA; // on disk 0xCAFEBABF
    private const uint LcSymtab = 0x02;
    private const uint LcSegment64 = 0x19;
    // nlist type-field masks (mach-o/nlist.h).
    private const byte N_STAB = 0xE0; // debug (STABS) entries
    private const byte N_TYPE = 0x0E; // mask for the type bits
    private const byte N_SECT = 0x0E; // defined in the section given by n_sect
    private const byte N_EXT = 0x01;  // external (exported) symbol

    private static int ParseFat(ReadOnlySpan<byte> data, uint magic)
    {
        // Fat header fields are big-endian on disk.
        var archCount = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(4, 4));
        var is64 = magic == FatMagic64;
        var entrySize = is64 ? 32 : 20;

        for (uint i = 0; i < archCount; i++)
        {
            var entry = 8 + (int)i * entrySize;
            // offset field is at byte 8 of a fat_arch / fat_arch_64 entry.
            var sliceOffset = is64
                ? (long)BinaryPrimitives.ReadUInt64BigEndian(data.Slice(entry + 8, 8))
                : BinaryPrimitives.ReadUInt32BigEndian(data.Slice(entry + 8, 4));

            if (sliceOffset <= 0 || sliceOffset + 4 > data.Length) continue;
            var sliceMagic = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice((int)sliceOffset));
            if (sliceMagic == MachO64Magic)
            {
                var result = ParseMachOSlice(data, (int)sliceOffset);
                if (result >= 0) return result;
            }
            // 32-bit slices skipped: no current Unity arch is 32-bit Mach-O.
        }
        return -1;
    }

    // Parses one 64-bit Mach-O image starting at sliceOffset (0 for a thin binary). All file
    // offsets inside the load commands are relative to that slice base.
    private static int ParseMachOSlice(ReadOnlySpan<byte> data, int sliceOffset)
    {
        // mach_header_64 (32 bytes): magic, cputype, cpusubtype, filetype, ncmds, sizeofcmds, flags, reserved.
        var ncmds = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(sliceOffset + 16));
        const int headerSize = 32;

        var segments = new List<(ulong vmaddr, ulong vmsize, ulong fileoff)>();
        var symoff = 0;
        var stroff = 0;
        uint nsyms = 0;
        uint strsize = 0;
        var haveSymtab = false;

        var cursor = sliceOffset + headerSize;
        for (uint i = 0; i < ncmds; i++)
        {
            if (cursor + 8 > data.Length) break;
            var cmd = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(cursor));
            var cmdsize = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(cursor + 4));
            if (cmdsize < 8 || cursor + cmdsize > data.Length) break;

            if (cmd == LcSymtab)
            {
                // symtab_command: cmd(4), cmdsize(4), symoff(4), nsyms(4), stroff(4), strsize(4).
                symoff = (int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(cursor + 8));
                nsyms = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(cursor + 12));
                stroff = (int)BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(cursor + 16));
                strsize = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(cursor + 20));
                haveSymtab = true;
            }
            else if (cmd == LcSegment64)
            {
                // segment_command_64 prefix: cmd(4), cmdsize(4), segname(16), vmaddr(8), vmsize(8), fileoff(8).
                var vmaddr = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(cursor + 24));
                var vmsize = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(cursor + 32));
                var fileoff = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(cursor + 40));
                segments.Add((vmaddr, vmsize, fileoff));
            }

            cursor += (int)cmdsize;
        }

        if (!haveSymtab || nsyms == 0) return -1;

        const int symbolSize = 16; // nlist_64
        var symbols = data.Slice(sliceOffset + symoff, (int)nsyms * symbolSize);
        var strings = data.Slice(sliceOffset + stroff, (int)strsize);

        for (var i = 0; i < nsyms; i++)
        {
            var b = i * symbolSize;
            var nStrx = BinaryPrimitives.ReadUInt32LittleEndian(symbols.Slice(b));
            var nType = symbols[b + 4];
            // n_sect at b+5, n_desc at b+6..7, n_value at b+8..15.
            var nValue = BinaryPrimitives.ReadUInt64LittleEndian(symbols.Slice(b + 8));

            if ((nType & N_STAB) != 0) continue;       // debug (STABS) entry
            if ((nType & N_EXT) == 0) continue;        // must be exported
            if ((nType & N_TYPE) != N_SECT) continue;  // must be defined in a section
            if (!NameMatchesAt(strings, (int)nStrx, ourMachOName) &&
                !NameMatchesAt(strings, (int)nStrx, ourElfPeName))
                continue;

            // n_value is a virtual address; map it back to a file offset via the segment it lands in.
            foreach (var (vmaddr, vmsize, fileoff) in segments)
                if (vmsize != 0 && nValue >= vmaddr && nValue < vmaddr + vmsize)
                    return sliceOffset + (int)(fileoff + (nValue - vmaddr));
            return -1;
        }
        return -1;
    }

    // ---- Low-level helpers ----

    // Reads a NUL-terminated UTF-8 string at the given offset in the mapped view.
    private static string ReadCString(ReadOnlySpan<byte> data, int offset, int max = 256)
    {
        var end = Math.Min(data.Length, offset + max);
        var slice = data.Slice(offset, end - offset);
        var nul = slice.IndexOf((byte)0);
        var len = nul < 0 ? slice.Length : nul;
        return Encoding.UTF8.GetString(slice.Slice(0, len).ToArray());
    }

    // True if 'table' contains 'target' followed by a NUL terminator at the given offset.
    private static bool NameMatchesAt(ReadOnlySpan<byte> table, int offset, ReadOnlySpan<byte> target)
    {
        if (offset < 0 || offset + target.Length >= table.Length) return false;
        return table.Slice(offset, target.Length).SequenceEqual(target) && table[offset + target.Length] == 0;
    }

    // Ordinal comparison of a NUL-terminated C string at data[offset..] against 'target' (no NUL).
    private static int CompareCStringAt(ReadOnlySpan<byte> data, int offset, ReadOnlySpan<byte> target)
    {
        var i = 0;
        const int maxLen = 1024;
        while (true)
        {
            if (offset + i >= data.Length) return 1; // unexpected end of data
            var nameByte = data[offset + i];
            var nameEnded = nameByte == 0;
            var targetEnded = i >= target.Length;

            if (nameEnded && targetEnded) return 0;
            if (nameEnded) return -1; // name is a prefix of target
            if (targetEnded) return 1; // target is a prefix of name
            if (nameByte != target[i]) return nameByte < target[i] ? -1 : 1;

            i++;
            if (i > maxLen) return 1; // pathologically long name; keep the search bounded.
        }
    }

    private static ushort R16(ReadOnlySpan<byte> b, int o, bool le) => le
        ? BinaryPrimitives.ReadUInt16LittleEndian(b.Slice(o))
        : BinaryPrimitives.ReadUInt16BigEndian(b.Slice(o));

    private static uint R32(ReadOnlySpan<byte> b, int o, bool le) => le
        ? BinaryPrimitives.ReadUInt32LittleEndian(b.Slice(o))
        : BinaryPrimitives.ReadUInt32BigEndian(b.Slice(o));

    private static ulong R64(ReadOnlySpan<byte> b, int o, bool le) => le
        ? BinaryPrimitives.ReadUInt64LittleEndian(b.Slice(o))
        : BinaryPrimitives.ReadUInt64BigEndian(b.Slice(o));
}
