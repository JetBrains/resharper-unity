#nullable enable

using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.Serialization;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.Shaders.HlslSupport.Integration.Injections
{
    public class InjectedHlslLocationInfo
    {
        public InjectedHlslLocationInfo(VirtualFileSystemPath path, TextRange range, InjectedHlslProgramType programType)
        {
            FileSystemPath = path;
            Range = range;
            ProgramType = programType;
        }

        public static InjectedHlslLocationInfo Read(UnsafeReader reader)
        {
            return new InjectedHlslLocationInfo(
                UnsafeMarshallers.VirtualFileSystemPathCurrentSolutionCorrectCaseMarshaller.Unmarshal(reader),
                new TextRange(reader.ReadInt32(), reader.ReadInt32()), reader.ReadEnum(InjectedHlslProgramType.Unknown));
        }

        public static void Write(UnsafeWriter writer, InjectedHlslLocationInfo value)
        {
            UnsafeMarshallers.VirtualFileSystemPathCurrentSolutionCorrectCaseMarshaller.Marshal(writer, value.FileSystemPath);
            writer.WriteInt32(value.Range.StartOffset);
            writer.WriteInt32(value.Range.EndOffset);
            writer.WriteEnum(value.ProgramType);
        }

        public CppFileLocation ToCppFileLocation()
        {
            return new CppFileLocation(new FileSystemPathWithRange(FileSystemPath, Range));
        }

        public TextRange Range { get; }
        public VirtualFileSystemPath FileSystemPath { get; }
        public InjectedHlslProgramType ProgramType { get; }

        private bool Equals(InjectedHlslLocationInfo other)
        {
            return Range.Equals(other.Range) && FileSystemPath.Equals(other.FileSystemPath) && ProgramType == other.ProgramType;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((InjectedHlslLocationInfo) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Range.GetHashCode();
                hashCode = (hashCode * 397) ^ FileSystemPath.GetHashCode();
                hashCode = (hashCode * 397) ^ (int) ProgramType;
                return hashCode;
            }
        }
    }
}
