using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace ApiParser
{
    public static class Program
    {
        private const string ScriptReferenceRelativePath = @"Documentation\en\ScriptReference";

        private static readonly IList<Tuple<string, Version>> Docs = new List<Tuple<string, Version>>
        {
            // These folders need to live in the runtime folder
            // Can't redistribute, sorry
            Tuple.Create("Documentation-5.2.3f1", new Version(5, 2)),
            Tuple.Create("Documentation-5.3.7f1", new Version(5, 3)),
            Tuple.Create("Documentation-5.4.3f1", new Version(5, 4)),
            Tuple.Create("Documentation-5.5.0f3", new Version(5, 5))
        };

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

            var unityApi = new UnityApi();
            var parser = new ApiParser(unityApi, ScriptReferenceRelativePath);

            parser.Progress += (s, e) =>
            {
                var cursorTop = Console.CursorTop;
                Console.WriteLine("{0,5} / {1,5} ({2,3}%)", e.Current, e.Total, e.Percent);
                Console.SetCursorPosition(0, cursorTop);
            };

            foreach (var doc in Docs)
            {
                Console.WriteLine(doc.Item1);
                parser.ParseFolder(doc.Item1, doc.Item2);
                AddUndocumentApis(unityApi, doc.Item2);
            }

            using (var writer = new XmlTextWriter(@"api.xml", Encoding.UTF8) {Formatting = Formatting.Indented})
            {
                parser.ExportTo(writer);
            }

            // Console.WriteLine( "Press <Enter> key to continue..." );
            // Console.ReadLine();
        }

        private static void AddUndocumentApis(UnityApi unityApi, Version apiVersion)
        {
            // From AssetPostprocessingInternal
            var type = unityApi.FindType("AssetPostprocessor");

            var eventFunction = new UnityApiEventFunction("OnPreprocessAssembly", false, ApiType.Void, apiVersion, undocumented: true);
            eventFunction.AddParameter("pathName", ApiType.String);
            type.MergeEventFunction(eventFunction, apiVersion);

            eventFunction = new UnityApiEventFunction("OnGeneratedCSProjectFiles", true, ApiType.Void, apiVersion, undocumented: true);
            type.MergeEventFunction(eventFunction, apiVersion);

            // Technically, return type is optional
            eventFunction = new UnityApiEventFunction("OnPreGeneratingCSProjectFiles", true, ApiType.Bool, apiVersion,  undocumented: true);
            type.MergeEventFunction(eventFunction, apiVersion);

            // From AssetModificationProcessorInternal
            type = unityApi.FindType("AssetModificationProcessor");
            eventFunction = new UnityApiEventFunction("OnStatusUpdated", true, ApiType.Void, apiVersion, undocumented: true);
            type.MergeEventFunction(eventFunction, apiVersion);
        }
    }

}
