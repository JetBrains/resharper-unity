using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace ApiParser
{
    public static class Program
    {
        private static int ourProgress;

        public static void Main(string[] args)
        {
            var progPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var dataPath = Path.Combine(progPath, @"Unity\Editor\Data");
            var monoPath = Path.Combine(dataPath, @"Mono\lib\mono\unity");
            var basePath = Path.Combine(dataPath, @"Managed");
            TypeResolver.AddAssembly(typeof(string).Assembly);
            TypeResolver.AddAssembly(Assembly.LoadFrom(Path.Combine(basePath, @"UnityEngine.dll")));
            TypeResolver.AddAssembly(Assembly.LoadFrom(Path.Combine(monoPath, @"UnityScript.dll")));
            TypeResolver.AddAssembly(Assembly.LoadFrom(Path.Combine(basePath, @"UnityEditor.dll")));

            Console.WriteLine();
            var path = Path.Combine(dataPath, @"Documentation\en\ScriptReference");

            var unityApi = new UnityApi();
            var parser = new ApiParser(path, unityApi);

            parser.Progress += (s, e) =>
            {
                if (e.Percent <= ourProgress) return;

                ourProgress = e.Percent;
                var cursorTop = Console.CursorTop;
                Console.WriteLine("{0,5} / {1,5} ({2,3}%)", e.Current, e.Total, e.Percent);
                Console.SetCursorPosition(0, cursorTop);
            };

            parser.ParseFolder();

            AddUndocumentApis(unityApi);

            using (var writer = new XmlTextWriter(@"api.xml", Encoding.UTF8) {Formatting = Formatting.Indented})
            {
                parser.ExportTo(writer);
            }

            // Console.WriteLine( "Press <Enter> key to continue..." );
            // Console.ReadLine();
        }

        private static void AddUndocumentApis(UnityApi unityApi)
        {
            // From AssetPostprocessingInternal
            var type = unityApi.FindType("AssetPostprocessor");
            var eventFunction = type.AddEventFunction("OnPostprocessAllAssets", true, ApiType.Void, undocumented: true);
            eventFunction.AddParameter("importedAssets", ApiType.StringArray);
            eventFunction.AddParameter("deletedAssets", ApiType.StringArray);
            eventFunction.AddParameter("movedAssets", ApiType.StringArray);
            eventFunction.AddParameter("movedFromPathAssets", ApiType.StringArray);

            eventFunction = type.AddEventFunction("OnPreprocessAssembly", false, ApiType.Void, undocumented: true);
            eventFunction.AddParameter("pathName", ApiType.String);

            type.AddEventFunction("OnGeneratedCSProjectFiles", true, ApiType.Void, undocumented: true);

            // Technically, return type is optional
            type.AddEventFunction("OnPreGeneratingCSProjectFiles", true, ApiType.Bool, undocumented: true);

            // From AssetModificationProcessorInternal
            type = unityApi.FindType("AssetModificationProcessor");
            type.AddEventFunction("OnStatusUpdated", true, ApiType.Void, undocumented: true);
        }
    }

}
