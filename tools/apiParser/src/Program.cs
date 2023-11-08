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
// ReSharper disable LocalizableElement

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
                
                eventFunction = new UnityApiEventFunction("OnGeneratedCSProjectFiles",
                    true, false, ApiType.Void, apiVersion, undocumented: true);
                eventFunction.AddDescription(Strings.AssetPostprocessor_OnGeneratedCSProjectFiles_Description, RiderSupportedLanguages.iv);
                eventFunction.AddDescription("これはドキュメント化されておらず、Visual Studio が Unity と互換性のない新しいリリースになったときの 'セーフガード' であるため、ユーザーは csproj ファイルを後処理して問題を解決できます (または単に完全に置換できます)。うまくいけば、これは不要になります。", RiderSupportedLanguages.ja);
                eventFunction.AddDescription("이는 문서화되지 않았으며 Visual Studio의 새 릴리스가 Unity와 호환되지 않는 경우를 위한 '보호 장치'로서, 이를 통해 사용자는 csproj 파일을 후처리하여 문제를 해결할 수 있습니다(또는 단순히 전면 교체). 이것이 필요한 상황이 오지 않기를 바랍니다.", RiderSupportedLanguages.ko);
                eventFunction.AddDescription("此项目未记录，并且是 Visual Studio 获得与 Unity 不兼容的新版本时的“保障”，这样用户便可对我们的 csproj 文件进行后处理来修正问题(或完全替换它们)。希望我们永远不需要这个。", RiderSupportedLanguages.zh);
                type.MergeEventFunction(eventFunction, apiVersion);

                // Technically, return type is optional
                // https://github.com/Unity-Technologies/UnityCsReference/blob/96187e5fc1a23847206bf66b6f2d0e4a1ad43301/Editor/Mono/AssetPostprocessor.cs#L138
                eventFunction = new UnityApiEventFunction("OnPreGeneratingCSProjectFiles",
                    true, false, ApiType.Bool, apiVersion, undocumented: true);
                eventFunction.AddDescription( Strings.AssetPostprocessor_OnPreGeneratingCSProjectFiles_Description, RiderSupportedLanguages.iv);
                eventFunction.AddDescription("このコールバックは UnityVS が Unity からプロジェクトの生成を引き継ぐために使用されます", RiderSupportedLanguages.ja);
                eventFunction.AddDescription("이 콜백은 프로젝트 생성을 Unity에서 인계받기 위해 UnityVS에 의해 사용됩니다", RiderSupportedLanguages.ko);
                eventFunction.AddDescription("UnityVS 使用此回调从 Unity 接管项目生成", RiderSupportedLanguages.zh);
                type.MergeEventFunction(eventFunction, apiVersion);

                // These two were added in 2018.1, as verified on GitHub
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.4/Editor/Mono/AssetPostprocessor.cs
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2018.1/Editor/Mono/AssetPostprocessor.cs#L76
                if (apiVersion >= new Version(2018, 1))
                {
                    // Technically, return type is optional
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/96187e5fc1a23847206bf66b6f2d0e4a1ad43301/Editor/Mono/AssetPostprocessor.cs#L123
                    eventFunction = new UnityApiEventFunction("OnGeneratedCSProject",
                        true, false, ApiType.String, apiVersion, undocumented: true);
                    eventFunction.AddParameter("path", ApiType.String);
                    eventFunction.AddParameter("content", ApiType.String);
                    eventFunction.AddDescription(Strings.AssetPostprocessor_OnGeneratedCSProject_Description, RiderSupportedLanguages.iv);
                    eventFunction.AddDescription("このコールバックは C# コードエディターが .csproj ファイルを変更するために使用されます。", RiderSupportedLanguages.ja);
                    eventFunction.AddDescription("이 콜백은 .csproj 파일을 수정하기 위해 C# 코드 에디터에 의해 사용됩니다", RiderSupportedLanguages.ko);
                    eventFunction.AddDescription("C# 代码编辑器使用此回调来修改 .csproj 文件。", RiderSupportedLanguages.zh);
                    type.MergeEventFunction(eventFunction, apiVersion);

                    // Technically, return type is optional
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/96187e5fc1a23847206bf66b6f2d0e4a1ad43301/Editor/Mono/AssetPostprocessor.cs#L108
                    eventFunction = new UnityApiEventFunction("OnGeneratedSlnSolution",
                        true, false, ApiType.String, apiVersion, undocumented: true);
                    eventFunction.AddParameter("path", ApiType.String);
                    eventFunction.AddParameter("content", ApiType.String);
                    eventFunction.AddDescription(Strings.AssetPostprocessor_OnGeneratedSlnSolution_Description, RiderSupportedLanguages.iv);
                    eventFunction.AddDescription("このコールバックは C# コードエディターが .sln ファイルを変更するために使用されます。", RiderSupportedLanguages.ja);
                    eventFunction.AddDescription("이 콜백은 .sln 파일을 수정하기 위해 C# 코드 에디터에 의해 사용됩니다", RiderSupportedLanguages.ko);
                    eventFunction.AddDescription("C# 代码编辑器使用此回调来修改 .sln 文件。", RiderSupportedLanguages.zh);
                    type.MergeEventFunction(eventFunction, apiVersion);
                }

                // OnPostprocessAllAssets got a new parameter in 2021.2. Only the new parameter has an official doc page
                // but that page says:
                // Note: A version of this callback without the didDomainReload parameter is also available
                // (OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths))
                if (apiVersion >= new Version(2021, 2))
                {
                    eventFunction = new UnityApiEventFunction("OnPostprocessAllAssets",
                        true, false, ApiType.Void, apiVersion,
                        "ScriptReference/AssetPostprocessor.OnPostprocessAllAssets.html");
                    eventFunction.AddDescription( Strings.AssetPostprocessor_OnPostprocessAllAssets_Description, RiderSupportedLanguages.iv);
                    eventFunction.AddDescription("これは任意の数のアセットがインポートされた後 (アセット進捗バーが終端に達したとき) に呼び出されます。", RiderSupportedLanguages.ja);
                    eventFunction.AddDescription("이는 여러 애셋 가져오기가 완료된 후(애셋 진행률 표시줄이 끝에 도달할 때) 호출됩니다.", RiderSupportedLanguages.ko);
                    eventFunction.AddDescription("在任意数量的资源导入完成后调用(当资源进度条到达末尾时)。", RiderSupportedLanguages.zh);
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
                var eventFunction = new UnityApiEventFunction("OnRectTransformDimensionsChange",
                    false, false, ApiType.Void, apiVersion, undocumented: true);
                eventFunction.AddDescription(Strings.MonoBehaviour_OnRectTransformDimensionsChange_Description, RiderSupportedLanguages.iv);
                eventFunction.AddDescription("このコールバックは関連付けられた RectTransform の寸法が変更された場合に呼び出されます。", RiderSupportedLanguages.ja);
                eventFunction.AddDescription("이 콜백은 연결된 RectTransform의 크기가 변경된 경우 호출됩니다.", RiderSupportedLanguages.ko);
                eventFunction.AddDescription("如果关联的 RectTransform 的维度发生更改，则会调用此回调。", RiderSupportedLanguages.zh);
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
                eventFunction.AddDescription("この関数はスクリプトが読み込まれたか、インスペクターで値が変更された場合に呼び出されます (エディターのみで呼び出されます)。", RiderSupportedLanguages.ja);
                eventFunction.AddDescription("이 함수는 스크립트가 로드되거나 검사 도구에서 값이 변경된 경우 호출됩니다(에디터에서만 호출됨).", RiderSupportedLanguages.ko);
                eventFunction.AddDescription("当加载脚本或在检查器中更改值时调用(仅在编辑器中调用)。", RiderSupportedLanguages.zh);
                type.MergeEventFunction(eventFunction, apiVersion);

                // Documented in 2020.1
                eventFunction = new UnityApiEventFunction("Reset", false, false, ApiType.Void, apiVersion, undocumented: true);
                eventFunction.AddDescription(Strings.ScriptableObject_Reset_Description, RiderSupportedLanguages.iv);
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
                eventFunction.AddDescription("シーンビューが描画される前に呼び出されます。", RiderSupportedLanguages.ja);
                eventFunction.AddDescription("씬 뷰가 그려지기 전에 호출됩니다.", RiderSupportedLanguages.ko);
                eventFunction.AddDescription("在绘制场景视图之前调用。", RiderSupportedLanguages.zh);
                type.MergeEventFunction(eventFunction, apiVersion);

                // Editor.OnSceneDrag has been around since at least 2017.1. Still undocumented as of 2020.2
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/GUI/EditorCache.cs#L63
                eventFunction = new UnityApiEventFunction("OnSceneDrag", false, false, ApiType.Void, apiVersion,
                    undocumented: true);
                eventFunction.AddDescription(Strings.Editor_OnSceneDrag_Description, RiderSupportedLanguages.iv);
                eventFunction.AddDescription("シーンビューにドラッグされる各オブジェクトに対して呼び出されます", RiderSupportedLanguages.ja);
                eventFunction.AddDescription("씬 뷰로 드래그된 각 객체에 대해 호출됩니다", RiderSupportedLanguages.ko);
                eventFunction.AddDescription("为拖动到场景视图上的每个对象调用", RiderSupportedLanguages.zh);
                eventFunction.AddParameter("sceneView", new ApiType("UnityEditor.SceneView"), new UnityApiDescriptions
                {
                    { RiderSupportedLanguages.iv, Strings.Editor_OnSceneDrag_sceneView_Description },
                    { RiderSupportedLanguages.ja, "現在のシーンビュー" },
                    { RiderSupportedLanguages.ko, "현재 씬 뷰" }, 
                    { RiderSupportedLanguages.zh, "当前场景视图" }
                });
                eventFunction.AddParameter("index", ApiType.Int, new UnityApiDescriptions
                {
                    { RiderSupportedLanguages.iv, Strings.Editor_OnSceneDrag_index_Description },
                    { RiderSupportedLanguages.ja, "DragAndDrop.objectReferences 配列へのインデックス" },
                    { RiderSupportedLanguages.ko, "DragAndDrop.objectReferences 배열로의 색인" }, 
                    { RiderSupportedLanguages.zh, "DragAndDrop.objectReferences 数组中的索引" }
                });
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
                    eventFunction.AddDescription("このエディターに対してカスタム Bounds を計算可能かどうかを検証します。", RiderSupportedLanguages.ja);
                    eventFunction.AddDescription("사용자 지정 bounds가 이 에디터에서 계산 가능한지 여부를 검사합니다.", RiderSupportedLanguages.ko);
                    eventFunction.AddDescription("验证是否可以为此编辑器计算自定义边界。", RiderSupportedLanguages.zh);
                    type.MergeEventFunction(eventFunction, apiVersion);

                    // Editor.OnGetFrameBounds has been around since at least 2017.1. First documented in 2020.2
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/SceneView/SceneView.cs#L2303
                    // https://docs.unity3d.com/2020.2/Documentation/ScriptReference/Editor.OnGetFrameBounds.html
                    eventFunction = new UnityApiEventFunction("OnGetFrameBounds", false, false,
                        new ApiType("UnityEngine.Bounds"), apiVersion,
                        undocumented: true);
                    eventFunction.AddDescription(Strings.Editor_OnGetFrameBounds_Description, RiderSupportedLanguages.iv);
                    eventFunction.AddDescription("このエディターのターゲットに対するカスタム Bounds を取得します。", RiderSupportedLanguages.ja);
                    eventFunction.AddDescription("이 에디터의 타깃에 대한 사용자 지정 bounds를 가져옵니다.", RiderSupportedLanguages.ko);
                    eventFunction.AddDescription("获取此编辑器的目标的自定义边界。", RiderSupportedLanguages.zh);
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
                eventFunction.AddDescription("修飾キーが変更されると呼び出されます。EditorApplication.modifierKeysChanged イベントを自動的に登録および登録解除します", RiderSupportedLanguages.ja);
                eventFunction.AddDescription("제어자 키가 변경될 때 호출됩니다. EditorApplication.modifierKeysChanged 이벤트를 자동으로 등록 및 등록 취소합니다", RiderSupportedLanguages.ko);
                eventFunction.AddDescription("当修改键更改时调用。自动注册和取消注册 EditorApplication.modifierKeysChanged 事件", RiderSupportedLanguages.zh);
                type.MergeEventFunction(eventFunction, apiVersion);

                // EditorWindow.ShowButton has been around since at least 2017.1. Still undocumented as of 2020.2
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/HostView.cs#L356
                // http://www.improck.com/2014/11/editorwindow-inspector-lock-icon/
                eventFunction = new UnityApiEventFunction("ShowButton", false, false, ApiType.Void, apiVersion,
                    undocumented: true);
                eventFunction.AddDescription(Strings.EditorWindow_ShowButton_Description, RiderSupportedLanguages.iv);
                eventFunction.AddDescription("エディターペインの一般的なメニューの横に小さなボタンを表示できるようにします (例: インスペクターのロックアイコン)", RiderSupportedLanguages.ja);
                eventFunction.AddDescription("에디터 패널에서 일반 메뉴 옆에 작은 버튼이 표시되도록 허용(예: 검사 도구 잠금 아이콘)", RiderSupportedLanguages.ko);
                eventFunction.AddDescription("允许编辑器窗格在通用菜单旁边显示一个小按钮(如检查器锁定图标)", RiderSupportedLanguages.zh);
                eventFunction.AddParameter("rect", new ApiType("UnityEngine.Rect"),
                    new UnityApiDescriptions
                        { { RiderSupportedLanguages.iv, Strings.EditorWindow_ShowButton_rect_Description } });
                type.MergeEventFunction(eventFunction, apiVersion);

                // EditorWindow.OnBecameVisible has been around since at least 2017.1. Still undocumented as of 2020.2
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/HostView.cs#L302
                eventFunction = new UnityApiEventFunction("OnBecameVisible", false, false, ApiType.Void, apiVersion,
                    undocumented: true);
                eventFunction.AddDescription(Strings.EditorWindow_OnBecameVisible_Description, RiderSupportedLanguages.iv);
                eventFunction.AddDescription("エディターウィンドウが開かれると呼び出されます", RiderSupportedLanguages.ja);
                eventFunction.AddDescription("에디터 창이 열렸을 때 호출됩니다", RiderSupportedLanguages.ko);
                eventFunction.AddDescription("打开编辑器窗口时调用", RiderSupportedLanguages.zh);
                type.MergeEventFunction(eventFunction, apiVersion);

                // EditorWindow.OnBecameInvisible has been around since at least 2017.1. Still undocumented as of 2020.2
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/HostView.cs#L337
                eventFunction = new UnityApiEventFunction("OnBecameInvisible", false, false, ApiType.Void, apiVersion,
                    undocumented: true);
                eventFunction.AddDescription(Strings.EditorWindow_OnBecameInvisible_Description, RiderSupportedLanguages.iv);
                eventFunction.AddDescription("エディターウィンドウが閉じられると呼び出されます", RiderSupportedLanguages.ja);
                eventFunction.AddDescription("에디터 창이 닫혔을 때 호출됩니다", RiderSupportedLanguages.ko);
                eventFunction.AddDescription("关闭编辑器窗口时调用", RiderSupportedLanguages.zh);
                type.MergeEventFunction(eventFunction, apiVersion);

                // EditorWindow.OnDidOpenScene has been around since at least 2017.1. Still undocumented as of 2020.2
                // https://github.com/Unity-Technologies/UnityCsReference/blob/2017.1/Editor/Mono/HostView.cs#L163
                eventFunction = new UnityApiEventFunction("OnDidOpenScene", false, false, ApiType.Void, apiVersion,
                    undocumented: true);
                eventFunction.AddDescription(Strings.EditorWindow_OnDidOpenScene_Description, RiderSupportedLanguages.iv);
                eventFunction.AddDescription("シーンが開かれると呼び出されます", RiderSupportedLanguages.ja);
                eventFunction.AddDescription("씬이 열렸을 때 호출됩니다", RiderSupportedLanguages.ko);
                eventFunction.AddDescription("打开场景时调用", RiderSupportedLanguages.zh);
                type.MergeEventFunction(eventFunction, apiVersion);

                if (apiVersion >= new Version(2019, 1))
                {
                    // EditorWindow.OnAddedAsTab was introduced in 2019.1. Still undocumented as of 2020.2
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/2019.1/Editor/Mono/GUI/DockArea.cs#L188
                    eventFunction = new UnityApiEventFunction("OnAddedAsTab", false, false, ApiType.Void, apiVersion,
                        undocumented: true);
                    eventFunction.AddDescription(Strings.EditorWindow_OnAddedAsTab_Description, RiderSupportedLanguages.iv);
                    eventFunction.AddDescription("エディターウィンドウがタブとして追加されると呼び出されます", RiderSupportedLanguages.ja);
                    eventFunction.AddDescription("에디터 창이 탭으로서 추가될 때 호출됩니다", RiderSupportedLanguages.ko);
                    eventFunction.AddDescription("当编辑器窗口被添加为标签页时调用", RiderSupportedLanguages.zh);
                    type.MergeEventFunction(eventFunction, apiVersion);

                    // EditorWindow.OnBeforeRemovedAsTab was introduced in 2019.1
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/2019.1/Editor/Mono/GUI/DockArea.cs#L195
                    eventFunction = new UnityApiEventFunction("OnBeforeRemovedAsTab", false, false, ApiType.Void, apiVersion,
                        undocumented: true);
                    eventFunction.AddDescription(Strings.EditorWindow_OnBeforeRemovedAsTab_Description, RiderSupportedLanguages.iv);
                    eventFunction.AddDescription("エディターウィンドウがタブとして除去されると呼び出されます", RiderSupportedLanguages.ja);
                    eventFunction.AddDescription("에디터 창이 탭으로서 제거될 때 호출됩니다", RiderSupportedLanguages.ko);
                    eventFunction.AddDescription("在将编辑器窗口从标签页中移除之前调用", RiderSupportedLanguages.zh);
                    type.MergeEventFunction(eventFunction, apiVersion);
                }

                if (apiVersion >= new Version(2019, 3))
                {
                    // EditorWindow.OnTabDetached was introduced in 2019.3
                    // https://github.com/Unity-Technologies/UnityCsReference/blob/2019.3/Editor/Mono/GUI/DockArea.cs#L940
                    eventFunction = new UnityApiEventFunction("OnTabDetached", false, false, ApiType.Void, apiVersion,
                        undocumented: true);
                    eventFunction.AddDescription(Strings.EditorWindow_OnTabDetached_Description, RiderSupportedLanguages.iv);
                    eventFunction.AddDescription("ドラッグアンドドロップ中にエディターウィンドウがデタッチされると呼び出されます", RiderSupportedLanguages.ja);
                    eventFunction.AddDescription("에디터 창 탭이 분리될 때 드래그앤드롭 중에 호출됩니다", RiderSupportedLanguages.ko);
                    eventFunction.AddDescription("拖放期间，当编辑器窗口标签页分离时调用", RiderSupportedLanguages.zh);
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
                    eventFunction.AddDescription("メインウィンドウが移動されると呼び出されます", RiderSupportedLanguages.ja);
                    eventFunction.AddDescription("메인 창이 이동될 때 호출됩니다", RiderSupportedLanguages.ko);
                    eventFunction.AddDescription("移动主窗口时调用", RiderSupportedLanguages.zh);
                    type.MergeEventFunction(eventFunction, apiVersion);
                }
            }
        }
    }
}
