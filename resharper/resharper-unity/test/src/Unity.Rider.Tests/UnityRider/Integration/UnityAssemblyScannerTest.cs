using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using JetBrains.ReSharper.Plugins.Unity.Rider.Integration;
using JetBrains.Rider.Model.Unity.FrontendBackend;
using NUnit.Framework;

namespace JetBrains.ReSharper.Plugins.Unity.Rider.Tests.UnityRider.Integration
{
    // Unity stamps an exported UnityScriptingBackend symbol into its native binaries, and
    // UnityAssemblyScanner reads it. Unity 6000.7 is the first version that exports the symbol, so a
    // player from an older version cannot exercise the parser at all. These tests build the smallest
    // Mach-O image the parser accepts, so every assertion states exactly which bytes produce which
    // answer.
    //
    // Mach-O only. The PE and the ELF parsers have no test, because this machine can neither build
    // nor validate such an image. See RIDER-119841.
    [TestFixture]
    public class UnityAssemblyScannerTest
    {
        [TestCase("IL2CPP", UnityScriptingBackend.IL2CPP)]
        [TestCase("Mono", UnityScriptingBackend.Mono)]
        [TestCase("CoreCLR", UnityScriptingBackend.CoreCLR)]
        public void Scan_ReadsTheBackendFromAThinImage(string backend, UnityScriptingBackend expected)
        {
            Assert.AreEqual(expected, Scan(MachO.Thin(backend)));
        }

        [Test]
        public void Scan_ReadsTheBackendFromAFatImage()
        {
            Assert.AreEqual(UnityScriptingBackend.CoreCLR, Scan(MachO.Fat(MachO.Thin("CoreCLR"))));
        }

        [Test]
        public void Scan_ReturnsUnknown_WhenTheValueIsNotABackendName()
        {
            Assert.AreEqual(UnityScriptingBackend.Unknown, Scan(MachO.Thin("Potato")));
        }

        [Test]
        public void Scan_ReturnsUnknown_WhenNothingExportsTheSymbol()
        {
            Assert.AreEqual(UnityScriptingBackend.Unknown,
                            Scan(MachO.Thin("IL2CPP", symbolName: "_SomethingElse")));
        }

        // Every Unity binary holds the mangled C++ constant
        // UnityEngine::Insights::OtelConstants::kOtelUnityScriptingBackend, which contains the symbol
        // name as a substring. A raw byte search finds it and reports a backend that is not there.
        // NameMatchesAt demands a NUL terminator, so the parser must reject it.
        [Test]
        public void Scan_ReturnsUnknown_WhenTheSymbolNameIsOnlyASubstring()
        {
            const string otelConstant =
                "__ZN10UnityEngine8Insights13OtelConstants26kOtelUnityScriptingBackendE";
            Assert.AreEqual(UnityScriptingBackend.Unknown,
                            Scan(MachO.Thin("IL2CPP", symbolName: otelConstant)));
        }

        // The Mach-O ABI emits a C symbol with a leading underscore, but the parser accepts the bare
        // name too. Both spellings must answer.
        [TestCase("_UnityScriptingBackend")]
        [TestCase("UnityScriptingBackend")]
        public void Scan_AcceptsBothSpellingsOfTheSymbol(string symbolName)
        {
            Assert.AreEqual(UnityScriptingBackend.IL2CPP, Scan(MachO.Thin("IL2CPP", symbolName)));
        }

        [Test]
        public void Scan_ReturnsUnknown_ForAFileThatIsNotAnImage()
        {
            Assert.AreEqual(UnityScriptingBackend.Unknown, Scan(Encoding.ASCII.GetBytes("not a binary")));
        }

