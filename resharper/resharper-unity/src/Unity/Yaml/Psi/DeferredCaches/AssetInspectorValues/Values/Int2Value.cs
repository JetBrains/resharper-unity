using JetBrains.Annotations;
using JetBrains.Application.PersistentMap;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.Serialization;

namespace JetBrains.ReSharper.Plugins.Unity.Yaml.Psi.DeferredCaches.AssetInspectorValues.Values
{
    [PolymorphicMarshaller]
    public class Int2Value : IAssetValue
    {
        
        [UsedImplicitly] 
        public static UnsafeReader.ReadDelegate<object> ReadDelegate = Read;

        private static object Read(UnsafeReader reader) => new Int2Value(reader.ReadInt(), reader.ReadInt());

        [UsedImplicitly]
        public static UnsafeWriter.WriteDelegate<object> WriteDelegate = (w, o) => Write(w, o as Int2Value);

        private static void Write(UnsafeWriter writer, Int2Value value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
        }

        public int X { get; }
        public int Y { get; }

        public Int2Value(int x, int y)
        {
            X = x;
            Y = y;
        }
        
        public string GetPresentation(ISolution solution, IDeclaredElement declaredElement, bool prefabImport)
        {
            return $"x: {X}, y: {Y}";
        }

        public string GetFullPresentation(ISolution solution, IDeclaredElement declaredElement, bool prefabImport)
        {
            return GetPresentation(solution, declaredElement, prefabImport);
        }
    }
}