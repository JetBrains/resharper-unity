using System.Linq;
using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.Application.Threading;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Util;
using JetBrains.ReSharper.Psi.Util;
using JetBrains.Serialization;
using JetBrains.Util;
using static JetBrains.Serialization.UnsafeWriter;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values
{
    [PolymorphicMarshaller]
    public class AssetSimpleValue : IAssetValue
    {
        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader) => new AssetSimpleValue(reader.ReadString());

        [UsedImplicitly]
        public static WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as AssetSimpleValue);

        private static void Write(UnsafeWriter writer, AssetSimpleValue value)
        {
            writer.Write(value.SimpleValue);
        }

        public AssetSimpleValue(string value)
        {
            SimpleValue = value ?? string.Empty;
        }

        protected bool Equals(AssetSimpleValue other)
        {
            return SimpleValue == other.SimpleValue;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AssetSimpleValue) obj);
        }

        public override int GetHashCode()
        {
            return SimpleValue.GetPlatformIndependentHashCode();
        }

        private string GetPresentationInner(ISolution solution, IDeclaredElement declaredElement, bool prefabImport, bool makeTextShorter)
        {
            solution.GetComponent<IShellLocks>().AssertReadAccessAllowed();
            var type = declaredElement.Type();
            if (type == null)
                return "...";
            if (type.IsBool())
            {
                if (SimpleValue.Equals("0"))
                    return "\"false\"";
                return "\"true\"";
            }

            if (type.IsEnumType())
            {
                if (!int.TryParse(SimpleValue, out var result))
                    return  "...";
                var @enum = type.GetTypeElement() as IEnum;
                var enumMemberType = @enum?.EnumMembers.FirstOrDefault()?.ConstantValue.Type;
                if (enumMemberType == null)
                    return "...";

                var enumConstantValue = ConstantValue.Enum(ConstantValue.Int(result, type.Module), enumMemberType);
                var enumMembers = CSharpEnumUtil.CalculateEnumMembers(enumConstantValue, @enum);

                return string.Join(" | ", enumMembers.Select(t => t.ShortName));
            }

            var textPresentation = SimpleValue;
            if (makeTextShorter && textPresentation != null)
                textPresentation = StringUtil.DropMiddleIfLong(textPresentation, 30);
            return $"\"{textPresentation ?? "..." }\"";
        }

        public string GetPresentation(ISolution solution, IDeclaredElement declaredElement, bool prefabImport) => GetPresentationInner(solution, declaredElement, prefabImport, true);
        public string GetFullPresentation(ISolution solution, IDeclaredElement declaredElement, bool prefabImport) => GetPresentationInner(solution, declaredElement, prefabImport, false);

        public string SimpleValue { get; }
    }
}