        private static UnityScriptingBackend Scan(byte[] image)
        {
            var path = Path.Combine(Path.GetTempPath(), "UnityAssemblyScannerTest-" + Guid.NewGuid().ToString("N"));
            try
            {
                File.WriteAllBytes(path, image);
                return UnityAssemblyScanner.TryScan(path).Result;
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        // Builds the smallest 64-bit Mach-O image that UnityAssemblyScanner.ParseMachOSlice accepts:
        // a mach_header_64, one LC_SEGMENT_64 that covers the whole file, and one LC_SYMTAB.
        // The segment starts at virtual address 0 and at file offset 0, so a symbol value equals a
        // file offset, which keeps the fixture readable.
        private static class MachO
        {
            private const uint Magic64 = 0xFEEDFACF;
            private const int CpuTypeArm64 = 0x0100000C;
            private const uint FileTypeDylib = 6;
            private const uint LcSegment64 = 0x19;
            private const uint LcSymtab = 0x02;
            private const byte NSectExt = 0x0F; // N_SECT | N_EXT: defined in a section, and exported.

            private const int HeaderSize = 32;
            private const int SegmentSize = 72;
            private const int SymtabSize = 24;
            private const int SymbolSize = 16; // nlist_64
            private const int PayloadStart = HeaderSize + SegmentSize + SymtabSize;

            public static byte[] Thin(string backend, string symbolName = "_UnityScriptingBackend")
            {
                // The string table starts with an empty string, so index 0 means "no name".
                var strings = new List<byte> { 0 };
                var nameOffset = strings.Count;
                strings.AddRange(Encoding.ASCII.GetBytes(symbolName));
                strings.Add(0);

                var stringOffset = PayloadStart + SymbolSize;
                var backendOffset = stringOffset + strings.Count;
                var total = backendOffset + Encoding.ASCII.GetByteCount(backend) + 1;

                var image = new MemoryStream();
                var w = new BinaryWriter(image);

                // mach_header_64
                w.Write(Magic64);
                w.Write(CpuTypeArm64);
                w.Write(0);                            // cpusubtype
                w.Write(FileTypeDylib);
                w.Write(2u);                           // ncmds
                w.Write((uint)(SegmentSize + SymtabSize));
                w.Write(0u);                           // flags
                w.Write(0u);                           // reserved

                // segment_command_64, with no sections
                w.Write(LcSegment64);
                w.Write((uint)SegmentSize);
                w.Write(SegmentName("__DATA"));
                w.Write(0UL);                          // vmaddr
                w.Write((ulong)total);                 // vmsize
                w.Write(0UL);                          // fileoff
                w.Write((ulong)total);                 // filesize
                w.Write(7);                            // maxprot
                w.Write(3);                            // initprot
                w.Write(0u);                           // nsects
                w.Write(0u);                           // flags

                // symtab_command
                w.Write(LcSymtab);
                w.Write((uint)SymtabSize);
                w.Write((uint)PayloadStart);           // symoff
                w.Write(1u);                           // nsyms
                w.Write((uint)stringOffset);           // stroff
                w.Write((uint)strings.Count);          // strsize

                // nlist_64, pointing at the backend string
                w.Write((uint)nameOffset);             // n_strx
                w.Write(NSectExt);                     // n_type
                w.Write((byte)1);                      // n_sect
                w.Write((ushort)0);                    // n_desc
                w.Write((ulong)backendOffset);         // n_value

                w.Write(strings.ToArray());
                w.Write(Encoding.ASCII.GetBytes(backend));
                w.Write((byte)0);
                w.Flush();
                return image.ToArray();
            }

            // Wraps slices in a fat image. Fat header fields are big-endian on disk.
            public static byte[] Fat(params byte[][] slices)
            {
                const int sliceAlignment = 0x1000;
                var headerSize = 8 + slices.Length * 20;
                var firstSlice = (headerSize + sliceAlignment - 1) / sliceAlignment * sliceAlignment;

                var image = new MemoryStream();
                var w = new BinaryWriter(image);
                w.Write(BigEndian(0xCAFEBABE));
                w.Write(BigEndian((uint)slices.Length));

                var offset = firstSlice;
                for (var i = 0; i < slices.Length; i++)
                {
                    w.Write(BigEndian((uint)(CpuTypeArm64 + i))); // a distinct cputype per slice
                    w.Write(BigEndian(0u));                       // cpusubtype
                    w.Write(BigEndian((uint)offset));
                    w.Write(BigEndian((uint)slices[i].Length));
                    w.Write(BigEndian(12u));                      // align, as a power of two
                    offset += Align(slices[i].Length, sliceAlignment);
                }

                w.Write(new byte[firstSlice - (int)image.Length]);
                foreach (var slice in slices)
                {
                    w.Write(slice);
                    w.Write(new byte[Align(slice.Length, sliceAlignment) - slice.Length]);
                }

                w.Flush();
                return image.ToArray();
            }

            private static int Align(int value, int alignment) =>
                (value + alignment - 1) / alignment * alignment;

            private static uint BigEndian(uint value) =>
                (value << 24) | ((value & 0xFF00) << 8) | ((value >> 8) & 0xFF00) | (value >> 24);

            private static byte[] SegmentName(string name)
            {
                var bytes = new byte[16];
                Encoding.ASCII.GetBytes(name, 0, name.Length, bytes, 0);
                return bytes;
            }
        }
    }
}
