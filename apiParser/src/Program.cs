﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using JetBrains.Util;

namespace ApiParser
{
    public static class Program
    {
        private static readonly List<(string, Version)> DocVersions = new List<(string, Version)>
        {
            // These folders need to live in the runtime folder
            // Can't redistribute, sorry. See README.md
            ("Documentation-5.0.4f1", new Version(5, 0)),
            ("Documentation-5.1.5f1", new Version(5, 1)),
            ("Documentation-5.2.5f1", new Version(5, 2)),
            ("Documentation-5.3.8f2", new Version(5, 3)),
            ("Documentation-5.4.6f3", new Version(5, 4)),
            ("Documentation-5.5.5f1", new Version(5, 5)),
            ("Documentation-5.6.3f1", new Version(5, 6)),
            ("Documentation-2017.1.2f1", new Version(2017, 1)),
            ("Documentation-2017.2.0f2", new Version(2017, 2)),
            ("Documentation-2017.3.1f1", new Version(2017, 3)),
            ("Documentation-2017.4.6f1", new Version(2017, 4)),
            ("Documentation-2018.1.6f1", new Version(2018, 1))
        };

        public static void Main(string[] args)
        {
            if (args.Length != 1 && args.Length != 2)
            {
                Console.WriteLine("Usage: ApiParser.exe docsFolder");
                Console.WriteLine("       ApiParser.exe apiXmlPath version");
                Console.WriteLine();
                Console.WriteLine("  docsFolder - folder that contains all versions of Unity docs");
                Console.WriteLine("  apiXmlPath - location of api.xml to read and merge into");
                Console.WriteLine("  version - version of Unity to read docs from. Must be installed in standard Unity Hub location");
                Console.WriteLine();
                Console.WriteLine("Note that the output file is written to the current directory");
                return;
            }

            var stopwatch = Stopwatch.StartNew();

            var docVersions = DocVersions;
            var latestVersion = Regex.Match(docVersions.Last().Item1, @"Documentation-(.*)$").Groups[1].Value;
            var apiXml = FileSystemPath.Empty;

            if (args.Length == 1)
                Directory.SetCurrentDirectory(args[0]);
            else
            {
                apiXml = FileSystemPath.ParseRelativelyTo(args[0], FileSystemPath.Parse(Directory.GetCurrentDirectory()));
                if (!apiXml.ExistsFile)
                    throw new InvalidOperationException("api.xml path does not exist");
                latestVersion = args[1];
                var docRoot = GetDocumentationRoot(latestVersion);
                if (!docRoot.ExistsDirectory)
                    throw new InvalidOperationException($"Cannot find locally installed docs: {docRoot}");
                var parseableVersion = Regex.Match(latestVersion, @"^(\d+\.\d+)").Groups[1].Value;
                docVersions = new List<(string, Version)> {(docRoot.FullPath, Version.Parse(parseableVersion))};
            }

            var progPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var managedPath = Path.Combine(progPath, "Unity", "Editor", "Data", "Managed");
            if (!Directory.Exists(managedPath))
            {
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                    managedPath = Path.Combine(progPath, "Unity", "Hub", "Editor", latestVersion, "Editor", "Data", "Managed");
                else
                    managedPath = Path.Combine(progPath, "Unity", "Hub", "Editor", latestVersion, "Unity.app", "Contents", "Managed");
            }

            // Add assemblies to the type resolver so we can get the fully qualified names of types
            // The Unity docs only give us the short names
            TypeResolver.AddAssembly(typeof(IEnumerator).Assembly);
            TypeResolver.AddAssembly(Assembly.LoadFrom(Path.Combine(managedPath, @"UnityEngine.dll")));
            TypeResolver.AddAssembly(Assembly.LoadFrom(Path.Combine(managedPath, @"UnityEditor.dll")));
            Console.WriteLine();

            var unityApi = new UnityApi();
            if (apiXml.ExistsFile)
                unityApi = UnityApi.ImportFrom(apiXml);
            var parser = new ApiParser(unityApi);

            parser.Progress += (s, e) =>
            {
                var cursorTop = Console.CursorTop;
                Console.WriteLine("{0,5} / {1,5} ({2,3}%)", e.Current, e.Total, e.Percent);
                Console.SetCursorPosition(0, cursorTop);
            };

            foreach (var (name, version) in docVersions)
            {
                Console.WriteLine(name);
                parser.ParseFolder(name, version);

                AddUndocumentedApis(unityApi, version);
            }

            // These modify existing functions
            AddUndocumentedOptionalParameters(unityApi);
            AddUndocumentedCoroutines(unityApi);
            FixDataFromIncorrectDocs(unityApi);

            using (var writer = new XmlTextWriter(@"api.xml", Encoding.UTF8) {Formatting = Formatting.Indented})
            {
                parser.ExportTo(writer);
            }

            Console.WriteLine("Done. Elapsed time: {0}", stopwatch.Elapsed);

            // Console.WriteLine( "Press <Enter> key to continue..." );
            // Console.ReadLine();
        }

