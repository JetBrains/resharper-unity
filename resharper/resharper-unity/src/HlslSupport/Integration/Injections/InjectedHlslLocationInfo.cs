using JetBrains.ReSharper.Psi.Cpp.Caches;
using JetBrains.Serialization;
using JetBrains.Util;
using JetBrains.Util.PersistentMap;

namespace JetBrains.ReSharper.Plugins.Unity.HlslSupport.Integration.Injections
{
    public class InjectedHlslLocationInfo
    {
        public InjectedHlslLocationInfo(FileSystemPath path, TextRange range, InjectedHlslProgramType programType)
        {
            FileSystemPath = path;
            Range = range;
            ProgramType = programType;
        }

        public static InjectedHlslLocationInfo Read(UnsafeReader reader)
        {
            return new InjectedHlslLocationInfo(UnsafeMarshallers.FileSystemPathMarshaller.Unmarshal(reader), new TextRange(reader.ReadInt(), reader.ReadInt()), 
                reader.ReadEnum(InjectedHlslProgramType.Uknown));

        }

        public static void Write(UnsafeWriter writer, InjectedHlslLocationInfo value)
        {
            UnsafeMarshallers.FileSystemPathMarshaller.Marshal(writer, value.FileSystemPath);
            writer.Write(value.Range.StartOffset);
            writer.Write(value.Range.EndOffset);
            writer.WriteEnum(value.ProgramType);
        }

        public CppFileLocation ToCppFileLocation()
        {
            return new CppFileLocation(new FileSystemPathWithRange(FileSystemPath, Range));
        }

        public TextRange Range { get; }
        public FileSystemPath FileSystemPath { get; }
        public InjectedHlslProgramType ProgramType { get; }

        protected bool Equals(InjectedHlslLocationInfo other)
        {
            return Range.Equals(other.Range) && FileSystemPath.Equals(other.FileSystemPath) && ProgramType == other.ProgramType;
        }

        public override bool Equals(object obj)
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