using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using ApiParser.Resources;
using JetBrains.Util;

namespace ApiParser
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1 && args.Length != 2 && args.Length != 3)
            {
                Console.WriteLine("Usage: ApiParser.exe docsFolder");
                Console.WriteLine("       ApiParser.exe apiXmlPath version");
                Console.WriteLine("       ApiParser.exe docsFolder apiXmlPath version");
                Console.WriteLine();
                Console.WriteLine("ApiParser.exe docsFolder");
                Console.WriteLine("  Parse all documentation installed by Unity Hub, as well as everything in the docsFolder and create a new api.xml");
                Console.WriteLine();
                Console.WriteLine("  docsFolder - folder that contains multiple versions of Unity docs");
                Console.WriteLine("               Contents should be in the format Documentation-X.Y.ZfA/Documentation/CountryCode/ScriptReference");
                Console.WriteLine();
                Console.WriteLine("ApiParser.exe apiXmlPath version");
                Console.WriteLine("  Parse the installed documentation corresponding to version and merge into an existing api.xml file");
                Console.WriteLine();
                Console.WriteLine("  apiXmlPath - location of api.xml to read and merge into");
                Console.WriteLine("  version - version of Unity to read docs from. Must be installed in standard Unity Hub location");
                Console.WriteLine();
                Console.WriteLine("ApiParser.exe docsFolder apiXmlPath version");
                Console.WriteLine("  Parse the installed documentation corresponding to version and merge into an existing api.xml file");
                Console.WriteLine();
                Console.WriteLine("  docsFolder - folder that contains multiple versions of Unity docs");
                Console.WriteLine("               Contents should be in the format Documentation-X.Y.ZfA/Documentation/CountryCode/ScriptReference");
                Console.WriteLine("  apiXmlPath - location of api.xml to read and merge into");
                Console.WriteLine("  version - version of Unity to read docs from.");
                Console.WriteLine();
                Console.WriteLine("Note that the output file is written to the current directory");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var apiXml = FileSystemPath.Parse("api.xml");
            
            var docFolders = new List<FileSystemPath>();
            if (args.Length == 1)
            {
                Directory.SetCurrentDirectory(args[0]);
                docFolders.AddRange(FileSystemPath.Parse(args[0]).GetDirectoryEntries("*", PathSearchFlags.ExcludeFiles).Select(a=>a.GetAbsolutePath()));

                var unityPathInProgramFiles = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Unity", "Hub", "Editor");
                if (Directory.Exists(unityPathInProgramFiles))
                    docFolders.AddRange(Directory.EnumerateDirectories(unityPathInProgramFiles)
                        .Select(GetDocumentationRoot));
            }
            else if (args.Length == 2)
            {
                apiXml = FileSystemPath.ParseRelativelyTo(args[0], FileSystemPath.Parse(Directory.GetCurrentDirectory()));
                if (!apiXml.ExistsFile)
                    throw new InvalidOperationException("api.xml path does not exist");

                var requiredVersion = args[1];
                var docFolder = GetDocumentationRoot(requiredVersion);
                if (!docFolder.ExistsDirectory)
                    throw new InvalidOperationException($"Cannot find locally installed docs: {docFolder}");
                docFolders.Add(docFolder);
            }
            else
            {
                Directory.SetCurrentDirectory(args[0]);
                docFolders.AddRange(FileSystemPath.Parse(args[0]).GetDirectoryEntries("*", PathSearchFlags.ExcludeFiles).Select(a=>a.GetAbsolutePath()));
                
                apiXml = FileSystemPath.ParseRelativelyTo(args[1], FileSystemPath.Parse(Directory.GetCurrentDirectory()));
                if (!apiXml.ExistsFile)
                    throw new InvalidOperationException("api.xml path does not exist");

                var requiredVersion = args[2];
                // todo: search requiredVersion among GetDocumentationRoot(requiredVersion);
            }

            var docVersions = new List<(FileSystemPath, Version, RiderSupportedLanguages)>();
            foreach (var docFolder in docFolders)
            {
                var directoryName = docFolder.Name;
                var version = Regex.Match(directoryName, @"Documentation-(\d+.\d+)").Groups[1].Value;
                var langFolders = docFolder.Combine("Documentation").GetChildren();
                foreach (var folder in langFolders)
                {
                    docVersions.Add((folder.GetAbsolutePath(), Version.Parse(version), LocalizationUtil.TranslateCountryCodeIntoLanguageCode(folder.GetAbsolutePath().Name)));    
                }
            }
                
            docVersions = docVersions.OrderBy(v => v.Item2).ToList();
            
            var unityApi = new UnityApi();
            if (apiXml.ExistsFile)
                unityApi = UnityApi.ImportFrom(apiXml);
            var typeResolver = new TypeResolver();
            var parser = new ApiParser(unityApi, typeResolver);

            foreach (var (name, version, langCode) in docVersions)
            {
                Console.WriteLine($"{name} ({version}) {langCode}");
                parser.ParseFolder(name.FullPath, version, langCode);

                AddUndocumentedApis(unityApi, version);
            }

            // These modify existing functions
            AddUndocumentedOptionalParameters(unityApi);
            AddUndocumentedCoroutines(unityApi);
            FixDataFromIncorrectDocs(unityApi, typeResolver);

            using (var writer = new XmlTextWriter(apiXml.FullPath, Encoding.UTF8) {Formatting = Formatting.Indented})
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

        private static void FixDataFromIncorrectDocs(UnityApi unityApi, TypeResolver typeResolver)
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
                    var newParameter = new UnityApiParameter("assetPath", ApiType.String);
                    function.UpdateParameterIfExists("arg1", newParameter);
                    newParameter = new UnityApiParameter("message", ApiType.StringByRef);
                    function.UpdateParameterIfExists("arg2", newParameter);
                }

                foreach (var function in type.FindEventFunctions("OnWillDeleteAsset"))
                {
                    function.SetIsStatic();
                    function.SetReturnType(typeResolver.CreateApiType("UnityEditor.AssetDeleteResult"));
                    var newParameter = new UnityApiParameter("assetPath", ApiType.String);
                    function.UpdateParameterIfExists("arg1", newParameter);
                    newParameter = new UnityApiParameter("options", new ApiType("UnityEditor.RemoveAssetOptions"));
                    function.UpdateParameterIfExists("arg2", newParameter);
                }

                foreach (var function in type.FindEventFunctions("OnWillMoveAsset"))
                {
                    function.SetIsStatic();
                    function.SetReturnType(typeResolver.CreateApiType("UnityEditor.AssetMoveResult"));
                    var newParameter = new UnityApiParameter("sourcePath", ApiType.String);
                    function.UpdateParameterIfExists("arg1", newParameter);
                    newParameter = new UnityApiParameter("destinationPath", ApiType.String);
                    function.UpdateParameterIfExists("arg2", newParameter);
                }
            }

            type = unityApi.FindType("AssetPostprocessor");
            if (type != null)
            {
                // 2018.2 removes a UnityScript example which gave us the return type
                foreach (var function in type.FindEventFunctions("OnAssignMaterialModel"))
                    function.SetReturnType(typeResolver.CreateApiType("UnityEngine.Material"));
            }
        }

        // Note that if we add new undocumented APIs, this won't set the correct min/max version range, and will only
        // apply the given api versions. That gives us two options:
        // 1) Recreate the api.xml file from scratch by parsing the documentation of every Unity version since 5.0
        // 2) Cheat a little. When incrementally updating an existing api.xml for a single version and also adding new
        //    undocumented APIs, add extra calls to AddUndocumentedApis with the min/max version for those new APIs.
        //    Don't check these extra calls in!
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
                var description = Strings.AssetPostprocessor_OnGeneratedCSProjectFiles_Description;
                eventFunction = new UnityApiEventFunction("OnGeneratedCSProjectFiles",
                    true, false, ApiType.Void, apiVersion, undocumented: true);
                eventFunction.AddDescription(description, RiderSupportedLanguages.iv);
                type.MergeEventFunction(eventFunction, apiVersion);

                // Technically, return type is optional
                // https://github.com/Unity-Technologies/UnityCsReference/blob/96187e5fc1a23847206bf66b6f2d0e4a1ad43301/Editor/Mono/AssetPostprocessor.cs#L138
                description = Strings.AssetPostprocessor_OnPreGeneratingCSProjectFiles_Description;
                eventFunction = new UnityApiEventFunction("OnPreGeneratingCSProjectFiles",
                    true, false, ApiType.Bool, apiVersion, undocumented: true);
                eventFunction.AddDescription(description, RiderSupportedLanguages.iv);
                type.MergeEventFunction(eventFunction, apiVersion);

                // These two were added in 2018.1, as verified on GitHub
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.4/Editor/Mono/AssetPostprocessor.cs
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2018.1/Editor/Mono/AssetPostprocessor.cs#L76
                if (apiVersion >= new Version(2018, 1))
                {
                    // Technically, return type is optional
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/96187e5fc1a23847206bf66b6f2d0e4a1ad43301/Editor/Mono/AssetPostprocessor.cs#L123
                    description = Strings.AssetPostprocessor_OnGeneratedCSProject_Description;
                    eventFunction = new UnityApiEventFunction("OnGeneratedCSProject",
                        true, false, ApiType.String, apiVersion, undocumented: true);
                    eventFunction.AddParameter("path", ApiType.String);
                    eventFunction.AddParameter("content", ApiType.String);
                    eventFunction.AddDescription(description, RiderSupportedLanguages.iv);
                    type.MergeEventFunction(eventFunction, apiVersion);

                    // Technically, return type is optional
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/96187e5fc1a23847206bf66b6f2d0e4a1ad43301/Editor/Mono/AssetPostprocessor.cs#L108
                    description = Strings.AssetPostprocessor_OnGeneratedSlnSolution_Description;
                    eventFunction = new UnityApiEventFunction("OnGeneratedSlnSolution",
                        true, false, ApiType.String, apiVersion, undocumented: true);
                    eventFunction.AddParameter("path", ApiType.String);
                    eventFunction.AddParameter("content", ApiType.String);
                    eventFunction.AddDescription(description, RiderSupportedLanguages.iv);
                    type.MergeEventFunction(eventFunction, apiVersion);
                }

                // OnPostprocessAllAssets got a new parameter in 2021.2. Only the new parameter has an official doc page
                // but that page says:
                // Note: A version of this callback without the didDomainReload parameter is also available
                // (OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths))
                if (apiVersion >= new Version(2021, 2))
                {
                    // This function will match the pre 2021.2 functions and extend it's max applicable version
                    description = Strings.AssetPostprocessor_OnPostprocessAllAssets_Description;
                    var functions = type.FindEventFunctions("OnPostprocessAllAssets");
                    eventFunction = new UnityApiEventFunction("OnPostprocessAllAssets",
                        true, false, ApiType.Void, apiVersion,
                        "ScriptReference/AssetPostprocessor.OnPostprocessAllAssets.html");
                    eventFunction.AddDescription(description, RiderSupportedLanguages.iv);
                    eventFunction.AddParameter("importedAssets", ApiType.StringArray);
                    eventFunction.AddParameter("deletedAssets", ApiType.StringArray);
                    eventFunction.AddParameter("movedAssets", ApiType.StringArray);
                    eventFunction.AddParameter("movedFromAssetPaths", ApiType.StringArray);
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
                var description = Strings.MonoBehaviour_OnRectTransformDimensionsChange_Description;
                var eventFunction = new UnityApiEventFunction("OnRectTransformDimensionsChange",
                    false, false, ApiType.Void, apiVersion, undocumented: true);
                eventFunction.AddDescription(description, RiderSupportedLanguages.iv);
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
            if (type != null && apiVersion < new Version(2020, 1))
            {
                // Documented in 2020.1
                var eventFunction = new UnityApiEventFunction("OnValidate", false, false, ApiType.Void, apiVersion, undocumented: true);
                eventFunction.AddDescription(Strings.ScriptableObject_OnValidate_Description, RiderSupportedLanguages.iv);
                type.MergeEventFunction(eventFunction, apiVersion);

                // Documented in 2020.1
                eventFunction = new UnityApiEventFunction("Reset", false, false, ApiType.Void, apiVersion, undocumented: true);
                eventFunction.AddDescription("Reset to default values.", RiderSupportedLanguages.iv);
                type.MergeEventFunction(eventFunction, apiVersion);
            }

            // TODO: Check if these event functions are available in 5.0 - 5.6
            type = unityApi.FindType("Editor");
            if (type != null && apiVersion >= new Version(2017, 1))
            {
                // Editor.OnPreSceneGUI has been around since at least 2017.1. Still undocumented as of 2020.2
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/SceneView/SceneView.cs#L2436
                var eventFunction = new UnityApiEventFunction("OnPreSceneGUI", false, false, ApiType.Void, apiVersion,
                    undocumented: true);
                eventFunction.AddDescription(Strings.Editor_OnPreSceneGUI_Description, RiderSupportedLanguages.iv);
                type.MergeEventFunction(eventFunction, apiVersion);

                // Editor.OnSceneDrag has been around since at least 2017.1. Still undocumented as of 2020.2
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/GUI/EditorCache.cs#L63
                eventFunction = new UnityApiEventFunction("OnSceneDrag", false, false, ApiType.Void, apiVersion,
                    undocumented: true);
                eventFunction.AddDescription(Strings.Editor_OnSceneDrag_Description, RiderSupportedLanguages.iv);
                eventFunction.AddParameter("sceneView", new ApiType("UnityEditor.SceneView"), new KeyValuePair<RiderSupportedLanguages, string>(RiderSupportedLanguages.iv, Strings.Editor_OnSceneDrag_sceneView_Description));
                eventFunction.AddParameter("index", ApiType.Int, new KeyValuePair<RiderSupportedLanguages, string>(RiderSupportedLanguages.iv, Strings.Editor_OnSceneDrag_index_Description));
                type.MergeEventFunction(eventFunction, apiVersion);

                if (apiVersion < new Version(2020, 2))
                {
                    // Editor.HasFrameBounds has been around since at least 2017.1. First documented in 2020.2
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/SceneView/SceneView.cs#L2296
                    // https://docs.unity3d.com/2020.2/Documentation/ScriptReference/Editor.HasFrameBounds.html
                    eventFunction = new UnityApiEventFunction("HasFrameBounds", false, false, ApiType.Bool,
                        apiVersion,
                        undocumented: true);
                    eventFunction.AddDescription(Strings.Editor_HasFrameBounds_Description, RiderSupportedLanguages.iv);
                    type.MergeEventFunction(eventFunction, apiVersion);

                    // Editor.OnGetFrameBounds has been around since at least 2017.1. First documented in 2020.2
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/SceneView/SceneView.cs#L2303
                    // https://docs.unity3d.com/2020.2/Documentation/ScriptReference/Editor.OnGetFrameBounds.html
                    eventFunction = new UnityApiEventFunction("OnGetFrameBounds", false, false,
                        new ApiType("UnityEngine.Bounds"), apiVersion,
                        undocumented: true);
                    eventFunction.AddDescription(Strings.Editor_OnGetFrameBounds_Description, RiderSupportedLanguages.iv);
                    type.MergeEventFunction(eventFunction, apiVersion);
                }
            }

            // TODO: Check if these event functions are available in 5.0 - 5.6
            type = unityApi.FindType("EditorWindow");
            if (type != null && apiVersion >= new Version(2017, 1))
            {
                // EditorWindow.ModifierKeysChanged has been around since at least 2017.1. Still undocumented as of 2020.2
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/HostView.cs#L290
                // http://www.improck.com/2014/11/editorwindow-modifier-keys/
                var eventFunction = new UnityApiEventFunction("ModifierKeysChanged", false, false, ApiType.Void, apiVersion,
                    undocumented: true);
                eventFunction.AddDescription(Strings.EditorWindow_ModifierKeysChanged_Description, RiderSupportedLanguages.iv);
                type.MergeEventFunction(eventFunction, apiVersion);

                // EditorWindow.ShowButton has been around since at least 2017.1. Still undocumented as of 2020.2
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/HostView.cs#L356
                // http://www.improck.com/2014/11/editorwindow-inspector-lock-icon/
                eventFunction = new UnityApiEventFunction("ShowButton", false, false, ApiType.Void, apiVersion,
                    undocumented: true);
                eventFunction.AddDescription(Strings.EditorWindow_ShowButton_Description, RiderSupportedLanguages.iv);
                eventFunction.AddParameter("rect", new ApiType("UnityEngine.Rect"), new KeyValuePair<RiderSupportedLanguages, string>(RiderSupportedLanguages.iv, "Position to draw the button"));
                type.MergeEventFunction(eventFunction, apiVersion);

                // EditorWindow.OnBecameVisible has been around since at least 2017.1. Still undocumented as of 2020.2
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/HostView.cs#L302
                eventFunction = new UnityApiEventFunction("OnBecameVisible", false, false, ApiType.Void, apiVersion,
                    undocumented: true);
                eventFunction.AddDescription(Strings.EditorWindow_OnBecameVisible_Description, RiderSupportedLanguages.iv);
                type.MergeEventFunction(eventFunction, apiVersion);

                // EditorWindow.OnBecameInvisible has been around since at least 2017.1. Still undocumented as of 2020.2
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/HostView.cs#L337
                eventFunction = new UnityApiEventFunction("OnBecameInvisible", false, false, ApiType.Void, apiVersion,
                    undocumented: true);
                eventFunction.AddDescription(Strings.EditorWindow_OnBecameInvisible_Description, RiderSupportedLanguages.iv);
                type.MergeEventFunction(eventFunction, apiVersion);

                // EditorWindow.OnDidOpenScene has been around since at least 2017.1. Still undocumented as of 2020.2
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/HostView.cs#L163
                eventFunction = new UnityApiEventFunction("OnDidOpenScene", false, false, ApiType.Void, apiVersion,
                    undocumented: true);
                eventFunction.AddDescription(Strings.EditorWindow_OnDidOpenScene_Description, RiderSupportedLanguages.iv);
                type.MergeEventFunction(eventFunction, apiVersion);

                if (apiVersion >= new Version(2019, 1))
                {
                    // EditorWindow.OnAddedAsTab was introduced in 2019.1. Still undocumented as of 2020.2
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/2019.1/Editor/Mono/GUI/DockArea.cs#L188
                    eventFunction = new UnityApiEventFunction("OnAddedAsTab", false, false, ApiType.Void, apiVersion,
                        undocumented: true);
                    eventFunction.AddDescription(Strings.EditorWindow_OnAddedAsTab_Description, RiderSupportedLanguages.iv);
                    type.MergeEventFunction(eventFunction, apiVersion);

                    // EditorWindow.OnBeforeRemovedAsTab was introduced in 2019.1
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/2019.1/Editor/Mono/GUI/DockArea.cs#L195
                    eventFunction = new UnityApiEventFunction("OnBeforeRemovedAsTab", false, false, ApiType.Void, apiVersion,
                        undocumented: true);
                    eventFunction.AddDescription(Strings.EditorWindow_OnBeforeRemovedAsTab_Description, RiderSupportedLanguages.iv);
                    type.MergeEventFunction(eventFunction, apiVersion);
                }

                if (apiVersion >= new Version(2019, 3))
                {
                    // EditorWindow.OnTabDetached was introduced in 2019.3
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/2019.3/Editor/Mono/GUI/DockArea.cs#L940
                    eventFunction = new UnityApiEventFunction("OnTabDetached", false, false, ApiType.Void, apiVersion,
                        undocumented: true);
                    eventFunction.AddDescription(Strings.EditorWindow_OnTabDetached_Description, RiderSupportedLanguages.iv);
                    type.MergeEventFunction(eventFunction, apiVersion);
                }

                if (apiVersion >= new Version(2020, 1))
                {
                    // EditorWindow.OnMainWindowMove was introduced in 2020.1
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/2020.1/Editor/Mono/HostView.cs#L343
                    // See comment here
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/2020.1/Editor/Mono/ExternalPlayModeView/ExternalPlayModeView.cs#L112
                    eventFunction = new UnityApiEventFunction("OnMainWindowMove", false, false, ApiType.Void, apiVersion,
                        undocumented: true);
                    eventFunction.AddDescription(Strings.EditorWindow_OnMainWindowMove_Description, RiderSupportedLanguages.iv);
                    type.MergeEventFunction(eventFunction, apiVersion);
                }
            }
        }
    }
}
