using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using JetBrains.Util;

namespace ApiParser
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1 && args.Length != 2)
            {
                Console.WriteLine("Usage: ApiParser.exe docsFolder");
                Console.WriteLine("       ApiParser.exe apiXmlPath version");
                Console.WriteLine();
                Console.WriteLine("ApiParser.exe docsFolder");
                Console.WriteLine("  Parse all documentation installed by Unity Hub, as well as everything in the docsFolder and create a new api.xml");
                Console.WriteLine();
                Console.WriteLine("  docsFolder - folder that contains multiple versions of Unity docs");
                Console.WriteLine("               Contents should be in the format Documentation-X.Y.ZfA/Documentation/en/ScriptReference");
                Console.WriteLine();
                Console.WriteLine("ApiParser.exe apiXmlPath version");
                Console.WriteLine("  Parse the installed documentation corresponding to version and merge into an existing api.xml file");
                Console.WriteLine();
                Console.WriteLine("  apiXmlPath - location of api.xml to read and merge into");
                Console.WriteLine("  version - version of Unity to read docs from. Must be installed in standard Unity Hub location");
                Console.WriteLine();
                Console.WriteLine("Note that the output file is written to the current directory");
                return;
            }

            var stopwatch = Stopwatch.StartNew();
            var apiXml = FileSystemPath.Parse("api.xml");

            var docVersions = new List<(string, Version)>();
            if (args.Length == 1)
            {
                Directory.SetCurrentDirectory(args[0]);
                foreach (var directory in Directory.EnumerateDirectories(Directory.GetCurrentDirectory()))
                {
                    var docFolder = Path.Combine(Directory.GetCurrentDirectory(), directory);
                    var version = Regex.Match(directory, @"Documentation-(\d+.\d+)").Groups[1].Value;
                    docVersions.Add((docFolder, Version.Parse(version)));
                }

                foreach (var directory in Directory.EnumerateDirectories(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Unity", "Hub", "Editor")))
                {
                    var docFolder = GetDocumentationRoot(directory).FullPath;
                    var version = Regex.Match(directory, @"(\d+.\d+)").Groups[1].Value;
                    docVersions.Add((docFolder, Version.Parse(version)));
                }

                docVersions = docVersions.OrderBy(v => v.Item2).ToList();
            }
            else
            {
                apiXml = FileSystemPath.ParseRelativelyTo(args[0], FileSystemPath.Parse(Directory.GetCurrentDirectory()));
                if (!apiXml.ExistsFile)
                    throw new InvalidOperationException("api.xml path does not exist");

                var requiredVersion = args[1];
                var docRoot = GetDocumentationRoot(requiredVersion);
                if (!docRoot.ExistsDirectory)
                    throw new InvalidOperationException($"Cannot find locally installed docs: {docRoot}");
                var parseableVersion = Regex.Match(requiredVersion, @"^(\d+\.\d+)").Groups[1].Value;
                docVersions.Add((docRoot.FullPath, Version.Parse(parseableVersion)));
            }

            var unityApi = new UnityApi();
            if (apiXml.ExistsFile)
                unityApi = UnityApi.ImportFrom(apiXml);
            var typeResolver = new TypeResolver();
            var parser = new ApiParser(unityApi, typeResolver);

            foreach (var (name, version) in docVersions)
            {
                Console.WriteLine($"{name} ({version})");
                parser.ParseFolder(name, version);

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
                    var newParameter = new UnityApiParameter("assetPath", ApiType.String, string.Empty);
                    function.UpdateParameterIfExists("arg1", newParameter);
                    newParameter = new UnityApiParameter("message", ApiType.StringByRef, string.Empty);
                    function.UpdateParameterIfExists("arg2", newParameter);
                }

                foreach (var function in type.FindEventFunctions("OnWillDeleteAsset"))
                {
                    function.SetIsStatic();
                    function.SetReturnType(typeResolver.CreateApiType("UnityEditor.AssetDeleteResult"));
                    var newParameter = new UnityApiParameter("assetPath", ApiType.String, string.Empty);
                    function.UpdateParameterIfExists("arg1", newParameter);
                    newParameter = new UnityApiParameter("options", new ApiType("UnityEditor.RemoveAssetOptions"), string.Empty);
                    function.UpdateParameterIfExists("arg2", newParameter);
                }

                foreach (var function in type.FindEventFunctions("OnWillMoveAsset"))
                {
                    function.SetIsStatic();
                    function.SetReturnType(typeResolver.CreateApiType("UnityEditor.AssetMoveResult"));
                    var newParameter = new UnityApiParameter("sourcePath", ApiType.String, string.Empty);
                    function.UpdateParameterIfExists("arg1", newParameter);
                    newParameter = new UnityApiParameter("destinationPath", ApiType.String, string.Empty);
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

                // OnPostprocessAllAssets got a new parameter in 2021.2. Only the new parameter has an official doc page
                // but that page says:
                // Note: A version of this callback without the didDomainReload parameter is also available
                // (OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths))
                if (apiVersion >= new Version(2021, 2))
                {
                    // This function will match the pre 2021.2 functions and extend it's max applicable version
                    description =
                        "This is called after importing of any number of assets is complete (when the Assets progress bar has reached the end).";
                    var functions = type.FindEventFunctions("OnPostprocessAllAssets");
                    eventFunction = new UnityApiEventFunction("OnPostprocessAllAssets",
                        true, false, ApiType.Void, apiVersion, description,
                        "Documentation/en/ScriptReference/AssetPostprocessor.OnPostprocessAllAssets.html");
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
            if (type != null && apiVersion < new Version(2020, 1))
            {
                // Documented in 2020.1
                var eventFunction = new UnityApiEventFunction("OnValidate", false, false, ApiType.Void, apiVersion,
                    description:
                    "This function is called when the script is loaded or a value is changed in the inspector (Called in the editor only).",
                    undocumented: true);
                type.MergeEventFunction(eventFunction, apiVersion);

                // Documented in 2020.1
                eventFunction = new UnityApiEventFunction("Reset", false, false, ApiType.Void, apiVersion,
                    description: "Reset to default values.", undocumented: true);
                type.MergeEventFunction(eventFunction, apiVersion);
            }

            // TODO: Check if these event functions are available in 5.0 - 5.6
            type = unityApi.FindType("Editor");
            if (type != null && apiVersion >= new Version(2017, 1))
            {
                // Editor.OnPreSceneGUI has been around since at least 2017.1. Still undocumented as of 2020.2
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/SceneView/SceneView.cs#L2436
                var eventFunction = new UnityApiEventFunction("OnPreSceneGUI", false, false, ApiType.Void, apiVersion,
                    description: "Called before the Scene view is drawn.",
                    undocumented: true);
                type.MergeEventFunction(eventFunction, apiVersion);

                // Editor.OnSceneDrag has been around since at least 2017.1. Still undocumented as of 2020.2
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/GUI/EditorCache.cs#L63
                eventFunction = new UnityApiEventFunction("OnSceneDrag", false, false, ApiType.Void, apiVersion,
                    description: "Called for each object dragged onto the scene view",
                    undocumented: true);
                eventFunction.AddParameter("sceneView", new ApiType("UnityEditor.SceneView"), "The current scene view");
                eventFunction.AddParameter("index", ApiType.Int, "The index into the DragAndDrop.objectReferences array");
                type.MergeEventFunction(eventFunction, apiVersion);

                if (apiVersion < new Version(2020, 2))
                {
                    // Editor.HasFrameBounds has been around since at least 2017.1. First documented in 2020.2
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/SceneView/SceneView.cs#L2296
                    // https://docs.unity3d.com/2020.2/Documentation/ScriptReference/Editor.HasFrameBounds.html
                    eventFunction = new UnityApiEventFunction("HasFrameBounds", false, false, ApiType.Bool,
                        apiVersion,
                        description: "Validates whether custom bounds can be calculated for this editor.",
                        undocumented: true);
                    type.MergeEventFunction(eventFunction, apiVersion);

                    // Editor.OnGetFrameBounds has been around since at least 2017.1. First documented in 2020.2
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/SceneView/SceneView.cs#L2303
                    // https://docs.unity3d.com/2020.2/Documentation/ScriptReference/Editor.OnGetFrameBounds.html
                    eventFunction = new UnityApiEventFunction("OnGetFrameBounds", false, false,
                        new ApiType("UnityEngine.Bounds"), apiVersion,
                        description: "Gets custom bounds for the target of this editor.",
                        undocumented: true);
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
                    description: "Called when the modifier keys are changed. Automatically registers and de-registers the EditorApplication.modifierKeysChanged event",
                    undocumented: true);
                type.MergeEventFunction(eventFunction, apiVersion);

                // EditorWindow.ShowButton has been around since at least 2017.1. Still undocumented as of 2020.2
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/HostView.cs#L356
                // http://www.improck.com/2014/11/editorwindow-inspector-lock-icon/
                eventFunction = new UnityApiEventFunction("ShowButton", false, false, ApiType.Void, apiVersion,
                    description: "Allow Editor panes to show a small button next to the generic menu (e.g. inspector lock icon)",
                    undocumented: true);
                eventFunction.AddParameter("rect", new ApiType("UnityEngine.Rect"), "Position to draw the button");
                type.MergeEventFunction(eventFunction, apiVersion);

                // EditorWindow.OnBecameVisible has been around since at least 2017.1. Still undocumented as of 2020.2
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/HostView.cs#L302
                eventFunction = new UnityApiEventFunction("OnBecameVisible", false, false, ApiType.Void, apiVersion,
                    description: "Called when an editor window has been opened",
                    undocumented: true);
                type.MergeEventFunction(eventFunction, apiVersion);

                // EditorWindow.OnBecameInvisible has been around since at least 2017.1. Still undocumented as of 2020.2
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/HostView.cs#L337
                eventFunction = new UnityApiEventFunction("OnBecameInvisible", false, false, ApiType.Void, apiVersion,
                    description: "Called when an editor window has been closed",
                    undocumented: true);
                type.MergeEventFunction(eventFunction, apiVersion);

                // EditorWindow.OnDidOpenScene has been around since at least 2017.1. Still undocumented as of 2020.2
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/HostView.cs#L163
                eventFunction = new UnityApiEventFunction("OnDidOpenScene", false, false, ApiType.Void, apiVersion,
                    description: "Called when a scene has been opened",
                    undocumented: true);
                type.MergeEventFunction(eventFunction, apiVersion);

                if (apiVersion >= new Version(2019, 1))
                {
                    // EditorWindow.OnAddedAsTab was introduced in 2019.1. Still undocumented as of 2020.2
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/2019.1/Editor/Mono/GUI/DockArea.cs#L188
                    eventFunction = new UnityApiEventFunction("OnAddedAsTab", false, false, ApiType.Void, apiVersion,
                        description: "Called when the editor window is added as a tab",
                        undocumented: true);
                    type.MergeEventFunction(eventFunction, apiVersion);

                    // EditorWindow.OnBeforeRemovedAsTab was introduced in 2019.1
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/2019.1/Editor/Mono/GUI/DockArea.cs#L195
                    eventFunction = new UnityApiEventFunction("OnBeforeRemovedAsTab", false, false, ApiType.Void, apiVersion,
                        description: "Called before an editor window is removed as a tab",
                        undocumented: true);
                }

                if (apiVersion >= new Version(2019, 3))
                {
                    // EditorWindow.OnTabDetached was introduced in 2019.3
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/2019.3/Editor/Mono/GUI/DockArea.cs#L940
                    eventFunction = new UnityApiEventFunction("OnTabDetached", false, false, ApiType.Void, apiVersion,
                        description: "Called during drag and drop, when an editor window tab is detached",
                        undocumented: true);
                    type.MergeEventFunction(eventFunction, apiVersion);
                }

                if (apiVersion >= new Version(2020, 1))
                {
                    // EditorWindow.OnMainWindowMove was introduced in 2020.1
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/2020.1/Editor/Mono/HostView.cs#L343
                    // See comment here
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/2020.1/Editor/Mono/ExternalPlayModeView/ExternalPlayModeView.cs#L112
                    eventFunction = new UnityApiEventFunction("OnMainWindowMove", false, false, ApiType.Void, apiVersion,
                        description: "Called when the main window is moved",
                        undocumented: true);
                    type.MergeEventFunction(eventFunction, apiVersion);
                }
            }
        }
    }
}
