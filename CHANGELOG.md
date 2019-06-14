# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0). Note that this project does not follow semantic versioning but uses version numbers based on JetBrains [Rider](https://www.jetbrains.com/rider/) and [ReSharper](https://www.jetbrains.com/resharper/) releases.

This plugin has functionality that is common to both ReSharper and Rider. It also contains a plugin for the Unity editor that is used to communicate with Rider. Changes marked with a "Rider:" prefix are specific to Rider, while changes for the Unity editor plugin are marked with a "Unity editor:" prefix. No prefix means that the change is common to both Rider and ReSharper.

## 2019.2
### Fixed
- Disable automatic cleanup of Unity messages in Rider ([#1217]https://github.com/JetBrains/resharper-unity/pull/1217)
- Fix dynamic unit tests with similar name and different child-index ([#1214]https://github.com/JetBrains/resharper-unity/pull/1214)
- Fix mdb generation ([#1182]https://github.com/JetBrains/resharper-unity/pull/1182)

## 2019.1.2
* [Commits](https://github.com/JetBrains/resharper-unity/compare/191-eap8-rtm-2019.1.0...191-eap10-rtm-2019.1.2)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/28?closed=1)

### Added
- Methods and properties defined in `UnityEditor.Build.*` are now implicitly used([#686](https://github.com/JetBrains/resharper-unity/issues/686))
- Rider: Added support for the Rider integration package used by Unity 2019.2+. No longer copies Rider plugin to Assets folder, and is loaded directly from the Rider installation folder ([#1176](https://github.com/JetBrains/resharper-unity/pull/1176))

### Fixed
- Fix parse errors in YAML for strings that begin with quotes, braces or tildes ([#1169](https://github.com/JetBrains/resharper-unity/issues/1169), [RIDER-27475](https://youtrack.jetbrains.com/issue/RIDER-27475), [#1192](https://github.com/JetBrains/resharper-unity/pull/1192))
- Fix errors in scene files for unresolved methods ([RIDER-27445](https://youtrack.jetbrains.com/issue/RIDER-27445), [#1178](https://github.com/JetBrains/resharper-unity/1178), [#1174](https://github.com/JetBrains/resharper-unity/pull/1174))
- Fix rename of script components not being able to update correctly ([#1196](https://github.com/JetBrains/resharper-unity/pull/1196))
- Rider: Fix Code Vision usage counter not always correct for methods used in scene files ([RIDER-27684](https://youtrack.jetbrains.com/issue/RIDER-27684), [#1178](https://github.com/JetBrains/resharper-unity/1178), [#1174](https://github.com/JetBrains/resharper-unity/pull/1174))
- Rider: Fix high CPU usage on Linux ([#1163](https://github.com/JetBrains/resharper-unity/issues/1163), [#1171](https://github.com/JetBrains/resharper-unity/1171))
- Rider: Fix issue with switching to play mode when debugging ([RIDER-26857](https://youtrack.jetbrains.com/issue/RIDER-26857), [#1202](https://github.com/JetBrains/resharper-unity/pull/1202))
- Rider: Fix Code Vision flickering when typing inside method ([#1203](https://github.com/JetBrains/resharper-unity/pull/1203))
- Rider: Ignore "unityhub" Ubuntu process in debug dialog ([#1210](https://github.com/JetBrains/resharper-unity/pull/1210))



## 2019.1.1

No changes



## 2019.1
* [Commits](https://github.com/JetBrains/resharper-unity/compare/183-eap13-rtm...191-eap8-rtm-2019.1.0)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/22?closed=1)

### Added
- Add rename of methods and properties used in text base assets ([#1140](https://github.com/JetBrains/resharper-unity/pull/1140))
- Add inspection and quick fix to avoid inefficient order of multiplication operations ([#1031](https://github.com/JetBrains/resharper-unity/issues/1031))
- Add warning for string literal use in `Animator.ResetTrigger` ([RIDER-24421](https://youtrack.jetbrains.com/issue/RIDER-24421), [#1035](https://github.com/JetBrains/resharper-unity/issues/1035))
- Add support for marking ECS types and fields as "in use" ([#1010](https://github.com/JetBrains/resharper-unity/issues/1010), [#1036](https://github.com/JetBrains/resharper-unity/pull/1036))
- Add gutter icon indicator for event function that "hides" a private event function in a base class ([RIDER-14698](https://youtrack.jetbrains.com/issue/RIDER-14698), [#1081](https://github.com/JetBrains/resharper-unity/pull/1081))
- Add Unity version to error report environment details ([#1103](https://github.com/JetBrains/resharper-unity/pull/1103))
- Add ability to find similar issues for performance indicators ([#1109](https://github.com/JetBrains/resharper-unity/pull/1109))
- Rider: Add performance profiling for Unity ([#1073](https://github.com/JetBrains/resharper-unity/pull/1073))
- Rider: Debug unit tests using the Debug toolbar button ([#1094](https://github.com/JetBrains/resharper-unity/pull/1094))
- Rider: Add notification to update plugin if plugin is out of date and automatic update is disabled ([RIDER-22662](https://youtrack.jetbrains.com/issue/RIDER-22662), [#963](https://github.com/JetBrains/resharper-unity/pull/963))
- Rider: Add notification when "Show Usages in Unity" is clicked and asset serialisation is not set to "force text" ([#1087](https://github.com/JetBrains/resharper-unity/pull/1087))
- Rider: Add support for git packages in Unity Explorer ([#1028](https://github.com/JetBrains/resharper-unity/pull/1028))
- Rider: Add notification if there isn't a player log to show ([#820](https://github.com/JetBrains/resharper-unity/issues/820), [#1006](https://github.com/JetBrains/resharper-unity/pull/1006) - thanks @ajon542!)
- Rider: Add dialog to prompt Mac users to install mono for new scripting runtime projects ([RIDER-24005](https://youtrack.jetbrains.com/issue/RIDER-24005), [#1080](https://github.com/JetBrains/resharper-unity/pull/1080))
- Rider: Add Unity toolbar dropdown menu for class libraries ([#1109](https://github.com/JetBrains/resharper-unity/pull/1109))
- Rider: Refresh assets in Unity after VCS pull completes ([#947](https://github.com/JetBrains/resharper-unity/issues/947), [#1085](https://github.com/JetBrains/resharper-unity/pull/1085))
- Rider: Add project name to list of debuggable players (Unity 2019.2 only) ([#1114](https://github.com/JetBrains/resharper-unity/pull/1114))
- Rider: Add all package folders to find in files index ([#1122](https://github.com/JetBrains/resharper-unity/pull/1120))
- Rider: Add icon for marketplace ([RIDER-25040](https://youtrack.jetbrains.com/issue/RIDER-25040), [#1122](https://github.com/JetBrains/resharper-unity/pull/1122))
- Rider: Add icon for git based packages ([RIDER-24093](https://youtrack.jetbrains.com/issue/RIDER-24093), [#1122](https://github.com/JetBrains/resharper-unity/pull/1122))
- Rider: Show warnings when trying to manually edit Unity asset or project files ([RIDER-23145](https://youtrack.jetbrains.com/issue/RIDER-23145), [#1135](https://github.com/JetBrains/resharper-unity/pull/1135))

### Changed
- Performance critical context now works cross files ([#1037](https://github.com/JetBrains/resharper-unity/pull/1037))
- Event functions can be grouped by base classes in Generate dialog ([#810](https://github.com/JetBrains/resharper-unity/issues/810), [#1081](https://github.com/JetBrains/resharper-unity/pull/1081))
- No longer creates a `.sln.DotSettings.user` file when YAML size heuristic is applied ([#1087](https://github.com/JetBrains/resharper-unity/pull/1087))
- Treat `AddComponent(Type)` as an expensive operation ([#1044](https://github.com/JetBrains/resharper-unity/issues/1044), [#1109](https://github.com/JetBrains/resharper-unity/pull/1109))
- Include Unity YAML files in Solution Wide Error Analysis ([#1118](https://github.com/JetBrains/resharper-unity/pull/1118))
- Updated API to 2019.2.0a9 ([#1055](https://github.com/JetBrains/resharper-unity/issues/1055), [#1056](https://github.com/JetBrains/resharper-unity/pull/1056))
- Rider: Refresh assets before running unit tests ([#1070](https://github.com/JetBrains/resharper-unity/issues/1070), [#1078](https://github.com/JetBrains/resharper-unity/pull/1078))
- Rider: Run configuration will start Unity if not already running ([#1086](https://github.com/JetBrains/resharper-unity/pull/1086))
- Rider: Improve navigation from log viewer to code ([#367](https://github.com/JetBrains/resharper-unity/issues/367), [#1071](https://github.com/JetBrains/resharper-unity/pull/1071))
- Rider: Improve running unit tests which only differ by assembly name ([RIDER-24636](https://youtrack.jetbrains.com/issue/RIDER-24636), [#1058](https://github.com/JetBrains/resharper-unity/pull/1058))
- Rider: Show Usages in Unity is run on a background thread and can be cancelled ([#1087](https://github.com/JetBrains/resharper-unity/pull/1087))
- Rider: Improve wording for Code Vision highlights of serialised fields and types ([#1087](https://github.com/JetBrains/resharper-unity/pull/1087))
- Rider: Code Vision tooltips update when Unity is/is not running ([#1109](https://github.com/JetBrains/resharper-unity/pull/1109))
- Rider: Graceful handling of out of sync Unity Editor plugin versions ([#963](https://github.com/JetBrains/resharper-unity/pull/963))
- Rider: Improve indexing time when root folder contains non-project folders ([#1120](https://github.com/JetBrains/resharper-unity/1120))
- Rider: Include Unity YAML usages in Code Vision usage counts ([#1118](https://github.com/JetBrains/resharper-unity/pull/1118))
- Unity Editor: Allow opening assets imported by ScriptedImporters ([#981](https://github.com/JetBrains/resharper-unity/issues/981), [#995](https://github.com/JetBrains/resharper-unity/pull/995))

### Fixed
- Event handler completion no longer matches on parameter types, only method names ([RIDER-22944](https://youtrack.jetbrains.com/issue/RIDER-22944), [#1081](https://github.com/JetBrains/resharper-unity/pull/1081))
- No longer shows code completion after `[SerializeField]` attribute ([RIDER-22943](https://youtrack.jetbrains.com/issue/RIDER-22943), [#1081](https://github.com/JetBrains/resharper-unity/pull/1081))
- Correctly resolves generated code when there is a namespace called `System` in the current context ([RIDER-22605](https://youtrack.jetbrains.com/issue/RIDER-22605), [#1081](https://github.com/JetBrains/resharper-unity/pull/1081))
- Correctly handle code generation of event functions that are implemented as virtual methods ([RIDER-17104](https://youtrack.jetbrains.com/issue/RIDER-17104), [#603](https://github.com/JetBrains/resharper-unity/issues/603), [#879](https://github.com/JetBrains/resharper-unity/issues/879), [#1081](https://github.com/JetBrains/resharper-unity/pull/1081))
- Correctly update name of existing method declaration with code completion ([RIDER-9555](https://youtrack.jetbrains.com/issue/RIDER-9555), [RIDER-14037](https://youtrack.jetbrains.com/issue/RIDER-14037), [#0181](https://github.com/JetBrains/resharper-unity/pull/1081))
- Only show event function completion in classes, not structs or interfaces ([RIDER-14147](https://youtrack.jetbrains.com/issue/RIDER-14147), [#420](https://github.com/JetBrains/resharper-unity/issues/420), [#1081](https://github.com/JetBrains/resharper-uniyt/pull/1081))
- Respect existing access modifier in code completion ([RIDER-15290](https://youtrack.jetbrains.com/issue/RIDER-15290), [#1081](https://github.com/JetBrains/resharper-unity/pull/1081))
- Remove incorrect method signature warning if method is implementing a virtual method ([#876](https://github.com/JetBrains/resharper-unity/issues/876), [#1081](https://github.com/JetBrains/resharper-unity/pull/1081))
- Fix spelling mistake in issue suppression comment ([#1083](https://github.com/JetBrains/resharper-unity/issues/1083), [#1109](https://github.com/JetBrains/resharper-unity/pull/1109))
- Fix validation exceptions ([RIDER-26102](https://youtrack.jetbrains.com/issue/RIDER-26102), [RIDER-26109](https://youtrack.jetbrains.com/issue/RIDER-26109), [RIDER-26103](https://youtrack.jetbrains.com/issue/RIDER-26103), [#1108](https://github.com/JetBrains/resharper-unity/pull/1108))
- Fix exception when project contains empty YAML file ([RIDER-25787](https://youtrack.jetbrains.com/issue/RIDER-25787), [#1124](https://github.com/JetBrains/resharper-unity/pull/1124))
- Fix parse error in YAML files with multiline text containing single quotes ([RIDER-26849](https://youtrack.jetbrains.com./issue/RIDER-26849), [#1145](https://github.com/JetBrains/resharper-unity/pull/1145))
- Fix parse error in ShaderLab attribute values ([RIDER-26909](https://youtrack.jetbains.com/issue/RIDER-26909), [#857](https://github.com/JetBrains/resharper-unity/issues/857), [#1149](https://github.com/JetBrains/resharper-unity/pull/1149))
- Fix invalid references to scripts or event handlers in read only/referenced packages ([RIDER-27009](https://youtrack.jetbrains.com/issue/RIDER-27009), [#1153](https://github.com/JetBrains/resharper-unity/pull/1153))
- Fix rename of `MonoBehaviour` based classes being blocked by reported conflicts ([RIDER-27053](https://youtrack.jetbrains.com/issue/RIDER-27053), [#1153](https://github.com/JetBrains/resharper-unity/pull/1153))
- Rider: Fix minor UI annoyances in "Attach to Unity process" dialog ([#1114](https://github.com/JetBrains/resharper-unity/pull/1114))
- Rider: Show packages from correct per-project cache in Unity Explorer
- Rider: Correctly handle file/git based packages in Unity Explorer ([RIDER-25971](https://youtrack.jetbrains.com/issue/RIDER-25971), [#1095](https://github.com/JetBrains/resharper-unity/issue/1095) [#1099](https://github.com/JetBrains/resharper-unity/pull/1099))
- Rider: Fix exception causing Unity Explorer to disappear ([RIDER-25760](https://youtrack.jetbrains.com/issue/RIDER-25760), [#1096](https://github.com/JetBrains/resharper-unity/pull/1096))
- Rider: Fix exception showing auto-save notification ([RIDER-25830](https://youtrack.jetbrains.com/issue/RIDER-25830), [#1092](https://github.com/JetBrains/resharper-unity/issues/1092))
- Rider: Fix exception grouping Find Usages results ([RIDER-26119](https://youtrack.jetbrains.com/issue/RIDER-26119), [#1115](https://github.com/JetBrains/resharper-unity/issues/1115))
- Rider: Fix creation of Unity class library project if can't find Unity install ([#1013](https://github.com/JetBrains/resharper-unity/issue/1013), [#1014](https://github.com/JetBrains/resharper-unity/pull/1014))
- Rider: Improve error handling while looking for Toolbox installs ([#1089](https://github.com/JetBrains/resharper-unity/issues/1089), [RIDER-25706](https://youtrack.jetbrains.com/issue/RIDER-25706), [#1090](https://github.com/JetBrains/resharper-unity/pull/1090))
- Rider: Fix exception with Code Vision highlights ([RIDER-26108](https://youtrack.jetbrains.com/issue/RIDER-26108), [RIDER-26156](https://youtrack.jetbrains.com/issue/RIDER-26156), [#1109](https://github.com/JetBrains/resharper-unity/issues/1109))
- Rider: Fix exception reading `EditorInstance.json` ([RIDER-26124](https://youtrack.jetbrains.com/issue/RIDER-26124), [#1111](https://github.com/JetBrains/resharper-unity/pull/1111))
- Unity Editor: Use unique name for log file ([#1020](https://github.com/JetBrains/resharper-unity/pull/1020))
- Unity Editor: Don't call Unity API in batch mode ([#1020](https://github.com/JetBrains/resharper-unity/pull/1020))
- Unity Editor: Fix exception during Unity shutdown ([RIDER-19688](https://youtrack.jetbrains.com/issue/RIDER-19688), [#979](https://github.com/JetBrains/resharper-unity/pull/979))
- Rider: Fix Rider extra reloading projects on calling Refresh from Rider, applies to Unity pre-2018 ([#1116](https://github.com/JetBrains/resharper-unity/pull/1116)
- Unity Editor: Fix issue connecting to editor when Rider was not default editor at startup ([RIDER-26142](https://youtrack.jetbrains.com/issue/RIDER-26142), [#1111](https://github.com/JetBrains/resharper-unity/pull/1111))



## 2018.3.3
* [Commits](https://github.com/JetBrains/resharper-unity/compare/183-eap12-rtm...183-eap13-rtm)

### Fixed
- Unity Editor: Fix finding install path from JetBrains Toolbox ([RIDER-24173](https://youtrack.jetbrains.com/issue/RIDER-24173), [#1024](https://github.com/JetBrains/resharper-unity/pull/1024))



## 2018.3.2
* [Commits](https://github.com/JetBrains/resharper-unity/compare/183-eap11-rtm...183-eap12-rtm)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/26?closed=1)

### Added
- Unity editor: Add extra logging for switching to play mode and background refresh of assets (#987)

### Changed
- Mark more methods as expensive inside a performance critical context (#1000)
- Improve performance of rename and find usages with YAML files (#983)
- Improve performance of typing in YAML files, by incrementally re-parsing only the YAML document that contains the change (#993)
- Remove repeated use of project name from Unity Explorer when under assembly definition (#982, #989)
- Changed unresolved symbol error in `GetComponent`, `AddComponent` and `ScriptableObject.CreateInstance` to a configurable warning ([RIDER-23429](https://youtrack.jetbrains.com/issue/RIDER-23429), #1003)

### Fixed
- Fix processing hierarchy for YAML scene files (#985)
- Rider: Fix implicitly referenced system assemblies referencing incorrect Mono version in generated project files (#988, #992)
- Unity editor: Fix merging different game objects in find results window (#985)



## 2018.3 for ReSharper - 2013-01-17
* For ReSharper 2018.3
* Based on work in progress 2018.3.2 for Rider
* [Commits](https://github.com/JetBrains/resharper-unity/compare/183-eap11-rtm...183-eap11-rtm-resharper)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/26?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/183-eap11-rtm-resharper)

### Added
- Add parsing of method and class usage from scene, prefab and asset files (#263, [RIDER-7460](https://youtrack.jetbrains.com/issue/RIDER-7460), #870, #873, #903, #921, [RIDER-21907](https://youtrack.jetbrains.com/issue/RIDER-21907), [RIDER-21897](https://youtrack.jetbrains.com/issue/RIDER-21897), #943, #949)
- Add "Unity event handler" gutter icon to method and property setters registered to a Unity event via the Unity editor
- Correctly mark event handlers as in use
- Unity files appear in _Find Usages_ for event handlers and classes deriving from `MonoBehaviour`, grouped by type, component and object
- Disable rename for event handler methods to prevent breaking the registration in scene files
- Add Code Vision highlighting for implicitly used classes, methods, properties and fields
- Add option to hide gutter icons (automatically disabled when Code Vision enabled)
- Add performance indicators for performance critical code contexts (#816, #878)
- Add performance indicator for null comparison against Unity object ([RIDER-19297](https://youtrack.jetbrains.com/issue/RIDER-19297))
- Add performance indicator for `AddComponent` as an expensive method invocation ([RIDER-19299](https://youtrack.jetbrains.com/issue/RIDER-19299))
- Add performance indicator for `Find` methods ([RIDER-19287](https://youtrack.jetbrains.com/issue/RIDER-19287))
- Add performance indicator for `GetComponent` methods ([RIDER-19288](https://youtrack.jetbrains.com/issue/RIDER-19288))
- Add performance indicator for indirect invocation of expensive methods (#816)
- Add inspection to avoid string based method invocation ([RIDER-19295](https://youtrack.jetbrains.com/issue/RIDER-19295), #798)
- Add inspection and Quick Fix to avoid repeat access of properties that make native calls ([RIDER-19289](https://youtrack.jetbrains.com/issue/RIDER-19289), #797)
- Add inspection and Quick Fix to avoid instantiating an object and setting parent transform immediately after ([RIDER-19298](https://youtrack.jetbrains.com/issue/RIDER-19298), #797)
- Add inspection and Quick Fix to use static `int` field to access graphics properties instead of string access ([RIDER-19296](https://youtrack.jetbrains.com/issue/RIDER-19296), #783))
- Add inspection and Quick Fix to use non-allocating physics functions ([RIDER-19290](https://youtrack.jetbrains.com/issue/RIDER-19290), #784)
- Add Context Action to move expensive expression to `Start`, `Awake` or outside of loop ([RIDER-19297](https://youtrack.jetbrains.com/issue/RIDER-19297), [RIDER-19291](https://youtrack.jetbrains.com/issue/RIDER-19291), [RIDER-19287](https://youtrack.jetbrains.com/issue/RIDER-19287), #878)
- Add inspection and Quick Fix to avoid string based versions of `GetComponent`, `AddComponent` and `ScriptableObject.CreateInstance` ([RIDER-19293](https://youtrack.jetbrains.com/issue/RIDER-19293), #763)
- Add inspection and Quick Fix for correct method signature for `DrawGizmo` attribute (#36, #772)
- Add inspection for calling `base.OnGUI` in `PropertyDrawer` derived class (#886, thanks @vinhui!)
- Add suspicious comparison warning if comparing two Unity objects which don't have a common subtype ([RIDER-18671](https://youtrack.jetbrains.com/issue/RIDER-18671), #7864))
- Add "Why is ReSharper/Rider suggesting this?" for most new inspections
- Add code completion, rename and find usages to string literal component and scriptable object type names (#835)
- Add file template for `[InitializeOnLoad]` class (#795)

### Changed
- Updated to ReSharper 2018.3
- Improve performance of rename and find usages with YAML files (#983)
- Automatically disable YAML parsing if the project is too large (#973)
- Update API to Unity 2018.3.0b9 (#819, #897)
- Mark event handler methods and property setters as in use if they're declared on a base type (#922)
- Remove duplicate event functions from code completion (#685, #823)
- Improve redundant event function warnings ([RIDER-19894](https://youtrack.jetbrains.com/issue/RIDER-19894), #794)
- Stop _Generate Code_ dialog selecting all event functions by default when called from the gutter icon or Code Vision marker ([RIDER-22211](https://youtrack.jetbrains.com/issue/RIDER-22211), #939)
- Prevent Respeller running on `.asmdef` files ([RIDER-17701](https://youtrack.jetbrains.com/issue/RIDER-17701), #748)

### Fixed
- Fix processing hierarchy for YAML scene files (#985)
- Fix C# language level override incorrectly handling `latest` (#871)
- Fix to stop generating `readonly` modifier when converting auto property to property with serialised backing field (#892, #893)
- Fix bug in ShaderLab parsing `Blend` operations (#723, #785)
- Fix exception after renaming type (#820, [RIDER-18699](https://youtrack.jetbrains.com/issue/RIDER-18699))



## 2018.3.1 - 2018-12-26
* For Rider 2018.3.1. Not released for ReSharper
* [Commits](https://github.com/JetBrains/resharper-unity/compare/183-eap10-rtm...183-eap11-rtm)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/23?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/183-eap11-rtm)

### Added
- Automatically disable YAML parsing if the project is too large (#973)

### Fixed
- Rider: Fix reference to `UnityEditor.iOS.Extensions.Xcode.dll` in generated projects (#974, #976)
- Rider: Fix bug in setting reference to `UnityEditor.iOS.Extensions.Xcode.dll` in generated projects (#976)
- Rider: Fix bug failing to copy script assemblies during debugging (#964)



## 2018.3 - 2018-12-17
* For Rider 2018.3. Not released for ReSharper
* [Commits](https://github.com/JetBrains/resharper-unity/compare/182-eap12-2018.2.3...183-eap10-rtm)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/19?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/183-eap10-rtm)

### Added
- Add parsing of method and class usage from scene, prefab and asset files (#263, [RIDER-7460](https://youtrack.jetbrains.com/issue/RIDER-7460), #870, #873, #903, #921, [RIDER-21907](https://youtrack.jetbrains.com/issue/RIDER-21907), [RIDER-21897](https://youtrack.jetbrains.com/issue/RIDER-21897), #943, #949)
- Add "Unity event handler" gutter icon to method and property setters registered to a Unity event via the Unity editor
- Correctly mark event handlers as in use
- Unity files appear in _Find Usages_ for event handlers and classes deriving from `MonoBehaviour`, grouped by type, component and object
- Disable rename for event handler methods to prevent breaking the registration in scene files
- Add Code Vision highlighting for implicitly used classes, methods, properties and fields
- Add option to hide gutter icons (automatically disabled when Code Vision enabled)
- Add performance indicators for performance critical code contexts (#816, #878)
- Add performance indicator for null comparison against Unity object ([RIDER-19297](https://youtrack.jetbrains.com/issue/RIDER-19297))
- Add performance indicator for `AddComponent` as an expensive method invocation ([RIDER-19299](https://youtrack.jetbrains.com/issue/RIDER-19299))
- Add performance indicator for `Find` methods ([RIDER-19287](https://youtrack.jetbrains.com/issue/RIDER-19287))
- Add performance indicator for `GetComponent` methods ([RIDER-19288](https://youtrack.jetbrains.com/issue/RIDER-19288))
- Add performance indicator for indirect invocation of expensive methods (#816)
- Add inspection to avoid string based method invocation ([RIDER-19295](https://youtrack.jetbrains.com/issue/RIDER-19295), #798)
- Add inspection and Quick Fix to avoid repeat access of properties that make native calls ([RIDER-19289](https://youtrack.jetbrains.com/issue/RIDER-19289), #797)
- Add inspection and Quick Fix to avoid instantiating an object and setting parent transform immediately after ([RIDER-19298](https://youtrack.jetbrains.com/issue/RIDER-19298), #797)
- Add inspection and Quick Fix to use static `int` field to access graphics properties instead of string access ([RIDER-19296](https://youtrack.jetbrains.com/issue/RIDER-19296), #783))
- Add inspection and Quick Fix to use non-allocating physics functions ([RIDER-19290](https://youtrack.jetbrains.com/issue/RIDER-19290), #784)
- Add Context Action to move expensive expression to `Start`, `Awake` or outside of loop ([RIDER-19297](https://youtrack.jetbrains.com/issue/RIDER-19297), [RIDER-19291](https://youtrack.jetbrains.com/issue/RIDER-19291), [RIDER-19287](https://youtrack.jetbrains.com/issue/RIDER-19287), #878)
- Add inspection and Quick Fix to avoid string based versions of `GetComponent`, `AddComponent` and `ScriptableObject.CreateInstance` ([RIDER-19293](https://youtrack.jetbrains.com/issue/RIDER-19293), #763)
- Add inspection and Quick Fix for correct method signature for `DrawGizmo` attribute (#36, #772)
- Add inspection for calling `base.OnGUI` in `PropertyDrawer` derived class (#886, thanks @vinhui!)
- Add suspicious comparison warning if comparing two Unity objects which don't have a common subtype ([RIDER-18671](https://youtrack.jetbrains.com/issue/RIDER-18671), #7864))
- Add "Why is ReSharper/Rider suggesting this?" for most new inspections
- Add code completion, rename and find usages to string literal component and scriptable object type names (#835)
- Add file template for `[InitializeOnLoad]` class (#795)
- Rider: Syntax highlighting for YAML files
- Rider: Add entity component data to debugger (#720)
- Rider: Add components and children of `GameObject` to debugger (#838)
- Rider: Add child game objects of `Scene` to debugger (#838)
- Rider: Add double click to start debugger in _Attach to Unity Process_ dialog (#814)
- Rider: Add setting to disable sending Unity Console to Rider (#829)
- Rider: Add prefix, suffix and "disable inspections" options to custom serialised fields naming rule (#930, #928, [RIDER-22026](https://youtrack.jetbrains.com/issue/RIDER-22036), [RIDER-21193](https://youtrack.jetbrains.com/issue/RIDER-21193))
- Rider: Ensure code is compiled before running tests via Unity (#916, #931)
- Unity editor: Show version of editor plugin on Rider plugin page (#818, #822)
- Unity editor: Generate projects once per Unity startup (#874, #884, [RIDER-21237](https://youtrack.jetbrains.com/issue/RIDER-21237), [RIDER-21035](https://youtrack.jetbrains.com/issue/RIDER-21035))
- Unity editor: Add editor window to show results of Find Usages (#918)
- Unity editor: Add action to start Unity from Rider (#942, #946)

### Changed
- Update API to Unity 2018.3.0b9 (#819, #897)
- Mark event handler methods and property setters as in use if they're declared on a base type (#922)
- Remove duplicate event functions from code completion (#685, #823)
- Improve redundant event function warnings ([RIDER-19894](https://youtrack.jetbrains.com/issue/RIDER-19894), #794)
- Stop _Generate Code_ dialog selecting all event functions by default when called from the gutter icon or Code Vision marker ([RIDER-22211](https://youtrack.jetbrains.com/issue/RIDER-22211), #939)
- Prevent Respeller running on `.asmdef` files ([RIDER-17701](https://youtrack.jetbrains.com/issue/RIDER-17701), #748)
- Rider: Updated icons in Unity Explorer (#836, [RIDER-18475](https://youtrack.jetbrains.com/issue/RIDER-18475))
- Rider: Set font similar to Console for Unity Log View (#842)
- Rider: Explicit background refresh assets action will force AppDomain reload (#846)
- Rider: Detect non-default Unity installed (#854, #850)
- Rider: Refine auto-save notification advice (#707, #877)
- Rider: Preserve custom editor location (#872)
- Unity editor: Generated projects for Unity 2018.1+ no longer require .NET or Mono installed (#756)
- Unity editor: Add HintPaths to system libraries in generated projects ([RIDER-20161](https://youtrack.jetbrains.com/issue/RIDER-20161), #832)
- Unity editor: Speed up writing JSON file during plugin startup (#753)
- Unity editor: Stop capturing log events unless connected to a Unity project (#946, [RIDER-22361](https://youtrack.jetbrains.com/issue/RIDER-22361))

### Fixed
- Fix C# language level override incorrectly handling `latest` (#871)
- Fix to stop generating `readonly` modifier when converting auto property to property with serialised backing field (#892, #893)
- Fix bug in ShaderLab parsing `Blend` operations (#723, #785)
- Fix exception after renaming type (#820, [RIDER-18699](https://youtrack.jetbrains.com/issue/RIDER-18699))
- Rider: Fix filter in Unity log view to be case insensitive (#761)
- Rider: Fix running unit tests via Unity on Mac ([RIDER-20514](https://youtrack.jetbrains.com/issue/RIDER-20514), #530)
- Rider: Fix Unity Explorer not showing on Linux (#792, #793)
- Rider: Fix Unity Explorer and packages with fully qualified paths (#952)
- Rider: Fix list of debuggable Unity apps to include apps started from symlinks (#713)
- Rider: Fix prompt for `npm install` in `package.json` in Unity packages (#703, #789)
- Rider: Fix for ignored tests displaying wrong result status (#657, #718)
- Unity editor: Fix `mcs.rsp`/`csc.rsp` processing for references in quotes
- Unity editor: Fix adding reference to `UnityEditor.iOS.Extensions.Xcode.dll` for Unity installed from Hub (#843)


## 2018.2.3 - 2018-09-13
* For Rider 2018.2.3. Not released for ReSharper
* [Commits](https://github.com/JetBrains/resharper-unity/compare/182-eap11-2018.2.2...182-eap12-2018.2.3)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/24?closed=1)
* [Tag](https://github.com/JetBrains/resharper-unity/releases/tag/182-eap12-2018.2.3)

## Added
- Unity editor: Disable plugin when Unity is in batch mode (#776, [RIDER-19688](https://youtrack.jetbrains.com/issue/RIDER-19688))



## 2018.2.2 - 2018-09-11
* For Rider 2018.2.2. Not released for ReSharper
* [Commits](https://github.com/JetBrains/resharper-unity/compare/182-eap10-2018.2.1...182-eap11-2018.2.2)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/21?closed=1)
* [Tag](https://github.com/JetBrains/resharper-unity/releases/tag/182-eap11-2018.2.2)

### Added
- Rider: Add an action to the Unity toolbar to manually install the editor plugin (#765)

### Changed
- Improve performance of code completion for event functions (#471)
- Rider: Improved text in notifications when the editor plugin is installed or updated, or a project is opened as a folder (#756)
- Unity editor: Speed up initialising logging on each plugin reload (#750)
- Unity editor: Add reference to `Microsoft.CSharp.dll` to generated projects (#721, #740)

### Fixed
- Fix incorrect implicit usage inspections ([RIDER-19408](https://youtrack.jetbrains.com/issue/RIDER-19408), #760)
- Rider: Fix missing file type registration for some shader files (#717, #741)
- Rider: Fix exception when pausing a running game from Rider ([RIDER-19401](https://youtrack.jetbrains.com/issue/RIDER-19401), #758)
- Unity editor: Fix `LangVersion` not correctly set in generated projects on older versions of Unity (#751, #752)
- Unity editor: Fix projects not compiling with `TargetFrameworkAttribute` errors (#747, [RIDER-17390](https://youtrack.jetbrains.com/issue/RIDER-17390))
- Unity editor: Fix exception with plugin on older versions of Unity (#762, #766)



## 2018.2 for ReSharper - 2018-09-02
* For ReSharper 2018.2
* This is based on the 2018.2.1 release for Rider
* [Commits](https://github.com/JetBrains/resharper-unity/compare/182-eap9-rtm…182-eap9-rtm-resharper)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/16?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/182-eap9-rtm-resharper)

### Added
- Add support for `.asmdef` files (#283)
- Recognise custom serializable classes and handle serialized fields and usge (#419, [RIDER-9341](https://youtrack.jetbrains.com/issue/RIDER-9341), [RIDER-12239](https://youtrack.jetbrains.com/issue/RIDER-12239))
- Add undocumented API methods in `AssetPostprocessor` (`OnGeneratedCSProject` and `OnGeneratedSlnSolution`)
- Add redundant `SerializeField` attribute on readonly field inspection, plus quick fix (#503, #586)
- Add redundant `HighlightInInspector` attribute on serialised field, plus quick fix (#585, #586)
- Add Context Actions to toggle `HideInInspector` attribute on serialised fields (#494, #586)
- Add `FormerlySerializedAs` attribute when renaming a serialised field (#54, #659, [RIDER-12298](https://youtrack.jetbrains.com/issue/RIDER-12298), [RIDER-17887](https://youtrack.jetbrains.com/issue/RIDER-17887))
- Add redundant `FormerlySerializedAs` attribute inspection and quick fix, with code wiki entry
- Add possible mis-application of `FormerlySerializedAs` attribute on multiple field declaration, with quick fix and code wiki entry
- Add inspection for usage of `Camera.main` in `Update` methods (#196)
- Add `sprop` and `sfield` Live Templates (#565)
- Mark potential event handler methods and property setters as in use (#625, [RIDER-17276](https://youtrack.jetbrains.com/issue/RIDER-17276))
- Add ShaderLab colour scheme settings page ([RIDER-17305](https://youtrack.jetbrains.com/issue/RIDER-17305))
- Rider: Add Packages node to Unity Explorer (#476, #629)
- Rider: Add Scratches node to Unity Explorer (#629)
- Rider: Open editor and player log from Unity Log View tool window (#575)
- Rider: Add text filter to Unity Log View (#599)
- Rider: Add collapsing similar log items to Unity Log View (#512)
- Rider: Add ShaderLab colour scheme settings page ([RIDER-17305](https://youtrack.jetbrains.com/issue/RIDER-17305))
- Rider: Add Attach to Unity Process action to Unity actions dropdown

### Changed
- Updated to ReSharper 2018.2
- Improve performance of code completion for event functions (#471)
- Update API details to 2018.2.0b9 (#611, #613)
- Consolidate multiple incorrect method signature inspections into one, with quick fix (#534)
- Rework make serialised/non-serialised field context actions (#583, #586)
- Serialised field Context Action and Quick Fixes work correctly with multiple field declarations (#586)
- Don't show incorrect "always false" warning for "this == null" in Unity types (#368)
- Remove highlighted background for Cg blocks in ShaderLab files ([RIDER-16438](https://youtrack.jetbrains.com/issue/RIDER-16438))
- Rider: Updated icons for run configurations ([RIDER-18576](https://youtrack.jetbrains.com/issue/RIDER-18576), #694)
- Rider: Advanced integration feature (play/pause, etc.) available in all solutions in a Unity project folder (#581)

### Fixed
- Fix ShaderLab highlighting of keywords ([RIDER-17287](https://youtrack.jetbrains.com/issue/RIDER-17287))
- Fix rename's "find in text" renaming non-text elements in ShaderLab files
- Fix Unity specific inspections not showing in Solution Wide Errors tool window (#680)
- Rider: Fix ShaderLab highlighting of keywords ([RIDER-17287](https://youtrack.jetbrains.com/issue/RIDER-17287))
- Rider: Fix list of Unity players in Attach to Unity Process dialog (#634, #650, [RIDER-17130](https://youtrack.jetbrains.com/issue/RIDER-17130))
- Rider: Use correct IP address when attaching debugging to remote player (#650, [RIDER-17130](https://youtrack.jetbrains.com/issue/RIDER-17130))
- Rider: Fixed showing tool windows after hiding for Unity projects
- Rider: Fix incorrect connection icon shown when Unity is in play mode ([RIDER-15758](https://youtrack.jetbrains.com/issue/RIDER-15758)">)
- Rider: Enable editing of Unity specific Live Templates (#654)
- Rider: Prevent editor plugin being installed each time project is loaded (#656)
- Rider: Show a notification if the project is incorrectly opened as a folder (#658)
- Rider: Show a meaningful message when trying to run Unity tests under dotCover ([RIDER-17815](https://youtrack.jetbrains.com/issue/RIDER-17815))
- Unity editor: Add option to disable reloading assemblies during Play mode. Only for Unity 2018.1 and below (#520)
- Unity editor: Only write `.csproj` file to disk if it's changed (requires Unity 2018.1+)
- Unity editor: Fix crash with Unity 5.6.x (#660)
- Unity editor: Support Roslyn compiler response files (`csc.rsp`) (#690)



## 2018.2.1 - 2018-08-30
* For Rider 2018.2.1. Not released for ReSharper
* [Commits](https://github.com/JetBrains/resharper-unity/compare/182-eap9-rtm...182-eap10-2018.2.1)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/20?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/182-eap10-2018.2.1)

### Fixed
- Unity editor: Fix project failing to load due to Unicode issue (#727, #732)
- Unity editor: Fix response file defines and references not being applied to generated project files (#729, #735)



## 2018.2 - 2018-08-23
* For Rider 2018.2. Not released for ReSharper
* [Commits](https://github.com/JetBrains/resharper-unity/compare/wave12-2018.1.2-rtm...182-eap9-rtm)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/16?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/182-eap9-rtm)

### Added
- Add support for `.asmdef` files (#283)
- Recognise custom serializable classes and handle serialized fields and usge (#419, [RIDER-9341](https://youtrack.jetbrains.com/issue/RIDER-9341), [RIDER-12239](https://youtrack.jetbrains.com/issue/RIDER-12239))
- Add undocumented API methods in `AssetPostprocessor` (`OnGeneratedCSProject` and `OnGeneratedSlnSolution`)
- Add redundant `SerializeField` attribute on readonly field inspection, plus quick fix (#503, #586)
- Add redundant `HighlightInInspector` attribute on serialised field, plus quick fix (#585, #586)
- Add Context Actions to toggle `HideInInspector` attribute on serialised fields (#494, #586)
- Add `FormerlySerializedAs` attribute when renaming a serialised field (#54, #659, [RIDER-12298](https://youtrack.jetbrains.com/issue/RIDER-12298), [RIDER-17887](https://youtrack.jetbrains.com/issue/RIDER-17887))
- Add redundant `FormerlySerializedAs` attribute inspection and quick fix, with code wiki entry
- Add possible mis-application of `FormerlySerializedAs` attribute on multiple field declaration, with quick fix and code wiki entry
- Add inspection for usage of `Camera.main` in `Update` methods (#196)
- Add `sprop` and `sfield` Live Templates (#565)
- Mark potential event handler methods and property setters as in use (#625, [RIDER-17276](https://youtrack.jetbrains.com/issue/RIDER-17276))
- Add ShaderLab colour scheme settings page ([RIDER-17305](https://youtrack.jetbrains.com/issue/RIDER-17305))
- Rider: Add Packages node to Unity Explorer (#476, #629)
- Rider: Add Scratches node to Unity Explorer (#629)
- Rider: Open editor and player log from Unity Log View tool window (#575)
- Rider: Add text filter to Unity Log View (#599)
- Rider: Add collapsing similar log items to Unity Log View (#512)
- Rider: Add ShaderLab colour scheme settings page ([RIDER-17305](https://youtrack.jetbrains.com/issue/RIDER-17305))
- Rider: Add Attach to Unity Process action to Unity actions dropdown

### Changed
- Update API details to 2018.2.0b9 (#611, #613)
- Consolidate multiple incorrect method signature inspections into one, with quick fix (#534)
- Rework make serialised/non-serialised field context actions (#583, #586)
- Serialised field Context Action and Quick Fixes work correctly with multiple field declarations (#586)
- Don't show incorrect "always false" warning for "this == null" in Unity types (#368)
- Remove highlighted background for Cg blocks in ShaderLab files ([RIDER-16438](https://youtrack.jetbrains.com/issue/RIDER-16438))
- Rider: Updated icons for run configurations ([RIDER-18576](https://youtrack.jetbrains.com/issue/RIDER-18576), #694)
- Rider: Advanced integration feature (play/pause, etc.) available in all solutions in a Unity project folder (#581)

### Fixed
- Fix ShaderLab highlighting of keywords ([RIDER-17287](https://youtrack.jetbrains.com/issue/RIDER-17287))
- Fix rename's "find in text" renaming non-text elements in ShaderLab files
- Fix Unity specific inspections not showing in Solution Wide Errors tool window (#680)
- Rider: Fix ShaderLab highlighting of keywords ([RIDER-17287](https://youtrack.jetbrains.com/issue/RIDER-17287))
- Rider: Fix list of Unity players in Attach to Unity Process dialog (#634, #650, [RIDER-17130](https://youtrack.jetbrains.com/issue/RIDER-17130))
- Rider: Use correct IP address when attaching debugging to remote player (#650, [RIDER-17130](https://youtrack.jetbrains.com/issue/RIDER-17130))
- Rider: Fixed showing tool windows after hiding for Unity projects
- Rider: Fix incorrect connection icon shown when Unity is in play mode ([RIDER-15758](https://youtrack.jetbrains.com/issue/RIDER-15758)">)
- Rider: Enable editing of Unity specific Live Templates (#654)
- Rider: Prevent editor plugin being installed each time project is loaded (#656)
- Rider: Show a notification if the project is incorrectly opened as a folder (#658)
- Rider: Show a meaningful message when trying to run Unity tests under dotCover ([RIDER-17815](https://youtrack.jetbrains.com/issue/RIDER-17815))
- Unity editor: Add option to disable reloading assemblies during Play mode. Only for Unity 2018.1 and below (#520)
- Unity editor: Only write `.csproj` file to disk if it's changed (requires Unity 2018.1+)
- Unity editor: Fix crash with Unity 5.6.x (#660)
- Unity editor: Support Roslyn compiler response files (`csc.rsp`) (#690)



## 2018.1.2 - 2018-05-28
* For Rider 2018.1.2. Not released for ReSharper
* Note that there are no changes to the plugin bundled with Rider 2018.1.3 or 2018.1.4
* [Commits](https://github.com/JetBrains/resharper-unity/compare/wave12-2018.1.1-rtm…wave12-2018.1.2-rtm)
* [Tag](https://github.com/JetBrains/resharper-unity/releases/tag/wave12-eap13-2018.1.2-rtm)

### Fixed
- Unity editor: Don't fail to parse version numbers with no minor part
- Unity editor: Fix Rider's BringToFront after opening script from Unity
- Unity editor: Display link to script to force project generation processor order (#528)



## 2018.1.1 - 2018-05-25
* For Rider 2018.1.1. Not released for ReSharper
* [Commits](https://github.com/JetBrains/resharper-unity/compare/wave12-eap9-rtm...wave12-2018.1.1-rtm)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/17?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/wave12-eap12-2018.1.1-rtm)

### Added
- Add Unity version to opt-in statistics (#486)
- Rider: Add option to run tests via Unity or with standard nunit runner (#546)
- Rider: Support theory unit tests running in Unity (#428, #551)
- Rider: Add timestamp to log message (#524)
- Rider: Add option to hide unnecessary tool windows (#555)
- Unity editor: Add setting to override `LangVersion` (#529)

### Changed
- Rider: Avoid auto-refresh during play mode (#516)
- Rider: Improve Unity version detection to install correct plugin
- Rider: Use local documentation from Unity Hub, if available (#548)
- Unity editor: Improve performance of entering play mode (#527, #532)
- Unity editor: Simplify presentation of Rider installed from standalone installer
- Unity editor: Improve path locator for Linux (#561)
- Unity editor: Add reference to Unity's modular assemblies in generated projects (#562, [RIDER-15934](https://youtrack.jetbrains.com/issue/15934))

### Fixed
- Fix extra file types not recognised as shader files ([RIDER-14756](https://youtrack.jetbrains.com/issue/RIDER-14756), #547)
- Rider: Fix step/refresh actions only working once (#522)
- Rider: Fix deadlock capturing Unity logs ([RIDER-15081](https://youtrack.jetbrains.com/issue/RIDER-15081])
- Rider: Fix freeze while capturing too many log messages ([RIDER-15909](https://youtrack.jetbrains.com/issue/RIDER-15909))
- Rider: Fix showing new play mode log messages with edit filter enabled (#559)
- Rider: Fix Rider deleting "Default" run configurations ([RIDER-15321](https://youtrack.jetbrains.com/issue/RIDER-15321], #525)
- Rider: Fix background asset refresh happening during a refactoring (#535, #542)
- Rider: Fix prompt to choose which editor instance to debug, even when only one instance is running (#556)
- Unity editor: Fix post-processing `TargetFrameworkVersion` for Mono 2 profile
- Unity editor: Generate references from `mcs.rsp` correctly ([RIDER-15093](https://youtrack.jetbrains.com/issue/RIDER-15093), #518)
- Unity editor: Fix file path check on Linux
- Unity editor: Fix running unit tests on Unity 5.6.3 (#531)
- Unity editor: Fix running unit tests on Unity 2018.2
- Unity editor: Fix running row tests
- Unity editor: Fix logs only collected on main thread ([RIDER-15522](https://youtrack.jetbrains.com/issue/RIDER-15522))
- Unity editor: Fix all log entries recorded as in edit mode (#552)
- Unity editor: Fix directory not found exception on Linux (#544)



## 2018.1.0.380 for ReSharper - 2018-06-16
* For ReSharper 2018.1 and ReSharper 2018.1.2
* [Commits](https://github.com/JetBrains/resharper-unity/compare/wave12-eap9-rtm...wave12-2018.1-api-fix)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/wave12-2018.1-api-fix)

### Fixed
- Fix a breaking API change in ReSharper 2018.1.2 (#584)



## 2018.1 - 2018-04-18
* For Rider and ReSharper 2018.1
* [Commits](https://github.com/JetBrains/resharper-unity/compare/wave11-rider-2017.3.2...wave12-eap9-rtm)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/14?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/wave12-eap9-rtm)

### Added
- Add inspections for null coalescing and null propagation operators (#342, #35, #148)
- Add go to definition, find usages, highlighting, code completion for ShaderLab variable references (#362)
- Add undocumented `UIBehaviour` APIs (#394, #395, RIDER-12649)
- Add code inspection wiki for most inspections
- Add workaround for Unity's old version of annotations and make `[PublicAPI]` mark all members as in use (#337)
- Rider: Add Assets View as alternative to Solution Explorer
- Rider: Add Unity tool window to view Unity editor console logs, with parsed stack trace
- Rider: Play/pause/step Unity from within Rider
- Rider: Background refresh Unity projects
- Rider: Hides unnecessary tool windows when opening a Unity project
- Rider: Run Unity editor tests via Rider

### Changed
- Bumped version to 2018.1 to match Rider and ReSharper releases
- Update API details to 2018.1 (#365, #395)
- Change inspection for incorrectly applied attributes from error to redundant code (#325, #322, #376)
- Remove option to disable ShaderLab parsing from UI (#236)
- Unity editor: Added public method to allow other Unity code to open files in Rider

### Fixed
- Fix ShaderLab colour reference handling with non-US cultures (#346)
- Fix ShaderLab vector properties showing colour highlighting or throwing exceptions (#384, #397)
- Fix parse error with trailing whitespace in ShaderLab variable references (#257, #357)
- Fix exceptions with existing features
- Rider: Fix file templates work for files outside of project structure, such as in Unity Explorer (#358)
- Rider: Fix unit tests not being cancelled
- Unity editor: Fix editor plugin for 4.7-5.5



## 2017.3.1 - 2018-02-06
* For Rider 2017.3.1. Not released for ReSharper
* Note that there are no changes to the plugin bundled with Rider 2017.3.2
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v2.1.3-rider...wave11-rider-2017.3.1)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/15?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/wave11-rider-2017.3.1)

### Added
- Rider: Discover Unity's own installs of Mono for use in Toolset options page

### Changed
- Rider: Improve `UnityEngine.dll` detection on Linux (#313)

### Fixed
- Rider: Fix error when attaching to Unity editor and multiple Unity processes found (#308, #311)
- Rider: Fix Unity class library project template on non-Windows (#315, #318)
- Unity editor: Fix setting `TargetFrameworkVersion` for netstandard
- Unity editor: Fix missing reference to iOS assemblies (#227)
- Unity editor: Fix plugin causing project to fail compilation on Cloud Build (#314)
- Unity editor: Fix exception with null external editor path
- Unity editor: Use `Path.GetFullPath` to work better with Unity packages
- Unity editor: Fix to only post process the Unity generated solution file



## 2017.3 for Rider and 2.1.3 for ReSharper - 2017-12-22
* Bundled with Rider 2017.3. Released as 2.1.3 for ReSharper.
* [Commits](https://github.com/JetBrains/resharper-unity/compare/a14a3d50fa72b6c37a05184af56c6fefcb772f98...v2.1.3-resharper) (SHA is equivalent to `2.1.2.1739` on `master`)
* [Commits (rider changes)](https://github.com/JetBrains/resharper-unity/compare/v2.1.3-resharper...v2.1.3-rider)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/13?closed=1)
* [GitHub release (resharper)](https://github.com/JetBrains/resharper-unity/releases/tag/v2.1.3-resharper)
* [GitHub release (rider)](https://github.com/JetBrains/resharper-unity/releases/tag/v2.1.3-rider)

### Added
- Add Context Action to convert auto-property to property with serialized backing field (#195, #302)
- Add Context Action to mark field as serialized or non-serizable (#191, #295)
- Add inspection and Quick Fix for redundant `SerializeField` attribute (#295)
- Add inspections and Quick Fixes for method signature of methods with Unity attributes (#248)
- Add inspections for incorrectly applied attributes (#247)
- Rider: Add project template for Unity class library (#318)
- Rider: Open local or web documentation for Unity symbols (#98, #304)
- Rider: Support running simple non-Unity editor based nunit tests (#256)
- Rider: Add project template for Unity based class library (#303)
- Unity editor: Add custom references from `mcs.rsp` (#270, #272)
- Unity Editor: Integrate pdb2mdb to generate debugging info for class libraries (#290)

### Changed
- Improve relevance of Unity event functions in code completion (#260, #273)
- Rider: Improve updating Solution Explorer when updating .meta files (#296)
- Unity editor: Update path to new Toolbox path automatically
- Unity editor: Plugin regenerates project files on initialisation
- Unity Editor: Generated project includes Visual Studio for Tools flavour GUID
- Unity Editor: Split `TargetFrameworkVersion` overrides per scripting runtime

### Fixed
- Fix code completion before a field with an attribute (#259, #286)



## 2.1.2.1739 - 2017-11-15
* For Rider 2017.2.1
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v2.1.2...2.1.2.1739)
* [Tag](https://github.com/JetBrains/resharper-unity/releases/tag/2.1.2.1739)

### Added
- Rider: Add syntax highlighting for `.compute` files ([RIDER-11221](https://youtrack.jetbrains.com/issue/RIDER-11221))

### Changed
- Improve reliability of attaching debugger to Unity Editor (#262, #268)


## 2.1.2 for ReSharper 2017.2 - 2017-10-09
* For ReSharper 2017.2
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v2.1.1...v2.1.2-resharper)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/12?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v2.1.2-resharper)

### Added
- Support Unity API up to 2017.3.0b3 (#218)
- Add Unity specific file templates (#232, #237)
- Recognise projects with modularised UnityEngine assembly references (#241)
- Add colour highlighting and editing to ShaderLab
- Add icons for ShaderLab files and run configurations
- Show event function descriptions in generate dialog (#225, [RIDER-4904](https://youtrack.jetbrains.com/issue/RIDER-4904))

### Changed
- Updated to ReSharper 2017.2
- Improve parsing of Cg files (#243)
- Improve ShaderLab parsing (#228, #233, [RIDER-9214](https://youtrack.jetbrains.com/issue/RIDER-9214), #222)

### Fixed
- Fix code completion and generation not working with newer versions of Unity (#219, #245)
- Fix parsing of 2DArray in ShaderLab files ([RIDER-9786](https://youtrack.jetbrains.com/issue/RIDER-9786))



## 2.1.2 for Rider 2017.2 - 2017-10-09
* For Rider 2017.2
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v2.1.1...v2.1.2)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/12?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v2.1.2-resharper)
* [Tag](https://github.com/JetBrains/resharper-unity/releases/tag/v2.1.2)

### Added
- Support Unity API up to 2017.3.0b3 (#218)
- Add Unity specific file templates (#232, #237)
- Recognise projects with modularised UnityEngine assembly references (#241)
- Add colour highlighting and editing to ShaderLab
- Add icons for ShaderLab files and run configurations

### Changed
- Improve parsing of Cg files (#243)

### Fixed
- Fix code completion and generation not working with newer versions of Unity (#219, #245)
- Fix parsing of 2DArray in ShaderLab files ([RIDER-9786](https://youtrack.jetbrains.com/issue/RIDER-9786))



## 2.1.1 - 2017-09-16
* For Rider 2017.2 EAP2. Not released for ReSharper
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v2.1.0...v2.1.1)
* [Tag](https://github.com/JetBrains/resharper-unity/releases/tag/v2.1.1)

### Added
- Show event function descriptions in generate dialog (#225, [RIDER-4904](https://youtrack.jetbrains.com/issue/RIDER-4904))
- Unity editor: Add support for `mcs.rsp` (#230)

### Changed
- Improve ShaderLab parsing (#228, #233, [RIDER-9214](https://youtrack.jetbrains.com/issue/RIDER-9214), #222)



## 2.1.0 - 2017-09-04 (approximately)
* For Rider 2017.2 EAP1. Not released for ReSharper
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v2.0.4...v2.1.0) (Due to branching strategy, this list contains commits from previous releases)
* [Tag](https://github.com/JetBrains/resharper-unity/releases/tag/v2.1.0)

### Added
- Add annotations for modularised UnityEngine assemblies (#207)



## 2.0.4 - 2017-09-04
2.0.4.2575 for Rider 2017.1.2 - RD-171.4456.3568. Not released for ReSharper
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v2.0.3...v2.0.4)
* [Tag](https://github.com/JetBrains/resharper-unity/releases/tag/v2.0.4)

### Changed
* Fix parsing errors in ShaderLab files ([RIDER-8917](https://youtrack.jetbrains.com/issue/RIDER-8917), [RIDER-8914](https://youtrack.jetbrains.com/issue/RIDER-8914))
* ShaderLab syntax fixes ([RIDER-8917](https://youtrack.jetbrains.com/issue/RIDER-8917), [RIDER-8914](https://youtrack.jetbrains.com/issue/RIDER-8914))
* Rider: Change completion in shader files to be semi-focussed



## 2.0.3-resharper - 2017-08-31
* For ReSharper 2017.2
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v2.0.0-resharper...v2.0.3-resharper)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v2.0.3-resharper)

### Changed
- Updated to ReSharper 2017.2 (#193)



## 2.0.3 - 2017-08-31
* For Rider 2017.1.1 (RD-171-4456.2813 + 2.0.3.2540)
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v2.0.2...a89dce7c8ba66cd8d6d86bb3dd1c7a82544fe21f) (SHA is equivalent to `v2.0.3`. Possibly tagged wrong branch?)
* [Tag](https://github.com/JetBrains/resharper-unity/releases/tag/v2.0.3)

### Fixed
- Parse pre-processor directives in ShaderLab (#186)
- Correctly handle property attributes in shader file (#187)
- Parse CGINCLUDE blocks at any point in shader file (#188, #189, #206)
- Parse property reference for BlendOp ([RIDER-8386](https://youtrack.jetbrains.com/issue/RIDER-8386))



## 2.0.0 for ReSharper - 2017-08-29
* For ReSharper 2017.1
* Based on work in progress 2.0.3 for Rider 2017.1
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v2.0.0...v2.0.2-resharper)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v2.0.0-resharper)

### Added
- Support for ShaderLab files. Syntax highlighting, error highlighting, commenting, bracket matching, folding
- Support for simple syntax highlighting and word completion of CG blocks
- Add ability to disable advanced ShaderLab syntax (#183)
- Add support for `HLSL` and `GLSL` blocks in ShaderLab

### Fixed
- Parse pre-processor directives in ShaderLab (#186)
- Correctly handle property attributes in shader file (#187)
- Parse CGINCLUDE blocks at any point in shader file (#188, #189, #206)
- Parse property reference for BlendOp ([RIDER-8386](https://youtrack.jetbrains.com/issue/RIDER-8386))



## 2.0.2 - 2017-08-03
* For Rider 2017.1 RTM. Not released for ReSharper
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v2.0.0...v2.0.2)
* [Tag](https://github.com/JetBrains/resharper-unity/releases/tag/v2.0.2)

### Added
- Add ability to disable advanced ShaderLab syntax (#183)
- Add support for `HLSL` and `GLSL` blocks in ShaderLab
- Rider: Make sure _Attach to Unity_ run configuration is selected



## 2.0.0 - 2017-07-14
* For Rider 2017.1 RC. Not released for ReSharper
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.9.1...v2.0.0)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/5?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v2.0.0)

### Added
- Support for ShaderLab files. Syntax highlighting, error highlighting, commenting, bracket matching, folding
- Support for simple syntax highlighting and word completion of CG blocks

### Changed
- Rider: Updated to Rider 2017.1 RC

### Fixed
- Rider: Wait for project to initialise before checking if it's a Unity project
- Unity editor: Use 4.x compatible API when checking file extensions (#170, #171)



## 1.9.2 - 2017-08-15
* For Rider 2017.1 EAP23. Not released for ReSharper
* Not tagged. Don't know commit, so might not even have been released. This is based on Milestone. Might actually be merged in to 2.0.0
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/10?closed=1)

### Changed
- Rider: Install the Unity3DRider plugin even if Unity references are unresolved (#160, #174)
- Rider: Find Unity3DRider plugin even if install location is moved (#169, #172)



## 1.9.1 - 2017-06-29
* For ReSharper 2017.1 and Rider 2017.1 EAP23
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.9.0...v1.9.1)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/9?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.9.0)
* [ReSharper release (2017-07-21)](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/1.9.1)

### Added
- Rider: Display notification if auto-save is enabled with a quick link to disable. This can affect running games in the Unity editor

### Changed
- Improve performance by logging and change tracking for non Unity projects
- Rider: Update to EAP23
- Rider: Add setting to disable install of Unity3dRider plugin (#159)
- Unity editor: Minor improvements to logging, such as configurable log level



## 1.9.0 - 2017-06-15
* For ReSharper 2017.1 and Rider 2017.1 EAP22
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.8.0...v1.9.0)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/11?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.9.0)
* [ReSharper release (2017-07-21)](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/1.9.0)

### Added
- Rider: Plugin is now a bundled plugin, out of the box in Rider
- Rider: Merge Unity3DRider plugin
- Rider: Install plugin automatically when opening a Unity project in Rider

### Changed
- Only set C# language level if Unity project has an Assets folder (#150)

### Fixed
- Navigate to correct local documentation page, or Unity's search page (#152)
- Rider: Fix to prevent code cleanup crashing



## 1.8.0 - 2017-05-18
* For ReSharper 2017.1 and Rider 2017.1 EAP22
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.7.0...v1.8.0)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/8?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.8.0)
* [ReSharper release (2017-05-19)](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/1.8.0)

### Added
- Inspection and Quick Fix for calling `new` on `ScriptableObject` and `MonoBehaviour` (#142)
- Inspections and Quick Fixes for incorrect method signature for `InitializeOnLoad` attributes (#143)
- Inspection and Quick Fix to mark empty event functions as dead code (#137)
- Added "base type required" annotations for various attributes (#145)
- Added implicit use annotations for `UnityEngine.Networking` attributes (#136)
- Code completion, find usages and rename support for `SyncVarAttribute` hook function (#135)
- Support `hook` property of `SyncVarAttribute` (#136)
- Rider: Automatically exclude `Library` and `Temp` folders from Rider's full text search (#117)
- Rider: Add _Attach to Unity Editor_ debug configuration (#141)

### Changed
- Rider: Updated to Rider 2017.1 EAP22

### Fixed
- Fixed bug in `Invoke` symbol resolution to check base class (#138)



## 1.7.0 - 2017-04-05
* For ReSharper 2017.1 (and early EAP version of Rider)
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.6.2...v1.7.0)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/7?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.7.0)

### Added
- Treats `Assertion.Assert` as assertion methods (#129)

### Changed
- Updated to ReSharper 2017.1 (#110)

### Fixed
- Fix incorrect signatures in known API (#128)



## 1.6.2 - 2017-03-22
* For ReSharper 2016.3 and Rider EAP19
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.6.1...v1.6.2)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.6.2)

### Changed
- Updated to Rider EAP19
- Improve location of "Create serialized field" Quick Fix (#124)



## 1.6.1 - 2017-03-08
* For ReSharper 2016.3 and Rider EAP18
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.6.0...v1.6.1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.6.1)

### Fixed
- Fix nasty bug that will delete and recreate all `.meta` files when reloading projects. Sorry! (#118)



## 1.6.0 - 2017-03-01
* For ReSharper 2016.3 and Rider EAP18
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.5.1-rider...v1.6.0)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/6?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.6.0)

### Added
- Correctly update .meta files when creating, renaming or refactoring (#56, #61)
- Quick Fix to "Create serialized field" from usage (#111)
- Inspections and Quick Fixes for incorrect event function signature and return type
- Inspection to warn if coroutine return value is unused (#99)
- Context Action to convert event function signature to/from coroutine
- Event functions that are coroutines are now recognised and marked as in use (#52)
- Coroutine and optional parameter information to API and tooltips
- Regex annotations for `EditorTestsWithLogParser.ExpectLogLineRegex` (#95)

### Changed
- Expand API support to 5.0 - 5.6



## 1.5.1-rider - 2017-02-17
* For Rider EAP17
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.5.0-rider...v1.5.1-rider)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.5.1-rider)

### Changed
- Updated to Rider EAP17



## 1.5.0-rider - 2017-01-02
* Initial release for Rider
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.5.0...v1.5.0-rider)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.5.0-rider)

### Added
- Added support for Rider EAP15



## 1.5.0 - 2016-12-30
* For ReSharper 2016.3
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.4.0...v1.5.0)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/4?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.5.0)

### Added
- Updated to ReSharper 2016.3 (#80, #90)
- Add inspection and Quick Fix to use `CompareTag` instead of string comparison (#82)
- Add gutter icon and "Create" context action for Unity classes (#77)
- Support method name in string literal of `MonoBehaviour.IsInvoking` (#85)
- Support method name in string literal for `MonoBehaviour.Start`/`StopCoroutine` (#83)
- Support undocumented `ScriptableObject.OnValidate` and `Reset` (#79)
- Initial Rider support

### Changed
- Support Unity API for 5.2 - 5.5 (#81)
- Improve recognition of serialised fields (#87)

### Fixed
- Fix method generation for static event functions (#73)
- Remove duplicate items in auto complete list (#92)



## 1.4.0 - 2016-11-18
* For ReSharper 2016.2
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.3.0...v1.4.0)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/3?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.4.0)

### Added
- String formatting inspections for Unity logging methods
- Enable functionality in projects that reference Unity assemblies, not just those that have the VSTU project flavour GUID (#53)
- Treat `UnityEngine.Debug.Assert` as assertion methods, so ReSharper includes asserts in control flow analysis. (#62, #63 - thanks @joshuaoconnor!)
- Display a "gutter" icon for implicitly used event functions and fields (#58)
- Display colour highlights and the colour palette picker for `UnityEngine.Color` and `UnityEngine.Color32` (#51)
- Support undocumented messages, such as `OnGeneratedCSProjectFiles` (#59)

### Changed
- Improve handling of C# language version. Default is correctly set to C# 4, not 5. Uses C# 6 if option is enabled in Unity 5.5. Handles the CSharp60Support plugin (#50, #60)
- Renamed "message handlers" to "event functions", as per the Unity documentation
- Sort event functions alphabetically by default in code completion

### Fixed
- Fix `MonoBehaviour.Invoke` code completion and rename support in string literals to work with the correct class, not just the current class (#66)
- Fix namespace provider settings for `Assets` and `Assets\Scripts` folders (#64)



## 1.3.0 - 2016-09-26
* For ReSharper 2016.2
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.2.1...v1.3.0)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/2?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.3.0)

### Added
- Expanded generate code to all Unity class messages, not just `MonoBehaviour` (#20, #29, #44)
- External annotations to improve ReSharper's analysis. E.g. implicit usage and nullability of `Component.gameObject` (#34, #13, #15, #23, #42, #43)
- Code completion, find usages and rename support for `Invoke`, `InvokeRepeating` and `CancelInvoke` (#41)
- Auto-suggest message handler completion when creating methods
- Message handler descriptions for methods and parameters displayed in tooltips and QuickDoc
- "Read more" in QuickDoc navigates to Unity API documentation

### Changed
- Updated to ReSharper 2016.2 (#44, #46)



## 1.2.1 - 2016-04-16
* For ReSharper 2016.1
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.2.0...v1.2.1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.2.1)

### Changed
- Updated to ReSharper 2016.1



## 1.2.0 - 2015-11-16
* For ReSharper 10
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.1.2...v1.2.0)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.2.0)

### Added
- Suppress naming consistency warnings on message handlers
- Add parameters to generated message handlers (#8)
- Automatically set language level to C# 5 (#5)
- ReSharper no longer suggests `Assets` or `Scripts` when checking namespaces



## 1.1.2 - 2015-11-06
* For ReSharper 10
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.0.0...v1.1.2)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.1.2)

### Changed
- Updated to ReSharper 10



## 1.0.0 - 2015-10-16
* For ReSharper 9.2. Initial release
* [Commits](https://github.com/JetBrains/resharper-unity/compare/81b6bc5...v1.0.0)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.0.0)

### Added
- Marks `MonoBehaviour` classes, fields and methods as in use
- Adds _Generate Code_ provider for `MonoBehaviour` message handlers