        private static FileSystemPath GetDocumentationRoot(string latestVersion)
        {
            var programFiles = FileSystemPath.Parse(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
            var docRoot = programFiles / "Unity" / "Hub" / "Editor" / latestVersion;
            var windowsRoot = docRoot / "Editor" / "Data";
            return windowsRoot.ExistsDirectory ? windowsRoot : docRoot;
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

        private static void FixDataFromIncorrectDocs(UnityApi unityApi)
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
                    var newParameter = new UnityApiParameter("sourcePath", ApiType.String, string.Empty);
                    function.UpdateParameter("arg1", newParameter);
                    newParameter = new UnityApiParameter("destinationPath", ApiType.String, string.Empty);
                    function.UpdateParameter("arg2", newParameter);
                }
            }

            type = unityApi.FindType("AssetPostprocessor");
            if (type != null)
            {
                // 2018.2 removes a UnityScript example which gave us the return type
                foreach (var function in type.FindEventFunctions("OnAssignMaterialModel"))
                    function.SetReturnType(new ApiType("UnityEngine.Material"));
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

                // From GitHub. Love the optimism in this one :)
                // https://github.com/Unity-Technologies/UnityCsReference/blob/96187e5fc1a23847206bf66b6f2d0e4a1ad43301/Editor/Mono/AssetPostprocessor.cs#L96
                var description =
                    "This is undocumented, and a 'safeguard' for when Visual Studio gets a new release that "
                    + "is incompatible with Unity, so that users can postprocess our csproj files to fix the issue (or "
                    + "just completely replace them). Hopefully we'll never need this.";
                eventFunction = new UnityApiEventFunction("OnGeneratedCSProjectFiles",
                    true, false, ApiType.Void, apiVersion, description, undocumented: true);
                type.MergeEventFunction(eventFunction, apiVersion);

                // Technically, return type is optional
                // https://github.com/Unity-Technologies/UnityCsReference/blob/96187e5fc1a23847206bf66b6f2d0e4a1ad43301/Editor/Mono/AssetPostprocessor.cs#L138
                description = "This callback is used by UnityVS to take over project generation from Unity";
                eventFunction = new UnityApiEventFunction("OnPreGeneratingCSProjectFiles",
                    true, false, ApiType.Bool, apiVersion, description, undocumented: true);
                type.MergeEventFunction(eventFunction, apiVersion);

                // These two were added in 2018.1, as verified on GitHub
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.4/Editor/Mono/AssetPostprocessor.cs
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2018.1/Editor/Mono/AssetPostprocessor.cs#L76
                if (apiVersion >= new Version(2018, 1))
                {
                    // Technically, return type is optional
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/96187e5fc1a23847206bf66b6f2d0e4a1ad43301/Editor/Mono/AssetPostprocessor.cs#L123
                    description = "This callback is used by C# code editors to modify the .csproj files.";
                    eventFunction = new UnityApiEventFunction("OnGeneratedCSProject",
                        true, false, ApiType.String, apiVersion, description, undocumented: true);
                    eventFunction.AddParameter("path", ApiType.String);
                    eventFunction.AddParameter("content", ApiType.String);
                    type.MergeEventFunction(eventFunction, apiVersion);

                    // Technically, return type is optional
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/96187e5fc1a23847206bf66b6f2d0e4a1ad43301/Editor/Mono/AssetPostprocessor.cs#L108
                    description = "This callback is used by C# code editors to modify the .sln file";
                    eventFunction = new UnityApiEventFunction("OnGeneratedSlnSolution",
                        true, false, ApiType.String, apiVersion, description, undocumented: true);
                    eventFunction.AddParameter("path", ApiType.String);
                    eventFunction.AddParameter("content", ApiType.String);
                    type.MergeEventFunction(eventFunction, apiVersion);
                }
            }

            // From AssetModificationProcessorInternal
            type = unityApi.FindType("AssetModificationProcessor");
            if (type != null)
            {
                var eventFunction = new UnityApiEventFunction("OnStatusUpdated", true,
                    false, ApiType.Void, apiVersion, undocumented: true);
                type.MergeEventFunction(eventFunction, apiVersion);
            }

            type = unityApi.FindType("MonoBehaviour");
            if (type != null)
            {
                const string description =
                    "This callback is called if an associated RectTransform has its dimensions changed.";
                var eventFunction = new UnityApiEventFunction("OnRectTransformDimensionsChange",
                    false, false, ApiType.Void, apiVersion, description, undocumented: true);
                type.MergeEventFunction(eventFunction, apiVersion);

                eventFunction = new UnityApiEventFunction("OnBeforeTransformParentChanged",
                    false, false, ApiType.Void, apiVersion, undocumented: true);
                type.MergeEventFunction(eventFunction, apiVersion);

                eventFunction = new UnityApiEventFunction("OnDidApplyAnimationProperties",
                    false, false, ApiType.Void, apiVersion, undocumented: true);
                type.MergeEventFunction(eventFunction, apiVersion);

                eventFunction = new UnityApiEventFunction("OnCanvasGroupChanged",
                    false, false, ApiType.Void, apiVersion, undocumented: true);
                type.MergeEventFunction(eventFunction, apiVersion);

                eventFunction = new UnityApiEventFunction("OnCanvasHierarchyChanged",
                    false, false, ApiType.Void, apiVersion, undocumented: true);
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
