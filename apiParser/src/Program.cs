using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace ApiParser
{
    public static class Program
    {
        private static readonly string ScriptReferenceRelativePath = @"Documentation" + Path.DirectorySeparatorChar + "en" + Path.DirectorySeparatorChar + "ScriptReference";

        private static readonly IList<Tuple<string, Version>> Docs = new List<Tuple<string, Version>>
        {
            // These folders need to live in the runtime folder
            // Can't redistribute, sorry. See README.md
            Tuple.Create("Documentation-5.0.4f1", new Version(5, 0)),
            Tuple.Create("Documentation-5.1.5f1", new Version(5, 1)),
            Tuple.Create("Documentation-5.2.5f1", new Version(5, 2)),
            Tuple.Create("Documentation-5.3.8f2", new Version(5, 3)),
            Tuple.Create("Documentation-5.4.6f3", new Version(5, 4)),
            Tuple.Create("Documentation-5.5.5f1", new Version(5, 5)),
            Tuple.Create("Documentation-5.6.3f1", new Version(5, 6)),
            Tuple.Create("Documentation-2017.1.2f1", new Version(2017, 1)),
            Tuple.Create("Documentation-2017.2.0f2", new Version(2017, 2)),
            Tuple.Create("Documentation-2017.3.0b3", new Version(2017, 3)),
            Tuple.Create("Documentation-2018.1.0b9", new Version(2018, 1))
        };

        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: ApiParser.exe docsFolder");
                Console.WriteLine();
                Console.WriteLine("  docsFolder - folder that contains all versions of Unity docs");
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            Directory.SetCurrentDirectory(args[0]);

            var progPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var managedPath = Path.Combine(progPath, "Unity", "Editor", "Data", "Managed");
            if (!Directory.Exists(managedPath))
            {
                // TODO: Find the latest version rather than hardcode it
                // TODO: Handle this in Windows, too
                //managedPath = Path.Combine(progPath, "Unity", "Hub", "Editor", "2018.1.0b9", "Unity.app", "Contents", "Managed");
                managedPath = Path.Combine(progPath, "Unity", "Hub", "Editor", "2017.3.1f1", "Unity.app", "Contents", "Managed");
            }

            // Add assemblies to the type resolver so we can get the fully qualified names of types
            // The Unity docs only give us the short names
            TypeResolver.AddAssembly(Assembly.LoadFrom(Path.Combine(managedPath, @"UnityEngine.dll")));
            TypeResolver.AddAssembly(Assembly.LoadFrom(Path.Combine(managedPath, @"UnityEditor.dll")));
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
                AddUndocumentedApis(unityApi, doc.Item2);
            }
            AddUndocumentedOptionalParameters(unityApi);
            AddUndocumentedCoroutines(unityApi);
            Fixup(unityApi);

            using (var writer = new XmlTextWriter(@"api.xml", Encoding.UTF8) {Formatting = Formatting.Indented})
            {
                parser.ExportTo(writer);
            }

            Console.WriteLine("Done. Elapsed time: {0}", stopwatch.Elapsed);

            // Console.WriteLine( "Press <Enter> key to continue..." );
            // Console.ReadLine();
        }

        private static void AddUndocumentedCoroutines(UnityApi unityApi)
        {
            Console.WriteLine("Adding undocumented coroutines");

            var type = unityApi.FindType("MonoBehaviour");
            if (type != null)
            {
                // Not documented directly, but shown in examples
                // https://docs.unity3d.com/ScriptReference/MonoBehaviour.StartCoroutine.html
                // https://docs.unity3d.com/ScriptReference/WaitForEndOfFrame.html
                SetIsCoroutine(type, "Start");

                // Not documented as co-routines, but the non-2D versions are
                SetIsCoroutine(type, "OnCollisionEnter2D");
                SetIsCoroutine(type, "OnCollisionExit2D");
                SetIsCoroutine(type, "OnCollisionStay2D");
                SetIsCoroutine(type, "OnTriggerEnter2D");
                SetIsCoroutine(type, "OnTriggerExit2D");
                SetIsCoroutine(type, "OnTriggerStay2D");
            }
        }

        private static void SetIsCoroutine(UnityApiType type, string functionName)
        {
            foreach (var function in type.FindEventFunctions(functionName))
                function.SetIsCoroutine();
        }

        private static void AddUndocumentedOptionalParameters(UnityApi unityApi)
        {
            Console.WriteLine("Adding undocumented optional parameters");

            // TODO: Would this be better to mark the parameter as optional?
            // Then add an inspection to see if the optional parameter is used in the body of the method
            var type = unityApi.FindType("MonoBehaviour");
            if (type != null)
            {
                // Not formally documented, but described in the text
                const string justification = "Removing collision parameter avoids unnecessary calculations";
                MakeParameterOptional(type, "OnCollisionEnter", "other", justification);
                MakeParameterOptional(type, "OnCollisionEnter2D", "other", justification);
                MakeParameterOptional(type, "OnCollisionExit", "other", justification);
                MakeParameterOptional(type, "OnCollisionExit2D", "other", justification);
                MakeParameterOptional(type, "OnCollisionStay", "other", justification);
                MakeParameterOptional(type, "OnCollisionStay2D", "other", justification);
            }
        }

        private static void MakeParameterOptional(UnityApiType type, string functionName, string parameterName, string justification)
        {
            foreach (var function in type.FindEventFunctions(functionName))
                function.MakeParameterOptional(parameterName, justification);
        }

        private static void Fixup(UnityApi unityApi)
        {
            // Documentation doesn't state that it's static, or has wrong types
            Console.WriteLine("Fixing incorrect documentation");

            var type = unityApi.FindType("AssetModificationProcessor");
            if (type != null)
            {
                // Not part of the actual documentation
                foreach (var function in type.FindEventFunctions("IsOpenForEdit"))
                {
                    function.SetIsStatic();
                    function.SetReturnType(ApiType.Bool);
                    var newParameter = new UnityApiParameter("assetPath", ApiType.String, string.Empty);
                    function.UpdateParameter("arg1", newParameter);
                    newParameter = new UnityApiParameter("message", ApiType.StringByRef, string.Empty);
                    function.UpdateParameter("arg2", newParameter);
                }

                foreach (var function in type.FindEventFunctions("OnWillCreateAsset"))
                {
                    function.SetIsStatic();
                    var newParameter = new UnityApiParameter("assetPath", ApiType.String, string.Empty);
                    function.UpdateParameter("arg", newParameter);
                }

                foreach (var function in type.FindEventFunctions("OnWillDeleteAsset"))
                {
                    function.SetIsStatic();
                    function.SetReturnType(new ApiType("UnityEditor.AssetDeleteResult"));
                    var newParameter = new UnityApiParameter("assetPath", ApiType.String, string.Empty);
                    function.UpdateParameter("arg1", newParameter);
                    newParameter = new UnityApiParameter("options", new ApiType("UnityEditor.RemoveAssetOptions"), string.Empty);
                    function.UpdateParameter("arg2", newParameter);
                }

                foreach (var function in type.FindEventFunctions("OnWillMoveAsset"))
                {
                    function.SetIsStatic();
                    function.SetReturnType(new ApiType("UnityEditor.AssetMoveResult"));
                    var newParameter = new UnityApiParameter("fromPath", ApiType.String, string.Empty);
                    function.UpdateParameter("arg1", newParameter);
                    newParameter = new UnityApiParameter("toPath", ApiType.String, string.Empty);
                    function.UpdateParameter("arg2", newParameter);
                }
            }
        }

        private static void AddUndocumentedApis(UnityApi unityApi, Version apiVersion)
        {
            // From AssetPostprocessingInternal
            var type = unityApi.FindType("AssetPostprocessor");
            if (type != null)
            {
                var eventFunction = new UnityApiEventFunction("OnPreprocessAssembly",
                    false, false, ApiType.Void, apiVersion, undocumented: true);
                eventFunction.AddParameter("pathName", ApiType.String);
                type.MergeEventFunction(eventFunction, apiVersion);

                eventFunction = new UnityApiEventFunction("OnGeneratedCSProjectFiles",
                    true, false, ApiType.Void, apiVersion, undocumented: true);
                type.MergeEventFunction(eventFunction, apiVersion);

                // Technically, return type is optional
                eventFunction = new UnityApiEventFunction("OnPreGeneratingCSProjectFiles",
                    true, false, ApiType.Bool, apiVersion, undocumented: true);
                type.MergeEventFunction(eventFunction, apiVersion);
            }

            // From AssetModificationProcessorInternal
            type = unityApi.FindType("AssetModificationProcessor");
            if (type != null)
            {
                var eventFunction = new UnityApiEventFunction("OnStatusUpdated", true,
                    false, ApiType.Void, apiVersion, undocumented: true);
                type.MergeEventFunction(eventFunction, apiVersion);
            }

            // ScriptableObject
            // From Shawn White @ Unity (https://github.com/JetBrains/resharper-unity/issues/79#issuecomment-266727851):
            // OnValidate's behavior on ScriptableObject is the same as on MonoBehaviour. OnValidate is a non-static
            // method which is invoked from native and isn't picky about visibility (it'll get invoked regardless of
            // visibility). OnValidate is different from the other magic methods in that it only gets invoked from
            // the Editor. A good practice is to wrap OnValidate with #if UNITY_EDITOR so that the function can be
            // stripped out for deployment.
            //
            // To expand on ScriptableObject a bit. Native Unity code doesn't distinguish between MonoBehaviour and
            // ScriptableObject. They are the same native type (it's confusing, I know, and everyone gets tripped up
            // by it that looks at the native code base). The only difference is in how the object is used,
            // MonoBehaviours live on GameObjects, while ScriptableObjects can live alone.So in general, all magic
            // methods that would make sense without a GameObject context should work and be implemented for
            // ScriptableObjects. Off the top of my head this includes, Awake, OnEnable, OnDisable, OnDestroy,
            // OnValidate, and Reset, but there could be more.
            type = unityApi.FindType("ScriptableObject");
            if (type != null)
            {
                var eventFunction = new UnityApiEventFunction("OnValidate", false, false, ApiType.Void, apiVersion,
                    description:
                    "This function is called when the script is loaded or a value is changed in the inspector (Called in the editor only).",
                    undocumented: true);
                type.MergeEventFunction(eventFunction, apiVersion);

                eventFunction = new UnityApiEventFunction("Reset", false, false, ApiType.Void, apiVersion,
                    description: "Reset to default values.", undocumented: true);
                type.MergeEventFunction(eventFunction, apiVersion);
            }
        }
    }
}
