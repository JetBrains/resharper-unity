# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0). Note that this project does not follow semantic versioning but uses version numbers based on JetBrains [Rider](https://www.jetbrains.com/rider/) and [ReSharper](https://www.jetbrains.com/resharper/) releases.

This plugin has functionality that is common to both ReSharper and Rider. It also contains a plugin for the Unity editor that is used to communicate with Rider. Changes marked with a "Rider:" prefix are specific to Rider, while changes for the Unity editor plugin are marked with a "Unity editor:" prefix. No prefix means that the change is common to both Rider and ReSharper.

Since 2018.1, the version numbers and release cycle match Rider's versions and release dates. The plugin is always bundled with Rider, but is released for ReSharper separately. Sometimes the ReSharper version isn't released. This is usually because the changes are not applicable to ReSharper, but also by mistake.

## 2021.2.0
* [Commits](https://github.com/JetBrains/resharper-unity/compare/net211...net212)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/45?closed=1)

### Added

- Rider: Warn if trying to commit with unsaved scenes ([#1077](https://github.com/JetBrains/resharper-unity/issues/1077), [RIDER-60824](https://youtrack.jetbrains.com/issue/RIDER-60824), [#2071](https://github.com/JetBrains/resharper-unity/pull/2071))
- Rider: Support debugging local UWP players ([RIDER-62707](https://youtrack.jetbrains.com/issue/RIDER-62707), [#2098](https://github.com/JetBrains/resharper-unity/pull/2098))

### Changed

- Methods marked with `[UnitySetUp]` are treated as in use ([#2048](https://github.com/JetBrains/resharper-unity/issues/2048), [#2047](https://github.com/JetBrains/resharper-unity/pull/2047))
- Improve localised caching for performance across all Unity analyses ([RIDER-62709](https://youtrack.jetbrains.com/issue/RIDER-62709), [#2106](https://github.com/JetBrains/resharper-unity/pull/2106))
- Parse layer names for legacy projects ([#2094](https://github.com/JetBrains/resharper-unity/issues/2094), [#2107](https://github.com/JetBrains/resharper-unity/pull/2107))
- Rider: Improve performance showing packages in Unity Explorer when reopening a project ([RIDER-61805](https://youtrack.jetbrains.com/issue/RIDER-61805), [#2105](https://github.com/JetBrains/resharper-unity/pull/2105))
- Rider: Group Unity run configurations in the run config dialog ([#2081](https://github.com/JetBrains/resharper-unity/pull/2081))
- Rider: Ignore "break on unhandled exception" setting for IL2CPP players ([RIDER-62321](https://youtrack.jetbrains.com/issue/RIDER-62321), [#2098](https://github.com/JetBrains/resharper-unity/pull/2098))
- Rider: Show localised external Unity documentation if English isn't available ([RIDER-55737](https://youtrack.jetbrains.com/issue/RIDER-55737), [#2050](https://github.com/JetBrains/resharper-unity/pull/2050))

### Fixed

- Show editor file templates in package folders ([#2090](https://github.com/JetBrains/resharper-unity/pull/2090))
- Rider: Hide run marker gutter icons for static methods in Unity projects ([RIDER-55734](https://youtrack.jetbrains.com/issue/RIDER-55734), [#2063](https://github.com/JetBrains/resharper-unity/pull/2063))
- Rider: Error in Unity tool window log is not cleared ([RIDER-59689](https://youtrack.jetbrains.com/issue/RIDER-59689), [#2051](https://github.com/JetBrains/resharper-unity/pull/2051))
- Rider: External Documentation action now works with Unity API ([#2050](https://github.com/JetBrains/resharper-unity/pull/2050))
- Rider: Fix wrong page opening in external documentation ([RIDER-57745](https://youtrack.jetbrains.com/issue/RIDER-57745), [#2050](https://github.com/JetBrains/resharper-unity/pull/2050))
- Rider: External documentation for method parameter now navigates to documentation for method ([https://youtrack.jetbrains.com/issue/RIDER-60297](https://youtrack.jetbrains.com/issue/RIDER-60297), [#2050](https://github.com/JetBrains/resharper-unity/pull/2050))
- Rider: Fix packages occasionally not showing or updating in Unity Explorer when Unity is not running ([RIDER-62558](https://youtrack.jetbrains.com/issue/RIDER-62558), [#2085](https://github.com/JetBrains/resharper-unity/pull/2085))



## 2021.1.3
* [Commits](https://github.com/JetBrains/resharper-unity/compare/net211-rtm-2021.1.2...net211)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/47?closed=1)

### Fixed

- Rider: Remove Unity built in package folders from Perforce content roots ([RIDER-61551](https://youtrack.jetbrains.com/issue/RIDER-61551))
- Rider: Only add Unity folders to index for Unity packages ([#2076](https://github.com/JetBrains/resharper-unity/pull/2076))
- Rider: Clean up indexed folders added in previous versions ([#2076](https://github.com/JetBrains/resharper-unity/pull/2076))
- Rider: Fix unresolved git packages when the `packages-lock.json` is disabled ([#2084](https://github.com/JetBrains/resharper-unity/pull/2084))
- Rider: Fix unresolved or incorrectly resolved package used as a dependency when `packages-lock.json` is disabled ([#2084](https://github.com/JetBrains/resharper-unity/pull/2084))



## 2021.1.2
* Released: [2021-04-23](https://blog.jetbrains.com/dotnet/2021/04/23/resharper_rider_2021_1_2/)
* Build: 2021.1.2.127
* [Commits](https://github.com/JetBrains/resharper-unity/compare/net211-rtm-2021.1.0-rtm-2021.1.1...net211-rtm-2021.1.2)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/46?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/net211-rtm-2021.1.2)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/2021.1.2.127)

### Changed

- Rider: Add link to troubleshooting document to the "Attach to Unity Process" debug dialog ([#2072](https://github.com/JetBrains/resharper-unity/pull/2067))

### Fixed

- Fix missing implicit include of UnityGC.cginc in non-surface shaders ([RIDER-60508](https://youtrack.jetbrains.com/issue/RIDER-60508), [#2069](https://github.com/JetBrains/resharper-unity/pull/2069))
- Fix exception breaking highlighting in Unity.Mathematics generated files ([RIDER-61025](https://youtrack.jetbrains.com/issue/RIDER-61025), [#2067](https://github.com/JetBrains/resharper-unity/pull/2067))
- Fix exception that prevented macros being saved with the AsmDef file template ([RIDER-61376](https://youtrack.jetbrains.com/issue/RIDER-61376), [#2073](https://github.com/JetBrains/resharper-unity/pull/2073))
- Rider: Fix USB iOS debugging ([#2072](https://github.com/JetBrains/resharper-unity/pull/2072))
- Rider: Fix grouping issue in "Attach to Unity Process" dialog ([#2059](https://github.com/JetBrains/resharper-unity/issues/2059), [#2072](https://github.com/JetBrains/resharper-unity/pull/2072))
- Rider: Fix minor rendering issue in "Attach to Unity Process" dialog ([#2072](https://github.com/JetBrains/resharper-unity/pull/2072))
- Rider: Gracefully handle Unity API exceptions in debugger extensions - `UnityException`, `MissingComponentException`, `MissingReferenceException`, `UnassignedReferenceException` ([RIDER-60794](https://youtrack.jetbrains.com/issue/RIDER-60794), [RIDER-60795](https://youtrack.jetbrains.com/issue/RIDER-60795), [DEXP-585430](https://youtrack.jetbrains.com/issue/DEXP-585430), [DEXP-534612](https://youtrack.jetbrains.com/issue/DEXP-534612), [DEXP-558487](https://youtrack.jetbrains.com/issue/DEXP-558487), [#2057](https://github.com/JetBrains/resharper-unity/pull/2057))
- Rider: Fix incorrect reporting of `EvaluatorAbortedException` control flow exception ([DEXP-570625](https://youtrack.jetbrains.com/issue/DEXP-570265), [#2057](https://github.com/JetBrains/resharper-unity/pull/2057))
- Rider: Fix exceptions thrown when by Unity debugger extensions when implicit evaluation is disabled ([RIDER-60798](https://youtrack.jetbrains.com/issue/RIDER-60798), [#2057](https://github.com/JetBrains/resharper-unity/pull/2057))
- Rider: Fix some null reference exceptions when evaluating Unity debugger extensions ([DEXP-561412](https://youtrack.jetbrains.com/issue/DEXP-561412), [DEXP-577753](https://youtrack.jetbrains.com/issue/DEXP-577753), [#2507](https://github.com/JetBrains/resharper-unity/pull/2057))



## 2021.1.1
* Released: [2021-04-10](https://blog.jetbrains.com/dotnet/2021/04/10/rider-resharper-2021-1-1-released/)
* Build: 2021.1.0.114
* [No code changes](https://github.com/JetBrains/resharper-unity/compare/net211-rtm-2021.1.0...net211-rtm-2021.1.0-rtm-2021.1.1)



## 2021.1.0
* Released: [2021-04-08](https://blog.jetbrains.com/dotnet/2021/04/08/rider-2021-1-release/)
* Build: 2021.1.0.113
* [Commits](https://github.com/JetBrains/resharper-unity/compare/net203...net211-rtm-2021.1.0)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/42?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/net211-rtm-2021.1.0)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/2021.1.0.114)

### Added

- Add Find Usages support for methods and properties used in animations ([#488](https://github.com/JetBrains/resharper-unity/issues/488), [RIDER-43464](https://youtrack.jetbrains.com/issue/RIDER-43464), [RIDER-43338](https://youtrack.jetbrains.com/issue/RIDER-43338), [#1982](https://github.com/JetBrains/resharper-unity/pull/1982))
- Add completion and validation of animation state names in `Animator.Play` methods ([RIDER-17449](https://youtrack.jetbrains.com/issue/RIDER-17449), [#1982](https://github.com/JetBrains/resharper-unity/pull/1982))
- Add gutter icons to show Burst compiled methods ([#1995](https://github.com/JetBrains/resharper-unity/pull/1995))
- Add context actions to show call tree for performance critical, expensive and Burst compiled methods ([RIDER-35169](https://youtrack.jetbrains.com/issue/RIDER-35169), [#1995](https://github.com/JetBrains/resharper-unity/pull/1995))
- Rider: Add Code Vision actions to show call tree for performance critical, expensive and Burst compiled methods ([RIDER-35169](https://youtrack.jetbrains.com/issue/RIDER-35169), [#1995](https://github.com/JetBrains/resharper-unity/pull/1995))
- Rider: Add custom debugger type views for `SerializedObject` and `SerializedProperty` ([#1991](https://github.com/JetBrains/resharper-unity/pull/1991))
- Rider: Show Unity editor plugin errors and exceptions in Unity console, not just log ([RIDER-54352](https://youtrack.jetbrains.com/issue/RIDER-54352), [#2012](https://github.com/JetBrains/resharper-unity/pull/2012))
- Rider: Add marker to ignored folders in Unity Explorer ([#2024](https://github.com/JetBrains/resharper-unity/pull/2024))

### Changed

- Mark `UxmlFactory` derived classes as implicitly used ([RIDER-54860](https://youtrack.jetbrains.com/issue/RIDER-54860), [#2002](https://github.com/JetBrains/resharper-unity/pull/2002))
- Remove warnings for `try` and `foreach` in Burst analysis. These are supported since Burst 1.4 ([#2015](https://github.com/JetBrains/resharper-unity/issues/2015), [#1378](https://github.com/JetBrains/resharper-unity/issues/1378), [#2017](https://github.com/JetBrains/resharper-unity/pull/2017))
- Rider: Support code coverage for Unity play mode tests ([#2016](https://github.com/JetBrains/resharper-unity/pull/2016))
- Rider: Unity file templates are grouped into "Unity Class" and "Unity Shader" ([#1983](https://github.com/JetBrains/resharper-unity/pull/1983))
- Rider: Quick doc link for online pages now uses the version specific search page ([#2005](https://github.com/JetBrains/resharper-unity/issues/2005), [#2006](https://github.com/JetBrains/resharper-unity/pull/2006))
- Rider: Double click on assembly files in Unity Explorer will add the file to Assembly Explorer ([RIDER-54873](https://youtrack.jetbrains.com/issue/RIDER-54873), [#1996](https://github.com/JetBrains/resharper-unity/pull/1996))
- Rider: Improve wording on notifications when Rider is not set as Unity's External Editor ([#1958](https://github.com/JetBrains/resharper-unity/pull/1958))
- Rider: Improve performance adding log events to Unity tool window ([RIDER-57100](https://youtrack.jetbrains.com/issue/RIDER-57100), [#2014](https://github.com/JetBrains/resharper-unity/pull/2014))
- Rider: Improve discovery and display of packages in Unity Explorer ([#2024](https://github.com/JetBrains/resharper-unity/pull/2024))
- Rider: Stop showing VCS status colours in Unity Explorer References node ([#2024](https://github.com/JetBrains/resharper-unity/pull/2024))

### Fixed

- Fix broken code generation when replacing the entire name of an existing event function ([#2040](https://github.com/JetBrains/resharper-unity/pull/2040))
- Fix missing entries from ReSharper's Unity specific dictionary ([#1737](https://github.com/JetBrains/resharper-unity/issues/1737), [#1809](https://github.com/JetBrains/resharper-unity/issues/1809), [#2008](https://github.com/JetBrains/resharper-unity/pull/2008))
- Fix creation of `.meta` files in the root of the `Packages` folder, and in `file:` based packages ([#2000](https://github.com/JetBrains/resharper-unity/pull/2000))
- Fix suggested name collision when creating a field to cache a property index ([RIDER-55185](https://youtrack.jetbrains.com/issue/RIDER-55185), [#2002](https://github.com/JetBrains/resharper-unity/pull/2002))
- Fix stored asset file names to be relative to solution directory ([RIDER-54695](https://youtrack.jetbrains.com/issue/RIDER-54695), [#2002](https://github.com/JetBrains/resharper-unity/pull/2002))
- Fix meta files not being updated correctly if solution name doesn't match folder name exactly ([#2027](https://github.com/JetBrains/resharper-unity/issues/2027), [#2030](https://github.com/JetBrains/resharper-unity/pull/2030))
- Fix meta files being created for hidden assets ([#2035](https://github.com/JetBrains/resharper-unity/pull/2035))
- Rider: Fix location of context menu shown when clicking Unity Code Vision links ([RIDER-49573](https://youtrack.jetbrains.com/issue/RIDER-49573), [#2002](https://github.com/JetBrains/resharper-unity/pull/2002))
- Rider: Fix missing error indicator for `.asmdef` files ([#2002](https://github.com/JetBrains/resharper-unity/pull/2002))
- Rider: Fix issue launching Unity when debugging and editor isn't running ([RIDER-52498](https://youtrack.jetbrains.com/issue/RIDER-52498), [#1920](https://github.com/JetBrains/resharper-unity/pull/1920))
- Rider: Fix exception in protocol file watcher ([DEXP-559833](https://youtrack.jetbrains.com/issue/DEXP-559833), [#1942](https://github.com/JetBrains/resharper-unity/pull/1942))
- Rider: Fix exception with multiple assemblies with the same name ([RIDER-54876](https://youtrack.jetbrains.com/issue/RIDER-54876), [#1953](https://github.com/JetBrains/resharper-unity/pull/1953))
- Rider: Fix exception with feedback provider on welcome screen ([#2013](https://github.com/JetBrains/resharper-unity/pull/2013))
- Rider: Fix mouse focus issue when opening Rider full screen on macOS ([RIDER-55501](https://youtrack.jetbrains.com/issue/RIDER-55501), [#1980](https://github.com/JetBrains/resharper-unity/pull/1980))
- Rider: Fix files in `Packages` folder showing the non-project dialog when being modified ([#1997](https://github.com/JetBrains/resharper-unity/pull/1997))
- Rider: Fix package resolution when dependency uses higher version than referenced ([#1786](https://github.com/JetBrains/resharper-unity/issues/1786), [#2024](https://github.com/JetBrains/resharper-unity/pull/2024))
- Rider: Fix incorrectly creating Attach to Editor and Play run configuration for library projects ([#2019](https://github.com/JetBrains/resharper-unity/pull/2019))
- Rider: Fix not returning to edit mode when stopping Attach to Editor and Play debug session ([RIDER-50085](https://youtrack.jetbrains.com/issue/RIDER-50085), [#2023](https://github.com/JetBrains/resharper-unity/pull/2023))



## 2020.3.3
* Released: [2021-02-22](https://blog.jetbrains.com/dotnet/2021/02/22/rider-resharper-2020-3-3/)
* Build: 2020.3.3.149
* [Commits](https://github.com/JetBrains/resharper-unity/compare/net203-rtm-2020.3.1-rtm-2020.3.2...net203-rtm-2020.3.3)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/44?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/net203-rtm-2020.3.3)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/2020.3.3.149)

### Fixed

- Fix exception and missing namespace suppression when running plugin in `inspectcode` ([#2003](https://github.com/JetBrains/resharper-unity/pull/2003))
- Rider: Allow disabling Burst context Code Vision ([#1985](https://github.com/JetBrains/resharper-unity/pull/1985))



## 2020.3.2
* Released: [2020-12-30](https://blog.jetbrains.com/dotnet/2020/12/30/rider-resharper-2020-3-2-release/)
* Build: 2020.3.1.136
* [No code changes](https://github.com/JetBrains/resharper-unity/compare/net203-rtm-2020.3.1...net203-rtm-2020.3.1-rtm-2020.3.2)



## 2020.3.1
* Released: [2020-12-24](https://blog.jetbrains.com/dotnet/2020/12/24/resharper-rider-2020-3-1/)
* Build: 2020.3.1.132
* [Commits](https://github.com/JetBrains/resharper-unity/compare/net203-rtm-2020.3.0...net203-rtm-2020.3.1)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/43?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/net203-rtm-2020.3.1)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/2020.3.1.132)

### Changed

- Disable performance and Burst context with comment ([#1948](https://github.com/JetBrains/resharper-unity/pull/1948))

### Fixed

- Fix performance context and Burst actions not showing if caret was on first character of method declaration ([#1948](https://github.com/JetBrains/resharper-unity/pull/1948))
- Rider: Fix showing incorrect Code Vision link for expensive code ([#1948](https://github.com/JetBrains/resharper-unity/pull/1948))
- Rider: Fix automatically refreshing assets before running unit tests ([RIDER-54359](https://youtrack.jetbrains.com/issue/RIDER-54359), [#1960](https://github.com/JetBrains/resharper-unity/pull/1960))
- Rider: Fix missing trace scenario in Diagnostic Tools dialog ([#1962](https://github.com/JetBrains/resharper-unity/pull/1962))
- Unity editor: Fix log message on incorrect access of deprecated settings ([RIDER-55362](https://youtrack.jetbrains.com/issue/RIDER-55362), [#1967](https://github.com/JetBrains/resharper-unity/pull/1967))



## 2020.3.0
* Released: [2020-12-14](https://blog.jetbrains.com/dotnet/2020/12/14/rider-2020-3-release/)
* Build: 2020.3.0.5061
* [Commits](https://github.com/JetBrains/resharper-unity/compare/net202...net203-rtm-2020.3.0)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/41?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/net203-rtm-2020.3.0)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/2020.3.0.125)

### Added

- Add smart backspace for shader files ([#1863](https://github.com/JetBrains/resharper-unity/pull/1863))
- Add context actions to mark arbitrary methods as the start of a performance critical context ([#1852](https://github.com/JetBrains/resharper-unity/pull/1852))
- Add context actions to suppress performance critical context for a method ([#1183](https://github.com/JetBrains/resharper-unity/issues/1183), [RIDER-27955](https://youtrack.jetbrains.com/issue/RIDER-27955), [RIDER-44534](https://youtrack.jetbrains.com/issue/RIDER-44534), [#1852](https://github.com/JetBrains/resharper-unity/pull/1852))
- Add context actions to suppress expensive method highlighting when used in a performance critical context ([RIDER-46123](https://youtrack.jetbrains.com/issue/RIDER-46123), [#1248](https://github.com/JetBrains/resharper-unity/issues/1248), [#1852](https://github.com/JetBrains/resharper-unity/pull/1852))
- Add context actions to highlight a method as expensive when used in a performance critical context ([#1030](https://github.com/JetBrains/resharper-unity/issues/1030), [#1852](https://github.com/JetBrains/resharper-unity/pull/1852))
- Add context action to add `[BurstDiscard]` attribute to Burst compiled methods ([#1852](https://github.com/JetBrains/resharper-unity/pull/1852))
- Add inspection for correct constraint on type parameter of `SharedStatic` for Burst context ([#1852](https://github.com/JetBrains/resharper-unity/pull/1852))
- Add inspections for correct usage of `SharedStatic` in Burst code ([#1852](https://github.com/JetBrains/resharper-unity/pull/1852))
- Add option to disable Burst analysis ([#1852](https://github.com/JetBrains/resharper-unity/pull/1852))
- Add support for `[SerializeReference]` ([#1340](https://github.com/JetBrains/resharper-unity/issues/1340), [#1923](https://github.com/JetBrains/resharper-unity/pull/1923))
- Add performance critical context for event functions in various editor classes ([#1875](https://github.com/JetBrains/resharper-unity/pull/1875))
- Ensure navigating to source from PDBs is enabled for pre-compiled packages ([#1785](https://github.com/JetBrains/resharper-unity/issues/1785), [#1923](https://github.com/JetBrains/resharper-unity/pull/1923))
- Change defaults for implicit conversion hints to always show instead of push-to-hint ([#1923](https://github.com/JetBrains/resharper-unity/pull/1923))
- Use Unity terminology to describe event functions and serialised fields in identifier tooltips and inspections ([#1914](https://github.com/JetBrains/resharper-unity/pull/1914))
- Add annotation for `[NativeSetThreadIndex]` to mean implicit usage ([#1619](https://github.com/JetBrains/resharper-unity/issues/1619), [#1923](https://github.com/JetBrains/resharper-unity/pull/1923))
- Update API information to 2019.4.15f1, 2020.1.14f1 and 2020.2.0b12. Adds missing `EditorWindow.CreateGUI` message ([1937](https://github.com/JetBrains/resharper-unity/pull/1937))
- Rider: Highlight methods from a Burst compiled context with line marker and Code Vision links ([#1852](https://github.com/JetBrains/resharper-unity/pull/1852))
- Rider: Execute both edit and play mode tests in the same run ([#1894](https://github.com/JetBrains/resharper-unity/pull/1894))
- Rider: Run Unity menu item handlers via gutter icon ([RIDER-35911](https://youtrack.jetbrains.com/issue/RIDER-35911), [#1857](https://github.com/JetBrains/resharper-unity/pull/1857))
- Rider: Add context picker for shader files to work with `#if` directives ([RIDER-49212](https://youtrack.jetbrains.com/issue/RIDER-49212), [#1868](https://github.com/JetBrains/resharper-unity/pull/1868))

### Changed

- Minor improvements to File Templates ([#1856](https://github.com/JetBrains/resharper-unity/issues/1856), [#1873](https://github.com/JetBrains/resharper-unity/pull/1873))
- Removed usage inspection suppression for obsolete ECS injected fields ([#1923](https://github.com/JetBrains/resharper-unity/pull/1923))
- Rider: Add define symbols based on pragmas in shader files, such as `multi_compile`, `shader_feature`, `target`, `geometry`, etc. ([RIDER-49527](https://youtrack.jetbrains.com/issue/RIDER-49527), [#1826](https://github.com/JetBrains/resharper-unity/pull/1826))
- Rider: Launch Unity if not already running when trying to run, debug or cover unit tests ([#1831](https://github.com/JetBrains/resharper-unity/pull/1831))
- Rider: Replace Clear on Play option in Unity log view with filters for last play mode and last AppDomain reload ([RIDER-49887](https://youtrack.jetbrains.com/issue/RIDER-49887), [RIDER-15476](https://youtrack.jetbrains.com/issue/RIDER-15476) [#1833](https://github.com/JetBrains/resharper-unity/pull/1831))
- Rider: Minor update to icons to match Rider colour scheme

### Fixed

- Fix rename serialised field showing dialog twice with Player projects ([RIDER-52103](https://youtrack.jetbrains.com/issue/RIDER-52013), [#1921](https://github.com/JetBrains/resharper-unity/pull/1921))
- Fix missing remove redundant event function quick fix ([#1860](https://github.com/JetBrains/resharper-unity/issues/1860), [#1864](https://github.com/JetBrains/resharper-unity/pull/1864))
- Fix incorrect C# language version features suggested for player projects ([RIDER-52210](https://youtrack.jetbrains.com/issue/RIDER-52210), [#1890](https://github.com/JetBrains/resharper-unity/pull/1890))
- Fix incorrect namespace suggestions for Unity class libraries ([RIDER-51858](https://youtrack.jetbrains.com/issue/RIDER-51858), [#1898](https://github.com/JetBrains/resharper-unity/pull/1898))
- Fix changing name of wrong method while trying to override a new method ([RIDER-52404](https://youtrack.jetbrains.com/issue/RIDER-52404), [#1904](https://github.com/JetBrains/resharper-unity/pull/1904))
- Fix incorrect null reference warning after implicit null check ([RIDER-52665](https://youtrack.jetbrains.com/issue/RIDER-52665), [#1735](https://github.com/JetBrains/resharper-unity/issues/1735), [#1799](https://github.com/JetBrains/resharper-unity/issues/1799), [#1805](https://github.com/JetBrains/resharper-unity/issues/1805), [#1906](https://github.com/JetBrains/resharper-unity/pull/1906))
- Fix Create Asset Menu Quick Fix showing for `Editor` and `EditorWindow` classes ([#1702](https://github.com/JetBrains/resharper-unity/issues/1702), [#1704](https://github.com/JetBrains/resharper-unity/issues/1704), [#1876](https://github.com/JetBrains/resharper-unity/pull/1876))
- Fix exception parsing `EditorBuildSettings.asset` ([DEXP-554913](https://youtrack.jetbrains.com/issue/DEXP-554913), [#1916](https://github.com/JetBrains/resharper-unity/pull/1916))
- Fix incorrect base type required annotation for `HelpURLAttribute` ([#145](https://github.com/JetBrains/resharper-unity/issues/145), [#1923](https://github.com/JetBrains/resharper-unity/pull/1923))
- Fix broken `[Range]` and `[Min]` attribute integer value analysis ([#1788](https://github.com/JetBrains/resharper-unity/issues/1788), [#1923](https://github.com/JetBrains/resharper-unity/pull/1923))
- Fix incorrect redundant `[SerializeField]` attribute warnings for fields of generic type `T` ([#1885](https://github.com/JetBrains/resharper-unity/issues/1885), [#1923](https://github.com/JetBrains/resharper-unity/pull/1923))
- Fix incorrect redundant `[SerializeField]` attribute warnings for builtin types such as `Vector2Int` and `RectInt` ([RIDER-18729](https://youtrack.jetbrains.com/issue/RIDER-18729), [#1923](https://github.com/JetBrains/resharper-unity/pull/1923))
- Fix usage suppression for `ComponentSystemBase` ([#1895](https://github.com/JetBrains/resharper-unity/issues/1895), ([RIDER-46106](https://youtrack.jetbrains.com/issue/RIDER-46106), [#1923](https://github.com/JetBrains/resharper-unity/pull/1923))
- Fix `Dictionary` fields being treated as serialised fields ([#1776](https://github.com/JetBrains/resharper-unity/issues/1776), [#1923](https://github.com/JetBrains/resharper-unity/pull/1923))
- Fix `sfield` and `sprop` live templates being unavailable in `[Serializable]` types ([#1662](https://github.com/JetBrains/resharper-unity/issues/1662), [#1923](https://github.com/JetBrains/resharper-unity/pull/1923))
- Fix `.asmdef` file template missing under `Packages` folder ([#780](https://github.com/JetBrains/resharper-unity/issues/780), [#1923](https://github.com/JetBrains/resharper-unity/pull/1923))
- Fix asset usages not working with partial classes ([RIDER-50125](https://youtrack.jetbrains.com/issue/RIDER-50125), [#1935](https://github.com/JetBrains/resharper-unity/pull/1935))
- Fix exceptions when running plugin as part of ReSharper command line tools ([RIDER-53968](https://youtrack.jetbrains.com/issue/RIDER-53968), [#1935](https://github.com/JetBrains/resharper-unity/pull/1935))
- Fix false positive reports of repeated property access warnings with `MonoBehaviour` based components ([#1544](https://github.com/JetBrains/resharper-unity/issues/1544), [#1935](https://github.com/JetBrains/resharper-unity/pull/1935))
- Rider: Fix profiling Unity from Rider should not force DebugCodeOptimization ([RIDER-54917](https://youtrack.jetbrains.com/issue/RIDER-54917), [#1954](https://github.com/JetBrains/resharper-unity/pull/1954))
- Rider: Fix ignoring `[Explicit]` attribute on unit tests ([#1731](https://github.com/JetBrains/resharper-unity/issues/1731), [RIDER-48686](https://youtrack.jetbrains.com/issue/RIDER-48686), [#1825](https://github.com/JetBrains/resharper-unity/pull/1825))
- Rider: Handle parameterized test fixtures with parameterized tests ([RIDER-46658](https://youtrack.jetbrains.com/issue/RIDER-46658), [#1825](https://github.com/JetBrains/resharper-unity/pull/1825))
- Rider: Fix inconclusive result when explicitly running tests from two projects ([#1892](https://github.com/JetBrains/resharper-unity/pull/1892))
- Rider: Fix infinite "refreshing solution" when no connection to Unity editor plugin ([#1601](https://github.com/JetBrains/resharper-unity/issues/1601), [RIDER-48690](https://youtrack.jetbrains.com/issue/RIDER-48690), [#1828](https://github.com/JetBrains/resharper-unity/pull/1828))
- Rider: Fix ability to cancel unit tests while preparing to run ([#1886](https://github.com/JetBrains/resharper-unity/pull/1886))
- Rider: Fix missing animation frame from busy status icon ([#1862](https://github.com/JetBrains/resharper-unity/issues/1862), [#1865](https://github.com/JetBrains/resharper-unity/pull/1865))
- Rider: Fix exception in Unity run configuration templates with no project loaded ([RIDER-51997](https://youtrack.jetbrains.com/issue/RIDER-51997), [#1882](https://github.com/JetBrains/resharper-unity/pull/1882))
- Rider: Fix exception when trying to reconnect to Unity plugin ([DEXP-545880](https://youtrack.jetbrains.com/issue/DEXP-545880), [#1891](https://github.com/JetBrains/resharper-unity/pull/1891))
- Rider: Fix exception when updating editor notification for non-editable files ([RIDER-53722](https://youtrack.jetbrains.com/issue/RIDER-53722), [#1921](https://github.com/JetBrains/resharper-unity/pull/1921))
- Rider: Fix font size handling in Unity log tool window in presentation mode ([#1923](https://github.com/JetBrains/resharper-unity/pull/1923))
- Unity editor: Fix losing Console messages before first `EditorApplication.update` ([#1837](https://github.com/JetBrains/resharper-unity/pull/1837))
- Unity editor: Maintain last play time for log filtering across AppDomain reloads ([#1884](https://github.com/JetBrains/resharper-unity/pull/1884))
- Unity editor: Fix issues communicating with Rider when Rider is stopped and restarted ([#1881](https://github.com/JetBrains/resharper-unity/pull/1881))
- Unity editor: Fix crash in editor ([RIDER-52020](https://youtrack.jetbrains.com/issue/RIDER-52020), [#1889](https://github.com/JetBrains/resharper-unity/pull/1889))



## 2020.2.3
* Released: [2020-09-18](https://blog.jetbrains.com/dotnet/2020/09/18/resharper-rider-2020-2-3/)
* Build: 2020.2.2.314
* [No code changes](https://github.com/JetBrains/resharper-unity/compare/net202-rtm-2020.2.2...net202-rtm-2020.2.3)



## 2020.2.2
* Released: [2020-09-10](https://blog.jetbrains.com/dotnet/2020/09/10/resharper-rider-2020-2-2/)
* Build: 2020.2.2.311
* [Commits](https://github.com/JetBrains/resharper-unity/compare/net202-rtm-2020.2.0-rtm-2020.2.1...net202-rtm-2020.2.2)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/40?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/net202-rtm-2020.2.2)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/2020.2.2.311)

### Changed

- Rider: Updated "refresh in Unity" icon ([RIDER-44531](https://youtrack.jetbrains.com/issue/RIDER-44531), [#1792](https://github.com/JetBrains/resharper-unity/pull/1792))

### Fixed

- Correctly handle lambdas, actions and local functions in performance critical and Burst contexts ([#1787](https://github.com/JetBrains/resharper-unity/pull/1787))
- Recognise `OnValidate` as a valid message for `ScriptableObject` in Unity 2020.1.x projects ([RIDER-49130](https://youtrack.jetbrains.com/issue/RIDER-49130), [#1807](https://github.com/JetBrains/resharper-unity/pull/1807))
- Find shader includes in `file:` based packages ([#1817](https://github.com/JetBrains/resharper-unity/pull/1817))
- Rider: Correctly resolve local packages with too many parent segments in path ([#1796](https://github.com/JetBrains/resharper-unity/issues/1796), [#1811](https://github.com/JetBrains/resharper-unity/pull/1811))
- Rider: Correctly resolve embedded package where folder name does not match package name ([#1778](https://github.com/JetBrains/resharper-unity/issues/1778), [#1811](https://github.com/JetBrains/resharper-unity/pull/1811))



## 2020.2.1
* Released: [2020-08-21](https://blog.jetbrains.com/dotnet/2020/08/21/the-rider-2020-2-1-and-resharper-2020-2-1-hotfixes-are-here/)
* Build: 2020.2.0.281
* [Commits](https://github.com/JetBrains/resharper-unity/compare/net202-rtm-2020.2.0...net202-rtm-2020.2.0-rtm-2020.2.1)
* No milestone
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/net202-rtm-2020.2.0-rtm-2020.2.1)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/2020.2.0.281)

### Changed

- Add `__RESHARPER__` conditional symbol to shader files to help code insight features ([RIDER-45361](https://youtrack.jetbrains.com/issue/RIDER-45361))
- Add implicitly defined symbols for shaders such as `INTERNAL_DATA` ([RIDER-48368](https://youtrack.jetbrains.com/issue/RIDER-48368), [#1798](https://github.com/JetBrains/resharper-unity/pull/1798))
- Remove shader files from Solution Wide Error Analysis
- Improve suppression of unresolved error highlighting for shader files ([RIDER-49123](https://youtrack.jetbrains.com/issue/RIDER-49123))

### Fixed

- Fixed errors when showing compiled shader output ([RIDER-49218](https://youtrack.jetbrains.com/issue/RIDER-49218), [#1798](https://github.com/JetBrains/resharper-unity/pull/1798))



## 2020.2
* Released: [2020-08-13](https://blog.jetbrains.com/dotnet/2020/08/13/rider-2020-2-released/)
* Build: 2020.2.0.261
* [Commits](https://github.com/JetBrains/resharper-unity/compare/net201...net202-rtm-2020.2.0)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/36?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/net202-rtm-2020.2.0)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/2020.2.0.261)

### Added

- Add Unity code cleanup patterns that do not reorder serialised fields ([#88](https://github.com/JetBrains/resharper-unity/issues/88), [#1676](https://github.com/JetBrains/resharper-unity/pull/1676))
- Use `Range` and `Min` attributes to provide hints to integer dataflow analysis ([#1673](https://github.com/JetBrains/resharper-unity/pull/1673), [#1714](https://github.com/JetBrains/resharper-unity/pull/1714))
- Improve namespace suggestions for packages ([#1161](https://github.com/JetBrains/resharper-unity/issues/1161), [RIDER-36546](https://youtrack.jetbrains.com/issue/RIDER-36546), [#1677](https://github.com/JetBrains/resharper-unity/pull/1677), [#1689](https://github.com/JetBrains/resharper-unity/pull/1689))
- Add Unity specific spell check dictionary ([#1187](https://github.com/JetBrains/resharper-unity/issues/1187), [#1570](https://github.com/JetBrains/resharper-unity/pull/1570))
- Add support for UnityEditor.dll being split into modules in Unity 2020.2 ([#1699](https://github.com/JetBrains/resharper-unity/issues/1699), [RIDER-46544](https://youtrack.jetbrains.com/issue/RIDER-46544), [#1724](https://github.com/JetBrains/resharper-unity/pull/1724))
- Rider: Add "pausepoints" a type of breakpoint that doesn't suspend code execution, but pauses the Unity editor ([#1272](https://github.com/JetBrains/resharper-unity/issues/1272), [#1661](https://github.com/JetBrains/resharper-unity/pull/1661))
- Rider: Add USB debugging for iPhone/iPad ([RIDER-25430](https://youtrack.jetbrains.com/issue/RIDER-25430), [#1734](https://github.com/JetBrains/resharper-unity/pull/1734))
- Rider: Add analysis and inspections for Burst compiled code ([#1665](https://github.com/JetBrains/resharper-unity/pull/1665))
- Rider: Add option to disable debugger extensions ([#840](https://github.com/JetBrains/resharper-unity/issues/840), [#1741](https://github.com/JetBrains/resharper-unity/pull/1741))
- Rider: Add "Active Scene" and "this.gameObject" as automatic debugger watch expressions ([#1741](https://github.com/JetBrains/resharper-unity/pull/1741))
- Rider: Add debugging summary information for various Unity types, such as `GameObject`, `MeshFilter` and `Behaviour` ([#1741](https://github.com/JetBrains/resharper-unity/pull/1741))
- Rider: Automatically use UnityYamlMerge to merge asset files ([RIDER-33411](https://youtrack.jetbrains.com/issue/RIDER-33411), [#948](https://github.com/JetBrains/resharper-unity/issues/948), [#1682](https://github.com/JetBrains/resharper-unity/pull/1682))
- Rider: Automatically add run configuration for standalone player build targets ([#1708](https://github.com/JetBrains/resharper-unity/pull/1708))
- Rider: Add sample text for "Unity", "ShaderLab" and "Cg/HLSL" Colour Scheme options pages ([#1667](https://github.com/JetBrains/resharper-unity/pull/1667))
- Rider: Filter `.meta` files from the navigation bar ([RIDER-28425](https://youtrack.jetbrains.com/issue/RIDER-28425), [#1703](https://github.com/JetBrains/resharper-unity/pull/1703))

### Changed

- All applicable quick fixes are now bulk actions, and can be applied over project scope ([#1648](https://github.com/JetBrains/resharper-unity/issues/1648), [#1649](https://github.com/JetBrains/resharper-unity/pull/1649))
- Significant reduction in memory usage while indexing assets ([#1645](https://github.com/JetBrains/resharper-unity/pull/1645))
- Improved method signature validation and usage suppression using Unity's `RequiredSignature` attribute ([#1679](https://github.com/JetBrains/resharper-unity/pull/1679))
- Fields must have correct type to be considered a serialised field ([#1605](https://github.com/JetBrains/resharper-unity/issues/1605), [#1638](https://github.com/JetBrains/resharper-unity/pull/1638), [#1720](https://github.com/JetBrains/resharper-unity/pull/1720))
- Update API information to Unity 2020.2.0a18 ([#1760](https://github.com/JetBrains/resharper-unity/pull/1760))
- Added undocumented event functions for `Editor` and `EditorWindow` ([#986](https://github.com/JetBrains/resharper-unity/issues/986), [#1453](https://github.com/JetBrains/resharper-unity/issues/1453), [#1760](https://github.com/JetBrains/resharper-unity/pull/1760))
- Improve searching for Linux Unity installations ([#1763](https://github.com/JetBrains/resharper-unity/pull/1763))
- Rider: Better support for prefab modifications in Find Usages and showing Inspector values ([#1645](https://github.com/JetBrains/resharper-unity/pull/1645))
- Rider: Show method handlers for Unity events in the editor ([#1645](https://github.com/JetBrains/resharper-unity/pull/1645))
- Rider: Allow aborting tests while waiting for Unity ([#1675](https://github.com/JetBrains/resharper-unity/issues/1675), [36389](https://youtrack.jetbrains.com/issue/RIDER-36389), [#1692](https://github.com/JetBrains/resharper-unity/pull/1692))
- Rider: Allow launching and debugging standalone player via run configuration ([#1708](https://github.com/JetBrains/resharper-unity/pull/1708))
- Rider: Group processes by project in Attach to Unity Process dialog ([#1730](https://github.com/JetBrains/resharper-unity/pull/1730))
- Rider: Filter deprecated Unity base type properties from debugger views ([#1741](https://github.com/JetBrains/resharper-unity/pull/1741))
- Rider: Show various Unity types such as `Vector3` with full float precision ([#1515](https://github.com/JetBrains/resharper-unity/issues/1515), [#1741](https://github.com/JetBrains/resharper-unity/pull/1741))
- Rider: Group large lists of child game objects into chunks and lazily evaluate results ([#1741](https://github.com/JetBrains/resharper-unity/pull/1741))
- Rider: Improve editor discovery to refresh while Attach to Unity Process dialog is open ([#1730](https://github.com/JetBrains/resharper-unity/pull/1730))
- Rider: Disable "Start Unity" action when Unity is running ([RIDER-36108](https://youtrack.jetbrains.com/issue/RIDER-36108), [#1554](https://github.com/JetBrains/resharper-unity/pull/1554))
- Rider: Unity Log view optionally scrolls to show new items ([RIDER-14377](https://youtrack.jetbrains.com/issue/RIDER-14377), [#1678](https://github.com/JetBrains/resharper-unity/pull/1678))
- Rider: Play/pause/step buttons no longer disabled while Rider is indexing ([#1678](https://github.com/JetBrains/resharper-unity/pull/1678))
- Rider: Support local tarball packages in Unity Explorer ([#1589](https://github.com/JetBrains/resharper-unity/issues/1589), [#1769](https://github.com/JetBrains/resharper-unity/pull/1769))
- Rider: Support `UPM_CACHE_PATH` environment variable for package cache fallback in Unity Explorer ([#1766](https://github.com/JetBrains/resharper-unity/issues/1766), [#1769](https://github.com/JetBrains/resharper-unity/pull/1769))
- Rider: Update `.meta` file icons to something less distracting ([RIDER-45675](https://youtrack.jetbrains.com/issue/RIDER-45675), [#1698](https://github.com/JetBrains/resharper-unity/pull/1698))

### Fixed

- Fix meta file handling when references to Unity assemblies are invalid ([#1623](https://github.com/JetBrains/resharper-unity/pull/1623))
- Fix incorrect method signature validation for methods marked with `OnOpenedAsset` ([#1053](https://github.com/JetBrains/resharper-unity/issues/1053), [#1679](https://github.com/JetBrains/resharper-unity/pull/1679))
- Fix missing information in `.asmdef` schema file ([#1739](https://github.com/JetBrains/resharper-unity/issues/1739), [#1743](https://github.com/JetBrains/resharper-unity/pull/1743))
- Rider: Fix debugger sometimes treating user code as external code ([#1671](https://github.com/JetBrains/resharper-unity/issues/1671), [RIDER-43846](https://youtrack.jetbrains.com/issue/RIDER-43846), [#1697](https://github.com/JetBrains/resharper-unity/pull/1697))
- Rider: Fix timeout in debugger due to logging while evaluating properties ([RIDER-37068](https://youtrack.jetbrains.com/issue/RIDER-37068), [#1765](https://github.com/JetBrains/resharper-unity/pull/1765))
- Rider: Fix grouping assets by directory in Find Usages results ([#1668](https://github.com/JetBrains/resharper-unity/pull/1668))
- Rider: Fix exception trying to upgrade Unity editor plugin ([RIDER-42475](https://youtrack.jetbrains.com/issue/RIDER-42475), [#1658](https://github.com/JetBrains/resharper-unity/pull/1658))
- Rider: Fix unit tests not running unless Rider has focus ([RIDER-37990](https://youtrack.jetbrains.com/issue/RIDER-37990), [#1672](https://github.com/JetBrains/resharper-unity/pull/1672))
- Rider: Fix test runner hanging using continuous testing ([#1695](https://github.com/JetBrains/resharper-unity/issues/1695), [#1692](https://github.com/JetBrains/resharper-unity/pull/1692))
- Rider: Rewrite player discovery to be more efficient and to process all broadcast messages ([#1730](https://github.com/JetBrains/resharper-unity/pull/1730))
- Rider: Fix editor discovery listing incorrect processes in Attach to Unity Process dialog ([#1478](https://github.com/JetBrains/resharper-unity/issues/1478), [#1730](https://github.com/JetBrains/resharper-unity/pull/1730))
- Rider: Suppress incorrect refresh behaviour when saving files that are in multiple projects (e.g. Player projects) ([#1669](https://github.com/JetBrains/resharper-unity/pull/1669))
- Rider: Fix incorrect component name shown in Code Vision ([#1713](https://github.com/JetBrains/resharper-unity/pull/1713))
- Rider: Fix issues moving files in Unity Explorer when player projects are generated ([RIDER-46467](https://youtrack.jetbrains.com/issue/RIDER-46467), [#1738](https://github.com/JetBrains/resharper-unity/pull/1738))
- Rider: Fix git packages resolving in Unity Explorer in Unity 2019.4+ ([RIDER-47191](https://youtrack.jetbrains.com/issue/RIDER-47191), [#1769](https://github.com/JetBrains/resharper-unity/pull/1769))
- Unity editor: Fix reporting of duration of Unity tests (released in Rider package 2.0.4) ([RIDER-44853](https://youtrack.jetbrains.com/issue/RIDER-44853))
- Unity editor: Delay calling Unity API to workaround potential Unity crash ([RIDER-43951](https://youtrack.jetbrains.com/issue/RIDER-43951), [#1647](https://github.com/JetBrains/resharper-unity/pull/1647))
- Unity editor: Unchecking sending Console messages to Rider is respected without having to restart ([#1733](https://github.com/JetBrains/resharper-unity/pull/1733))



## 2020.1.4
* Released: [2020-07-09](https://blog.jetbrains.com/dotnet/2020/07/09/rider-2020-1-4-and-resharper-ultimate-2020-1-4-bugfixes-are-ready/)
* [No code changes](https://github.com/JetBrains/resharper-unity/compare/net201-rtm-2020.1.3...net201-rtm-2020.1.4)
* [GitHub tag](https://github.com/JetBrains/resharper-unity/releases/tag/net201-rtm-2020.1.4)



## 2020.1.3
* Released: [2020-05-19](https://blog.jetbrains.com/dotnet/2020/05/19/rider-resharper-ultimate-2020-1-3/)
* Build: 2020.1.0.182
* [Commits](https://github.com/JetBrains/resharper-unity/compare/net201-rtm-2020.1.2...net201-rtm-2020.1.3)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/39?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/net201-rtm-2020.1.3)
* No ReSharper release required.

### Fixed
- Rider: Fix unit test discovery in non-Unity solutions inside a Unity folder ([RIDER-44139](https://youtrack.jetbrains.com/issue/RIDER-44139), [#1657](https://github.com/JetBrains/resharper-unity/pull/1657))



## 2020.1.2
* Released: [2020-05-07](https://blog.jetbrains.com/dotnet/2020/05/07/rider-resharper-ultimate-2020-1-2/)
* Build: 2020.1.0.179
* [Commits](https://github.com/JetBrains/resharper-unity/compare/net201-rtm-2020.1.0-rtm-2020.1.1...net201-rtm-2020.1.2)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/37?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/net201-rtm-2020.1.2)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/2020.1.0.179)

### Changed

- Rider: Show serializable Code Vision for more fields ([#1624](https://github.com/JetBrains/resharper-unity/pull/1624))
- Rider: Improve presentation for asset Find Usages results ([#1624](https://github.com/JetBrains/resharper-unity/pull/1624))
- Rider: Improve presentation of Player projects in Unity Explorer ([#1634](https://github.com/JetBrains/resharper-unity/pull/1634))
- Unity Editor: Reduce frequency of refreshing Unity on save to explicit calls to Save All ([RIDER-37420](https://youtrack.jetbrains.com/issue/RIDER-37420), [#1629](https://github.com/JetBrains/resharper-unity/pull/1629))

### Fixed

- Fix incorrect base type required warning for `ExecuteAlways` attribute ([#1642](https://github.com/JetBrains/resharper-unity/pull/1642))
- Fix generation of static constructor for redundant `[InitializeOnLoad]` when member generator set to "default return value" ([#1625](https://github.com/JetBrains/resharper-unity/issues/1625), [#1644](https://github.com/JetBrains/resharper-unity/pull/1644))
- Rider: Fix solution hang on "constructing components" due to excessive `FileSystemWatcher` initialisation ([RIDER-41812](https://youtrack.jetbrains.com/issue/RIDER-41812), [#1631](https://github.com/JetBrains/resharper-unity/pull/1631))
- Rider: Fix exception finding file icon causing explorer view to be blank ([RIDER-43038](https://youtrack.jetbrains.com/issue/RIDER-43038), [#1632](https://github.com/JetBrains/resharper-unity/pull/1632))
- Rider: Fix handling of file system folders in `Packages` with the same name as a package ([#1626](https://github.com/JetBrains/resharper-unity/issues/1626), [#1632](https://github.com/JetBrains/resharper-unity/pull/1632))
- Rider: Fix size of tooltip for packages with many projects ([#1628](https://github.com/JetBrains/resharper-unity/issues/1628), [#1632](https://github.com/JetBrains/resharper-unity/pull/1632))
- Rider: Avoid potential deadlocks when updating Packages tree in Unity Explorer ([RIDER-43317](https://youtrack.jetbrains.com/issue/RIDER-43317), [#1643](https://github.com/JetBrains/resharper-unity/pull/1643))
- Rider: Fix ability to customise Unity Explorer nodes from plugins ([RIDER-39139](https://youtrack.jetbrains.com/issue/RIDER-39139), [#1646](https://github.com/JetBrains/resharper-unity/pull/1646))
- Rider: Fix C# language level detection on Linux ([#1640](https://github.com/JetBrains/resharper-unity/pull/1640))
- Unity Editor: Fix list of folders to search for .NET Framework references ([RIDER-42873](https://youtrack.jetbrains.com/issue/RIDER-42873), [#1630](https://github.com/JetBrains/resharper-unity/pull/1630))



## 2020.1.1
* Released: 2020-04-29
* Build: 2020.1.0.162
* [No code changes](https://github.com/JetBrains/resharper-unity/compare/net201-rtm-2020.1.0...net201-rtm-2020.1.0-rtm-2020.1.1)
* [GitHub tag](https://github.com/JetBrains/resharper-unity/releases/tag/net201-rtm-2020.1.0-rtm-2020.1.1)



## 2020.1
* Released: [2020-04-16](https://blog.jetbrains.com/dotnet/2020/04/16/rider-2020-1-released/)
* Build: 2020.1.0.161
* [Commits](https://github.com/JetBrains/resharper-unity/compare/net193...net201-rtm-2020.1.0)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/32?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/net201-rtm-2020.1.0)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/2020.1.0.161)

### Added

- Add performance inspection - prefer jagged array to multidimensional array access, with Quick Fix ([RIDER-22818](https://youtrack.jetbrains.com/issue/RIDER-22812), [#1459](https://github.com/JetBrains/resharper-unity/pull/1459))
- Exclude `Boo` and `UnityScript` namespaces, as well as the `System.Diagnostics.Debug` type from import completion ([#574](https://github.com/JetBrains/resharper-unity/issues/574), [#1473](https://github.com/JetBrains/resharper-unity/pull/1473))
- Add more attributes to external annotations. E.g. `ShortcutAttribute` will mark the method as in use ([#1546](https://github.com/JetBrains/resharper-unity/issues/1546), [RIDER-40330](https://youtrack.jetbrains.com/issue/RIDER-40330), [#1548](https://github.com/JetBrains/resharper-unity/pull/1548))
- Find Usages for serialised fields now includes asset usages ([#1530](https://github.com/JetBrains/resharper-unity/pull/1530))
- Add context action to create Unity Assets menu item for a `ScriptableObject` ([#1567](https://github.com/JetBrains/resharper-unity/pull/1567))
- Show serialised field values for scriptable objects ([#1567](https://github.com/JetBrains/resharper-unity/pull/1567))
- Show usages of scriptable objects in assets ([#1567](https://github.com/JetBrains/resharper-unity/pull/1567))
- Rider: Open corresponding `.asmdef` in Unity Inspector from `.csproj` editor notification ([#1574](https://github.com/JetBrains/resharper-unity/pull/1574))
- Rider: Treat `.inputactions` as a JSON file ([RIDER-38538](https://youtrack.jetbrains.com/issue/RIDER-38538))

### Changed

- Indexing of assets deferred until after project loaded and will not interfere with existing code insight features ([#1530](https://github.com/JetBrains/resharper-unity/pull/1530))
- Improved memory usage while parsing assets ([#1530](https://github.com/JetBrains/resharper-unity/pull/1530))
- Improved support of nested and variant prefabs ([#1530](https://github.com/JetBrains/resharper-unity/pull/1530))
- Serialised field values Code Vision now includes values from FomerlySerialisedAs attribute ([#1530](https://github.com/JetBrains/resharper-unity/pull/1530))
- Serialised field values Code Vision now includes derived classes ([#1550](https://github.com/JetBrains/resharper-unity/pull/1550))
- Show performance critical highlights for known methods without requiring Solution Wide Analysis enabled ([#1459](https://github.com/JetBrains/resharper-unity/pull/1459))
- Stop marking a method as expensive if it only contains a null check ([#1459](https://github.com/JetBrains/resharper-unity/pull/1459))
- Move vector multiplication order inspection to performance critical context only ([#1459](https://github.com/JetBrains/resharper-unity/pull/1459))
- Sort commonly used event functions higher in Generate dialog ([#1566](https://github.com/JetBrains/resharper-unity/pull/1566))
- Generate event functions at location of context action, rather than at end of class ([#1542](https://github.com/JetBrains/resharper-unity/issues/1542), [#1566](https://github.com/JetBrains/resharper-unity/pull/1566))
- Updated API information to 2020.1.0a25 ([#1553](https://github.com/JetBrains/resharper-unity/pull/1553))
- Remove messages that use obsolete parameter types ([#1545](https://github.com/JetBrains/resharper-unity/issues/1545), [#1553](https://github.com/JetBrains/resharper-unity/pull/1553))
- Rider: Adding file to Unity Explorer will add to correct C# project ([RIDER-23169](https://youtrack.jetbrains.com/issue/RIDER-23169), [#1470](https://github.com/JetBrains/resharper-unity/pull/1470), [#1501](https://github.com/JetBrains/resharper-unity/pull/1501))
- Rider: Show folders ending with `~` by default in Unity Explorer ([#1444](https://github.com/JetBrains/resharper-unity/issues/1444), [1506](https://github.com/JetBrains/resharper-unity/pull/1506))
- Rider: Move Unity Explorer settings to main "gear" icon ([#1506](https://github.com/JetBrains/resharper-unity/pull/1506))
- Rider: Interesting content in builtin packages is now visible by default ([#1556](https://github.com/JetBrains/resharper-unity/pull/1556))
- Rider: Only show "Show in Unity" link for Unity generated files when connected to Unity ([#1574](https://github.com/JetBrains/resharper-unity/pull/1574))
- Rider: Improve detection of Unity version, especially after upgrading project ([#1507](https://github.com/JetBrains/resharper-unity/issues/1507), [#1572](https://github.com/JetBrains/resharper-unity/pull/1572))
- Unity Editor: Move caret to correct column when opening file ([RIDER-27450](https://youtrack.jetbrains.com/issue/RIDER-27450), [#1486](https://github.com/JetBrains/resharper-unity/pull/1486))
- Unity Editor: Delete the old Rider plugin when opening a project in Unity 2019.2+ ([#1591](https://github.com/JetBrains/resharper-unity/pull/1591))

### Fixed

- Fix incorrect redundant `SerializeField` attribute warning for property backing field ([#1016](https://github.com/JetBrains/resharper-unity/issues/1016), [#1464](https://github.com/JetBrains/resharper-unity/pull/1464))
- Avoid creating meta files outside of Asset or Packages folders (from 2019.3.2) ([#1481](https://github.com/JetBrains/resharper-unity/issues/1481), [#1491](https://github.com/JetBrains/resharper-unity/pull/1491), [#1489](https://github.com/JetBrains/resharper-unity/pull/1489))
- Fix overwriting `IEnumerator` when auto-completing an event function that can be a coroutine ([#1258](https://github.com/JetBrains/resharper-unity/issues/1258), [#1566](https://github.com/JetBrains/resharper-unity/pull/1566))
- Fix duplicate "Generate Unity event functions" context action when gutter icons are visible ([#1537](https://github.com/JetBrains/resharper-unity/issues/1537), [#1566](https://github.com/JetBrains/resharper-unity/pull/1566))
- Fix completion of tag value adding extra closing quote ([RIDER-33067](https://youtrack.jetbrains.com/issue/RIDER-33067))
- Fix exception with building shortcut cache ([RIDER-41206](https://youtrack.jetbrains.com/issue/RIDER-41206))
- Rider: Fix Unity tests working with `.slnf` files ([#1571](https://github.com/JetBrains/resharper-unity/issues/1571), [#1577](https://github.com/JetBrains/resharper-unity/pull/1577))
- Rider: Fix tooltip display for packages in Unity Explorer ([#1506](https://github.com/JetBrains/resharper-unity/pull/1506))
- Rider: Fix resolving git based packages in Unity 2019.3+ ([#1616](https://github.com/JetBrains/resharper-unity/pull/1616))
- Rider: Fix settings search not finding Unity pages (from 2019.3.3) ([#1516](https://github.com/JetBrains/resharper-unity/issues/1516), [#1520](https://github.com/JetBrains/resharper-unity/pull/1520))
- Rider: Fix discovery and running of all tests in a project ([#1509](https://github.com/JetBrains/resharper-unity/issues/1509), [#1500](https://github.com/JetBrains/resharper-unity/pull/1500))
- Rider: Fix finding location of Unity based on custom Hub install location ([RIDER-42118](https://youtrack.jetbrains.com/issue/RIDER-42118), [#1604](https://github.com/JetBrains/resharper-unity/pull/1604))
- Rider: Use correct process ID for profiling and coverage ([DTRC-26621](https://youtrack.jetbrains.com/issue/DTRC-26621), [#1612](https://github.com/JetBrains/resharper-unity/pull/1612))
- Rider: Mark editor as disconnected if response is not timely ([#1610](https://github.com/JetBrains/resharper-unity/pull/1610))
- Unity Editor: Fix jumping to default desktop when opening files on Mac ([#1611](https://github.com/JetBrains/resharper-unity/pull/1611))



## 2019.3.4
* Released: 2020-02-28
* Build: 2019.3.0.234
* [No code changes](https://github.com/JetBrains/resharper-unity/compare/net193-eap-rtm-2019.3.3...net193-eap-rtm-2019.3.3-rtm-2019.3.4)
* [GitHub tag](https://github.com/JetBrains/resharper-unity/releases/tag/net193-eap-rtm-2019.3.3-rtm-2019.3.4)



## 2019.3.3
* Released: [2020-02-21](https://blog.jetbrains.com/dotnet/2020/02/21/rider-2019-3-3/)
* Build: 2019.3.0.226
* [Commits](https://github.com/JetBrains/resharper-unity/compare/net193-eap8-rtm-2019.3.2...net193-eap-rtm-2019.3.3)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/35?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/net193-eap-rtm-2019.3.3)
* No ReSharper release required.

### Fixed

- Rider: Fix settings search not finding Unity pages ([#1516](https://github.com/JetBrains/resharper-unity/issues/1516), [#1522](https://github.com/JetBrains/resharper-unity/pull/1522))



## 2019.3.2
* Released: [2020-02-12](https://blog.jetbrains.com/dotnet/2020/02/12/rider-2019-3-2/)
* Build: 2019.3.0.208
* [Commits](https://github.com/JetBrains/resharper-unity/compare/net193-eap7-rtm-2019.3.0-rtm-2019.3.1...net193-eap8-rtm-2019.3.2)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/34?closed=1)
* [ReSharper release (2020-02-16)](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/2019.3.0.208)

### Changed

- Unity Editor: Find Usages window now allows replacing current open scene ([#1479](https://github.com/JetBrains/resharper-unity/issues/1479), [#1480](https://github.com/JetBrains/resharper-unity/pull/1480))

### Fixed

- Avoid creating meta files outside of Asset or Packages folders ([#1489](https://github.com/JetBrains/resharper-unity/issues/1489), [#1491](https://github.com/JetBrains/resharper-unity/pull/1491))



## 2019.3.1
* Released: [2019-12-20](https://blog.jetbrains.com/dotnet/2019/12/20/rider-2019-3-1/)
* Build: 2019.3.0.162
* [Commits](https://github.com/JetBrains/resharper-unity/compare/net193-eap7-rtm-2019.3.0...net193-eap7-rtm-2019.3.0-rtm-2019.3.1)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/33?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/net193-eap7-rtm-2019.3.0-rtm-2019.3.1)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/2019.3.0.162)

### Added

- Rider: Add proper file icons for `*.uxml` and `*.uss` ([RIDER-34788](https://youtrack.jetbrains.com/issue/RIDER-34788), [#1443](https://github.com/JetBrains/resharper-unity/pull/1443))

### Changed

- Rider: Entire plugin is no longer disabled if the CSS plugin is disabled ([RIDER-36523](https://youtrack.jetbrains.com/issue/RIDER-36523), [#1443](https://github.com/JetBrains/resharper-unity/pull/1443))
- Rider: Make Attach to Unity Process dialog resizable ([#1446](https://github.com/JetBrains/resharper-unity/issues/1446), [#1450](https://github.com/JetBrains/resharper-unity/pull/1450))
- Rider: Identify child processes by role in Attach to Unity Process dialog ([#1328](https://github.com/JetBrains/resharper-unity/issues/1328), [#1450](https://github.com/JetBrains/resharper-unity/pull/1450))

### Fixed

- Fix usage count for custom event based event handlers in Unity 2018.4+ ([#1448](https://github.com/JetBrains/resharper-unity/issues/1448), [#1449](https://github.com/JetBrains/resharper-unity/pull/1449))
- Rider: Show correct project name when Unity started with certain command line on Windows ([#1450](https://github.com/JetBrains/resharper-unity/pull/1450))
- Rider: Show correct project name when multiple Unity processes listed in Attach to Process popup list ([#1456](https://github.com/JetBrains/resharper-unity/issues/1456), [#1450](https://github.com/JetBrains/resharper-unity/pull/1450))
- Rider: Fix exception in Attach to Unity Process dialog causing list to be empty ([#1454](https://github.com/JetBrains/resharper-unity/issues/1454), [#1450](https://github.com/JetBrains/resharper-unity/pull/1450))
- Rider: Show run configuration dialog for Unity class library projects ([#1445](https://github.com/JetBrains/resharper-unity/issues/1445), [#1450](https://github.com/JetBrains/resharper-unity/pull/1450))
- Rider: Fix finding existing Unity instance to debug ([RIDER-36256](https://youtrack.jetbrains.com/issue/RIDER-36256), [#1450](https://github.com/JetBrains/resharper-unity/pull/1450))
- Rider: Fix `EditorInstance.json` being locked by Rider ([#1450](https://github.com/JetBrains/resharper-unity/pull/1450))



## 2019.3
* Released: [2019-12-11](https://blog.jetbrains.com/dotnet/2019/12/11/rider-2019-3/)
* Build: 2019.3.0.124
* [Commits](https://github.com/JetBrains/resharper-unity/compare/192...net193-eap7-rtm-2019.3.0)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/29?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/net193-eap7-rtm-2019.3.0)
* ReSharper release delayed until 2019.3.1

### Added

- Update API information to 2019.3.0b11 ([#1412](https://github.com/JetBrains/resharper-unity/pull/1412))
- Methods with the `[SettingsProvider]` attribute are now marked as implicitly used ([#1225](https://github.com/JetBrains/resharper-unity/issues/1225), [#1362](https://github.com/JetBrains/resharper-unity/pull/1362))
- Support Find Usages on `UnityEvent` based event handlers ([#1142](https://github.com/JetBrains/resharper-unity/pull/1142))
- Add context action for creating method from unresolved string literal in `StartCoroutine` and `StopCoroutine` ([#RIDER-27707](https://youtrack.jetbrains.com/issue/RIDER-27707), [#1416](https://github.com/JetBrains/resharper-unity/pull/1416))
- Rider: Add support for play mode tests ([#1293](https://github.com/JetBrains/resharper-unity/issues/1293), [RIDER-19513](https://youtrack.jetbrains.com/issue/RIDER-19513))
- Rider: Add code coverage and continuous testing for Unity tests ([#1410](https://github.com/JetBrains/resharper-unity/pull/1410))
- Rider: Add syntax highlighting, schema generation and validation of UXML files ([#1399](https://github.com/JetBrains/resharper-unity/pull/1399))
- Rider: Add syntax highlighting, validation and completion for USS files ([#957](https://github.com/JetBrains/resharper-unity/issues/957), [RIDER-20576](https://youtrack.jetbrains.com/issue/RIDER-20576), [#1402](https://github.com/JetBrains/resharper-unity/pull/1402))
- Rider: Add Unity performance options to new inspection control panel ([#1408](https://github.com/JetBrains/resharper-unity/pull/1408))
- Rider: Show prompt to set Rider as default external editor when Unity is started by Rider ([#1127](https://github.com/JetBrains/resharper-unity/issues/1127), [#1270](https://github.com/JetBrains/resharper-unity/pull/1270))
- Rider: Show prompt for Linux users to install Mono 5.16+ ([#1375](https://github.com/JetBrains/resharper-unity/issues/1375), [#1383](https://github.com/JetBrains/resharper-unity/pull/1383))
- Rider: Show prompt if currently selected external editor does not match current Rider ([RIDER-35297](https://youtrack.jetbrains.com/issue/RIDER-35297), [#1409](https://github.com/JetBrains/resharper-unity/pull/1409))
- Rider: Add Unity specific Live Templates settings page ([#1351](https://github.com/JetBrains/resharper-unity/pull/1351))
- Rider: Add project name to "Attach to Unity Process" and "Attach to Unity Editor" run configuration dialogs ([#1009](https://github.com/JetBrains/resharper-unity/issues/1009), [#RIDER-31184](https://youtrack.jetbrains.com/issue/RIDER-31184), [#1298](https://github.com/JetBrains/resharper-unity/pull/1298))
- Rider: Add support for Clear on Play now in Rider's Unity log viewer ([#1281](https://github.com/JetBrains/resharper-unity/issues/1281), [#1294](https://github.com/JetBrains/resharper-unity/pull/1294))
- Unity Editor: Bring Unity Editor to foreground when Rider is showing Unity asset usages ([#1344](https://github.com/JetBrains/resharper-unity/pull/1344))

### Changed

- Improve performance parsing YAML scenes ([#1408](https://github.com/JetBrains/resharper-unity/pull/1408))
- Rider: Return support for .asmdef files ([RIDER-30018](https://youtrack.jetbrains.com/issue/RIDER-30018), [#1373](https://github.com/JetBrains/resharper-unity/pull/1373))
- Rider: Improve UX of "Attach to Unity Process" dialog ([#1278](https://github.com/JetBrains/resharper-unity/issues/1278), [#1298](https://github.com/JetBrains/resharper-unity/pull/1298))
- Rider: Improve display of count of merged log items ([#1296](https://github.com/JetBrains/resharper-unity/issues/1296), [#1301](https://github.com/JetBrains/resharper-unity/pull/1301))
- Rider: Status bar icon will show when Unity Editor is paused ([#1227](https://github.com/JetBrains/resharper-unity/issues/1227), [#1301](https://github.com/JetBrains/resharper-unity/pull/1301))
- Rider: Show Unity actions toolbar when opening a folder without a solution, to make it easy to launch Unity ([#1325](https://github.com/JetBrains/resharper-unity/pull/1325))
- Rider: Show asset usage count on property setter and `UnityEvent` based event handlers ([#1142](https://github.com/JetBrains/resharper-unity/pull/1142))
- Unity Editor: Use new 2019.2 API to open Rider at correct column as well as line (requires Rider package 1.1.0+) ([#888](https://github.com/JetBrains/resharper-unity/issues/888))
- Unity Editor: Don't create `EditorInstance.json` for Unity 2017.1+, since it does it itself ([#1356](https://github.com/JetBrains/resharper-unity/pull/1356))
- Unity Editor: Reduce size of pre-compiled editor plugin for Unity 20192.2+ to help AppDomain restart performance ([#1367](https://github.com/JetBrains/resharper-unity/pull/1367), [#1390](https://github.com/JetBrains/resharper-unity/pull/1390))

### Fixed

- Fix overridden `Update` methods not acting as performance critical context ([RIDER-33934](https://youtrack.jetbrains.com/issue/RIDER-33934), [#1408](https://github.com/JetBrains/resharper-unity/pull/1408))
- Fix Quick Fix incorrectly converting `LinecastAll` to `CapsuleCastNonAlloc` instead of `LinecastNonAlloc` ([#1324](https://github.com/JetBrains/resharper-unity/issues/1324), [RIDER-33442](https://youtrack.jetbrains.com/issue/RIDER-33443), [#1408](https://github.com/JetBrains/resharper-unity/pull/1408))
- Fix finding usages of methods used as event handler from prefab ([#1331](https://github.com/JetBrains/resharper-unity/issues/1331), [#1408](https://github.com/JetBrains/resharper-unity/pull/1408))
- Fix moving `.meta` file during "Move to Folder" refactoring ([#1370](https://github.com/JetBrains/resharper-unity/issues/1370), [#1389](https://github.com/JetBrains/resharper-unity/pull/1389))
- Fix orphan `.meta` file during "Safe Delete" refactoring ([#856](https://github.com/JetBrains/resharper-unity/issues/856), [#1389](https://github.com/JetBrains/resharper-unity/pull/1389))
- Fix correctly keeping `.meta` files up to date in `Packages` folder ([#1231](https://github.com/JetBrains/resharper-unity/issues/1231), [#1389](https://github.com/JetBrains/resharper-unity/pull/1389))
- Fix "Add RequireComponent" context action to correctly add second attribute ([#RIDER-34390](https://youtrack.jetbrains.com/issue/RIDER-34390), [#1416](https://github.com/JetBrains/resharper-unity/pull/1416])
- Fix issues with ordering of multiplication of vector multiplication ([RIDER-33981](https://youtrack.jetbrains.com/issue/RIDER-33981), [RIDER-32798](https://youtrack.jetbrains.com/issue/RIDER-32798), [RIDER-32851](https://youtrack.jetbrains.com/issue/RIDER-32851), [#1168](https://github.com/JetBrains/resharper-unity/issues/1168), [#1428](https://github.com/JetBrains/resharper-unity/pull/1428))
- Rider: Fix race condition preventing "Attach to Unity Process" dialog from always listing players ([RIDER-34039](https://youtrack.jetbrains.com/issue/RIDER-34039), [#1298](https://github.com/JetBrains/resharper-unity/pull/1298))
- Rider: Prevent "Attach to Unity Process" attempting to attach to the same process multiple times ([#1129](https://github.com/JetBrains/resharper-unity/issues/1129), [#1298](https://github.com/JetBrains/resharper-unity/pull/1298))
- Rider: Fix show usages on Code Vision link for auto property event handlers ([#1142](https://github.com/JetBrains/resharper-unity/pull/1142))
- Rider: Work around Unity bug failing to send log events after leaving play mode ([#1414](https://github.com/JetBrains/resharper-unity/pull/1414))
- Unity Editor: Stop suggesting C# 8 features when using a new msbuild from Mono ([#1379](https://github.com/JetBrains/resharper-unity/issues/1379), [#1380](https://github.com/JetBrains/resharper-unity/pull/1380))
- Unity Editor: Avoid all initialisation when started in batch mode ([#1396](https://github.com/JetBrains/resharper-unity/pull/1396))
- Unity Editor: Fix exception calling `EditorApplication.isPlaying` on wrong thread ([#1308](https://github.com/JetBrains/resharper-unity/pull/1308))



## 2019.2.4
* Released: [2020-02-14](https://blog.jetbrains.com/dotnet/2020/01/14/resharper-ultimate-rider-2019-2-4/)
* [Commits](https://github.com/JetBrains/resharper-unity/compare/192-eap10-rtm-2019.2.3...192-eap11-rtm-2019.2.4)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/192-eap11-rtm-2019.2.4)
* Not released for ReSharper (by mistake).

### Fixed
- Fix exception parsing scene files ([DEXP-481931](https://youtrack.jetbrains.com/issue/DEXP-481931))
- Rider: Fix parsing preview version of Rider plugin package ([#1349](https://github.com/JetBrains/resharper-unity/pull/1349))



## 2019.2.3
* Released: [2019-10-18](https://blog.jetbrains.com/dotnet/2019/10/18/rider-2019-2-3-bugfix/)
* [Commits](https://github.com/JetBrains/resharper-unity/compare/192-eap9-rtm-2019.2.2...192-eap10-rtm-2019.2.3)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/31?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/192-eap10-rtm-2019.2.3)
* Not released for ReSharper (by mistake).

### Changed
- Use platform native line endings in generated `.meta` files. Works better with Perforce ([#1323](https://github.com/JetBrains/resharper-unity/pull/1323))

### Fixed
- Fix issues with completion of Unity event functions ([RIDER-33167](https://youtrack.jetbrains.com/issue/RIDER-33167), [#1326](https://github.com/JetBrains/resharper-unity/pull/1326))
- Fix exception building caches ([DEXP-481931](https://youtrack.jetbrains.com/issue/DEXP-481931), [#1355](https://github.com/JetBrains/resharper-unity/pull/1355))
- Rider: Fix missing "Install Mono" notification ([#1329](https://github.com/JetBrains/resharper-unity/pull/1329))



## 2019.2.2
* Released: [2019-08-29](https://blog.jetbrains.com/dotnet/2019/08/29/rider-2019-2-2/)
* [Commits](https://github.com/JetBrains/resharper-unity/compare/192-eap8-rtm-2019.2.1...192-eap9-rtm-2019.2.2)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/30?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/192-eap9-rtm-2019.2.2)
* Not released for ReSharper (by mistake).

### Added
- Rider: Suggest files and folders to be ignored by version control ([RIDER-31206](https://youtrack.jetbrains.com/issue/RIDER-31206), [#1276](https://github.com/JetBrains/resharper-unity/pull/1276))

### Changed
- Suppress warning when using `CollisionFlags` in a bitwise operation ([RIDER-28661](https://youtrack.jetbrains.com/issue/RIDER-28661), [#1289](https://github.com/JetBrains/resharper-unity/pull/1289))
- Rider: Show notification that Unity isn't running when clicking Code Vision to find Unity usages ([#1275](https://github.com/JetBrains/resharper-unity/pull/1275))
- Rider: Ignore Unity.Licensing.Client in list of Unity processes to debug ([#1283](https://github.com/JetBrains/resharper-unity/issues/1283), [#1284](https://github.com/JetBrains/resharper-unity/pull/1284))
- Rider: Files in read only packages no longer shown as "ignored" in Unity Explorer ([#1288](https://github.com/JetBrains/resharper-unity/pull/1288))
- Unity Editor: Improve performance on AppDomain reloads ([#1291](https://github.com/JetBrains/resharper-unity/pull/1291))

### Fixed
- Fix exception when pasting code ([RIDER-31338](https://youtrack.jetbrains.com/issue/RIDER-31338), [#1280](https://github.com/JetBrains/resharper-unity/pull/1280))
- Rider: Fix fetching list of Unity editors and players on UI thread ([RIDER-31585](https://youtrack.jetbrains.com/issue/RIDER-31585))
- Unity Editor: Fix exception running tests with older test framework packages ([#1273](https://github.com/JetBrains/resharper-unity/pull/1273))



## 2019.2.1
* Released: 2019-08-20
* [No code changes](https://github.com/JetBrains/resharper-unity/compare/192-eap7-rtm-2019.2.0...192-eap8-rtm-2019.2.1)
* [GitHub tag](https://github.com/JetBrains/resharper-unity/releases/tag/192-eap8-rtm-2019.2.1)



## 2019.2
* Released: [2019-08-08](https://blog.jetbrains.com/dotnet/2019/08/08/rider-2019-2-released/)
* Build: 2019.2.0.72
* [Commits](https://github.com/JetBrains/resharper-unity/compare/191-eap11-rtm-2019.1.3...192-eap7-rtm-2019.2.0)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/22?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/192-eap7-rtm-2019.2.0)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/2019.2.0.72)

### Added
- Add completion and inspections for scenes, tags, layers and inputs ([#1158](https://github.com/JetBrains/resharper-unity/pull/1158))
- Add quick fix which specifies scene name when several scenes with the same name are present in build settings ([#1158](https://github.com/JetBrains/resharper-unity/pull/1158))
- Add new file and live templates ([#1201](https://github.com/JetBrains/resharper-unity/pull/1201))
- Add context action to generate event function in any Unity type ([#1209](https://github.com/JetBrains/resharper-unity/pull/1209))
- Add context actions to add Inspector attributes `Space`, `Header` and `Tooltip` to serialised fields ([#1244](https://github.com/JetBrains/resharper-unity/pull/1244))
- Add context actions to initialise component field or property in `Start` or `Awake` ([#356](https://github.com/JetBrains/resharper-unity/356), [#608](https://github.com/JetBrains/resharper-unity/issues/608), [#1259](https://github.com/JetBrains/resharper-unity/pull/1259))
- Add context action to add `RequireComponent` attribute for component field ([#608](https://github.com/JetBrains/resharper-unity/issues/608), [#1259](https://github.com/JetBrains/resharper-unity/pull/1259))
- Add Find Unity Usages of Symbol to Navigate To menu ([#1209](https://github.com/JetBrains/resharper-unity/pull/1209))
- Add inspection for duplicate shortcut items in a menu attribute ([#1246](https://github.com/JetBrains/resharper-unity/pull/1246))
- Rider: Add Inspector values as part of serialised field Code Vision ([#1226](https://github.com/JetBrains/resharper-unity/pull/1226))
- Rider: Add quick fix to add or enable scenes to build settings ([#1158](https://github.com/JetBrains/resharper-unity/pull/1158))
- Rider: Add "Show in Unity" action to Unity YAML file notifications ([#1236](https://github.com/JetBrains/resharper-unity/pull/1236))
- Unity Editor: Correctly detect Rider installed via snap ([#1215](https://github.com/JetBrains/resharper-unity/pull/1215))

### Changed
- Improve performance of YAML based asset parsing ([#1226](https://github.com/JetBrains/resharper-unity/pull/1226), [RIDER-30186](https://youtrack.jetbrains.com/issue/RIDER-30186), [#1256](https://github.com/JetBrains/resharper-unity/pull/1256))
- Generate event function body according to settings ([#1236](https://github.com/JetBrains/resharper-unity/pull/1236))
- Classes implementing editor interfaces no longer marked as unused ([#686](https://github.com/JetBrains/resharper-unity/issues/686), [#1167](https://github.com/JetBrains/resharper-unity/pull/1167))
- Remove syntax errors in ShaderLab files from Solution Wide Error Analysis ([#1268](https://github.com/JetBrains/resharper-unity/pull/1268))
- Rider: Show asset usage count in Code Vision ([#1209](https://github.com/JetBrains/resharper-unity/pull/1209))
- Rider: Show event function method summary docs in Code Vision tooltip ([#1206](https://github.com/JetBrains/resharper-unity/pull/1206))
- Rider: Improve handling of `[UnityTest]` attribute ([#1224](https://github.com/JetBrains/resharper-unity/pull/1224))
- Rider: Notification about saving during play mode moved from startup to first modification in play mode ([#1263](https://github.com/JetBrains/resharper-unity/pull/1263))
- Unity Editor: Improve performance of editor plugin reload ([#1197](https://github.com/JetBrains/resharper-unity/issues/1197), [#1221](https://github.com/JetBrains/resharper-unity/pull/1221))

### Fixed
- Fix filtering of event function code completion in ReSharper ([#1245](https://github.com/JetBrains/resharper-unity/issues/1245), [DEXP-454736](https://youtrack.jetbrains.com/issue/DEXP-454736), [#1255](https://github.com/JetBrains/resharper-unity/pull/1255))
- Fix error parsing ShaderLab properties with empty parameter list ([#1267](https://github.com/JetBrains/resharper-unity/pull/1267))
- Rider: Disable automatic cleanup of Unity messages in Rider ([RIDER-26880](https://youtrack.jetbrains.com/issue/RIDER-26880), [#1217](https://github.com/JetBrains/resharper-unity/pull/1217))
- Rider: Fix presentation of unit tests with similar name ([#526](https://github.com/JetBrains/resharper-unity/issues/526), [#1214](https://github.com/JetBrains/resharper-unity/pull/1214))
- Rider: Fix exception checking process ID in Unity run configuration ([RIDER-28743](https://youtrack.jetbrains.com/issue/RIDER-28743), [#1223](https://github.com/JetBrains/resharper-unity/pull/1223))
- Rider: Fix focus issues when opening a file and Rider is minimised ([#1100](https://github.com/JetBrains/resharper-unity/issues/1100), [#1262](https://github.com/JetBrains/resharper-unity/pull/1262))
- Unity Editor: Fix generation of MDB files ([#1155](https://github.com/JetBrains/resharper-unity/issues/1155), [#1182](https://github.com/JetBrains/resharper-unity/pull/1182))
- Unity Editor: Fix double refresh of assets on save all ([#1253](https://github.com/JetBrains/resharper-unity/issues/1253), [#1254](https://github.com/JetBrains/resharper-unity/pull/1254))



## 2019.1.3
* Released: 2019-07-10
* [Commits](https://github.com/JetBrains/resharper-unity/compare/191-eap10-rtm-2019.1.2...191-eap11-rtm-2019.1.3)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/191-eap11-rtm-2019.1.3)
* No ReSharper release required.

### Fixed
- Rider: Ignore "unityhub" Ubuntu process in debug dialog ([#1210](https://github.com/JetBrains/resharper-unity/pull/1210))



## 2019.1.2
* Released: [2019-06-06](https://blog.jetbrains.com/dotnet/2019/06/06/rider-2019-1-2/)
* [Commits](https://github.com/JetBrains/resharper-unity/compare/191-eap9-rtm-2019.1.1...191-eap10-rtm-2019.1.2)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/28?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/191-eap10-rtm-2019.1.2)
* Not released for ReSharper (by mistake).

### Added
- Rider: Added support for the Rider integration package used by Unity 2019.2+. No longer copies Rider plugin to Assets folder, and is loaded directly from the Rider installation folder ([#1176](https://github.com/JetBrains/resharper-unity/pull/1176))

### Fixed
- Fix parse errors in YAML for strings that begin with quotes, braces or tildes ([#1169](https://github.com/JetBrains/resharper-unity/issues/1169), [RIDER-27475](https://youtrack.jetbrains.com/issue/RIDER-27475), [#1192](https://github.com/JetBrains/resharper-unity/pull/1192))
- Fix errors in scene files for unresolved methods ([RIDER-27445](https://youtrack.jetbrains.com/issue/RIDER-27445), [#1178](https://github.com/JetBrains/resharper-unity/1178), [#1174](https://github.com/JetBrains/resharper-unity/pull/1174))
- Fix rename of script components not being able to update correctly ([#1196](https://github.com/JetBrains/resharper-unity/pull/1196))
- Rider: Fix Code Vision usage counter not always correct for methods used in scene files ([RIDER-27684](https://youtrack.jetbrains.com/issue/RIDER-27684), [#1178](https://github.com/JetBrains/resharper-unity/1178), [#1174](https://github.com/JetBrains/resharper-unity/pull/1174))
- Rider: Fix high CPU usage on Linux ([#1163](https://github.com/JetBrains/resharper-unity/issues/1163), [#1171](https://github.com/JetBrains/resharper-unity/1171))
- Rider: Fix issue with switching to play mode when debugging ([RIDER-26857](https://youtrack.jetbrains.com/issue/RIDER-26857), [#1202](https://github.com/JetBrains/resharper-unity/pull/1202))
- Rider: Fix Code Vision flickering when typing inside method ([#1203](https://github.com/JetBrains/resharper-unity/pull/1203))



## 2019.1.1
* Released: 2019-05-02
* [No code changes](https://github.com/JetBrains/resharper-unity/compare/191-eap8-rtm-2019.1.0...191-eap9-rtm-2019.1.1)
* [GitHub tag](https://github.com/JetBrains/resharper-unity/releases/tag/191-eap9-rtm-2019.1.1)



## 2019.1
* Released: [2019-04-30](https://blog.jetbrains.com/dotnet/2019/04/30/rider-2019-1-arrived/)
* [Commits](https://github.com/JetBrains/resharper-unity/compare/183-eap13-rtm...191-eap8-rtm-2019.1.0)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/22?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/191-eap8-rtm-2019.1.0)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/2019.1.0.76)

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
- Rider: Add all package folders to find in files index ([#1120](https://github.com/JetBrains/resharper-unity/pull/1120))
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
For Rider 2018.3.3. No release necessary for ReSharper
* Released: 2019-01-11
* [Commits](https://github.com/JetBrains/resharper-unity/compare/183-eap12-rtm...183-eap13-rtm)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/183-eap13-rtm)

### Fixed
- Unity Editor: Fix finding install path from JetBrains Toolbox ([RIDER-24173](https://youtrack.jetbrains.com/issue/RIDER-24173), [#1024](https://github.com/JetBrains/resharper-unity/pull/1024))



## 2018.3.2
* Released: [2019-01-30](https://blog.jetbrains.com/dotnet/2019/01/30/rider-2018-3-2-bug-fix-update-release/)
* [Commits](https://github.com/JetBrains/resharper-unity/compare/183-eap11-rtm...183-eap12-rtm)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/26?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/183-eap12-rtm)
* [ReSharper release (2019-02-05](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/2018.3.0.1103)

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



## 2018.3 for ReSharper
For ReSharper 2018.3 (Based on work in progress 2018.3.2 for Rider)

* Released: 2019-01-17
* [Commits](https://github.com/JetBrains/resharper-unity/compare/182-eap12-2018.2.3...183-eap11-rtm-resharper)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/26?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/183-eap11-rtm-resharper)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/2018.3.0.1092)

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



## 2018.3.1
* Released: [2018-12-26](https://blog.jetbrains.com/dotnet/2018/12/27/rider-2018-3-1-bug-fix-update/)
* [Commits](https://github.com/JetBrains/resharper-unity/compare/183-eap10-rtm...183-eap11-rtm)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/23?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/183-eap11-rtm)
* ReSharper release delayed until 2013-01-17

### Added
- Automatically disable YAML parsing if the project is too large (#973)

### Fixed
- Rider: Fix reference to `UnityEditor.iOS.Extensions.Xcode.dll` in generated projects (#974, #976)
- Rider: Fix bug in setting reference to `UnityEditor.iOS.Extensions.Xcode.dll` in generated projects (#976)
- Rider: Fix bug failing to copy script assemblies during debugging (#964)



## 2018.3
* Released: [2018-12-17](https://blog.jetbrains.com/dotnet/2018/12/18/rider-2018-3-released/)
* [Commits](https://github.com/JetBrains/resharper-unity/compare/182-eap12-2018.2.3...183-eap10-rtm)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/19?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/183-eap10-rtm)
* ReSharper release delayed until 2013-01-17

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



## 2018.2.3
* Released: [2018-09-13](https://blog.jetbrains.com/dotnet/2018/09/13/resharper-ultimate-2018-2-3-rider-2018-2-3-released/)
* [Commits](https://github.com/JetBrains/resharper-unity/compare/182-eap11-2018.2.2...182-eap12-2018.2.3)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/24?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/182-eap12-2018.2.3)
* No ReSharper release required.

### Added
- Unity editor: Disable plugin when Unity is in batch mode (#776, [RIDER-19688](https://youtrack.jetbrains.com/issue/RIDER-19688))



## 2018.2.2
* Released: [2018-09-11](https://blog.jetbrains.com/dotnet/2018/09/11/rider-2018-2-2-released/)
* [Commits](https://github.com/JetBrains/resharper-unity/compare/182-eap10-2018.2.1...182-eap11-2018.2.2)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/21?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/182-eap11-2018.2.2)
* Not released for ReSharper (by mistake).

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



## 2018.2 for ReSharper
For ReSharper 2018.2 (Based on Rider 2018.2.1. Release notes include everything in Rider 2018.2 and Rider 2018.2.1)

* Released: 2018-09-02
* [Commits](https://github.com/JetBrains/resharper-unity/compare/182-eap9-rtm…182-eap9-rtm-resharper)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/16?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/182-eap9-rtm-resharper)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/2018.2.0.653)

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

### Changed
- Updated to ReSharper 2018.2
- Improve performance of code completion for event functions (#471)
- Update API details to 2018.2.0b9 (#611, #613)
- Consolidate multiple incorrect method signature inspections into one, with quick fix (#534)
- Rework make serialised/non-serialised field context actions (#583, #586)
- Serialised field Context Action and Quick Fixes work correctly with multiple field declarations (#586)
- Don't show incorrect "always false" warning for "this == null" in Unity types (#368)
- Remove highlighted background for Cg blocks in ShaderLab files ([RIDER-16438](https://youtrack.jetbrains.com/issue/RIDER-16438))

### Fixed
- Fix ShaderLab highlighting of keywords ([RIDER-17287](https://youtrack.jetbrains.com/issue/RIDER-17287))
- Fix rename's "find in text" renaming non-text elements in ShaderLab files
- Fix Unity specific inspections not showing in Solution Wide Errors tool window (#680)



## 2018.2.1
* Released: [2018-08-30](https://blog.jetbrains.com/dotnet/2018/08/30/rider-2018-2-1-released/)
* [Commits](https://github.com/JetBrains/resharper-unity/compare/182-eap9-rtm...182-eap10-2018.2.1)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/20?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/182-eap10-2018.2.1)
* ReSharper release delayed until 2018-09-02

### Fixed
- Unity editor: Fix project failing to load due to Unicode issue (#727, #732)
- Unity editor: Fix response file defines and references not being applied to generated project files (#729, #735)



## 2018.2
* Released: [2018-08-23](https://blog.jetbrains.com/dotnet/2018/08/23/rider-2018-2-released/)
* [Commits](https://github.com/JetBrains/resharper-unity/compare/wave12-eap15-2018.1.4-rtm...182-eap9-rtm)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/16?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/182-eap9-rtm)
* ReSharper release delayed until 2018-09-02

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



## 2018.1.4
* Released: 2018-08-02
* [No code changes](https://github.com/JetBrains/resharper-unity/compare/wave12-eap14-2018.1.3-rtm...wave12-eap15-2018.1.4-rtm)
* [GitHub tag](https://github.com/JetBrains/resharper-unity/releases/tag/wave12-eap15-2018.1.4-rtm)



## 2018.1.3
* Released: 2018-07-05
* [No code changes](https://github.com/JetBrains/resharper-unity/compare/wave12-2018.1.2-rtm...wave12-eap14-2018.1.3-rtm)
* [GitHub tag](https://github.com/JetBrains/resharper-unity/releases/tag/wave12-eap14-2018.1.3-rtm)



## 2018.1.2
* Released: 2018-05-28
* [Commits](https://github.com/JetBrains/resharper-unity/compare/wave12-2018.1.1-rtm…wave12-2018.1.2-rtm)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/wave12-eap13-2018.1.2-rtm)
* No ReSharper release required.

### Fixed
- Unity editor: Don't fail to parse version numbers with no minor part
- Unity editor: Fix Rider's BringToFront after opening script from Unity
- Unity editor: Display link to script to force project generation processor order (#528)



## 2018.1.1
* Released: [2018-05-25](https://blog.jetbrains.com/dotnet/2018/05/25/rider-2018-1-1-released/)
* [Commits](https://github.com/JetBrains/resharper-unity/compare/wave12-eap9-rtm...wave12-2018.1.1-rtm)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/17?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/wave12-eap12-2018.1.1-rtm)
* Not released for ReSharper (by mistake).

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



## 2018.1.0.380 for ReSharper
Compatibility fix due to breaking change in ReSharper 2018.1.2. Release is compatible with 2018.1 and 2018.1.2.

* Released: 2018-06-16
* [Commits](https://github.com/JetBrains/resharper-unity/compare/wave12-eap9-rtm...wave12-2018.1-api-fix)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/wave12-2018.1-api-fix)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/2018.1.0.380)

### Fixed
- Fix a breaking API change in ReSharper 2018.1.2 (#584)



## 2018.1
Rider and ReSharper version numbers are synced with this release.

* Released: [2018-04-18](https://blog.jetbrains.com/dotnet/2018/04/19/rider-2018-1-released/)
* [Commits](https://github.com/JetBrains/resharper-unity/compare/wave11-rider-2017.3.2...wave12-eap9-rtm)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/14?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/wave12-eap9-rtm)
* ReSharper release unlisted due to breaking API change in ReSharper 2018.1.2

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



## 2017.3.2
* [No code changes](https://github.com/JetBrains/resharper-unity/compare/wave11-rider-2017.3.1...wave11-rider-2017.3.2)
* [GitHub tag](https://github.com/JetBrains/resharper-unity/releases/tag/wave11-rider-2017.3.2)



## 2017.3.1
* Released: [2018-02-06](https://blog.jetbrains.com/dotnet/2018/02/06/rider-2017-3-1-released/)
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v2.1.3-rider...wave11-rider-2017.3.1)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/15?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/wave11-rider-2017.3.1)
* No ReSharper release required

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



## 2017.3
Bundled with Rider 2017.3. Released as 2.1.3 for ReSharper.

* Released: 2017-12-22
* [Commits](https://github.com/JetBrains/resharper-unity/compare/a14a3d50fa72b6c37a05184af56c6fefcb772f98...v2.1.3-resharper) (SHA is equivalent to `2.1.2.1739` on `master`)
* [Commits (rider changes)](https://github.com/JetBrains/resharper-unity/compare/v2.1.3-resharper...v2.1.3-rider)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/13?closed=1)
* [GitHub release (resharper)](https://github.com/JetBrains/resharper-unity/releases/tag/v2.1.3-resharper)
* [GitHub release (rider)](https://github.com/JetBrains/resharper-unity/releases/tag/v2.1.3-rider)
* [ReSharper release (2.1.3)](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/2.1.3.4208)

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



## 2.1.2.1739
For Rider 2017.2.1

* Released: 2017-11-15
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v2.1.2...2.1.2.1739)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/2.1.2.1739)
* No ReSharper release required

### Added
- Rider: Add syntax highlighting for `.compute` files ([RIDER-11221](https://youtrack.jetbrains.com/issue/RIDER-11221))

### Changed
- Rider: Improve reliability of attaching debugger to Unity Editor (#262, #268)



## 2.1.2 for ReSharper 2017.2
* Released: 2017-10-09
* Build: 2.1.2.1505
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v2.1.1...v2.1.2-resharper)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/12?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v2.1.2-resharper)
* [ReSharper release (2019-10-17)](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/2.1.2.1505)

### Added
- Support Unity API up to 2017.3.0b3 (#218)
- Add Unity specific file templates (#232, #237)
- Recognise projects with modularised UnityEngine assembly references (#241)
- Add colour highlighting and editing to ShaderLab
- Add icons for ShaderLab files and run configurations
- Show event function descriptions in generate dialog (#225, [RIDER-4904](https://youtrack.jetbrains.com/issue/RIDER-4904))
- Add annotations for modularised UnityEngine assemblies (#207)

### Changed
- Updated to ReSharper 2017.2
- Improve parsing of Cg files (#243)
- Improve ShaderLab parsing (#228, #233, [RIDER-9214](https://youtrack.jetbrains.com/issue/RIDER-9214), #222)

### Fixed
- Fix code completion and generation not working with newer versions of Unity (#219, #245)
- Fix parsing of 2DArray in ShaderLab files ([RIDER-9786](https://youtrack.jetbrains.com/issue/RIDER-9786))
* Fix parsing errors in ShaderLab files ([RIDER-8917](https://youtrack.jetbrains.com/issue/RIDER-8917), [RIDER-8914](https://youtrack.jetbrains.com/issue/RIDER-8914))



## 2.1.2 for Rider 2017.2
* Released: 2017-10-09
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v2.1.1...v2.1.2)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/12?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v2.1.2)

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



## 2.1.1
For Rider 2017.2 EAP2. Not released for ReSharper

* Released: 2017-09-16
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v2.1.0...v2.1.1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v2.1.1)
* No ReSharper release

### Added
- Show event function descriptions in generate dialog (#225, [RIDER-4904](https://youtrack.jetbrains.com/issue/RIDER-4904))
- Unity editor: Add support for `mcs.rsp` (#230)

### Changed
- Improve ShaderLab parsing (#228, #233, [RIDER-9214](https://youtrack.jetbrains.com/issue/RIDER-9214), #222)



## 2.1.0
For Rider 2017.2 EAP1. Not released for ReSharper

* Released: 2017-09-04 (approximately)
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v2.0.4...v2.1.0) (Due to branching strategy, this list contains commits from previous releases)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v2.1.0)
* No ReSharper release

### Added
- Add annotations for modularised UnityEngine assemblies (#207)



## 2.0.4
For Rider 2017.1.2 (RD-171.4456.3568). Not released for ReSharper

* Released: 2017-09-04
* Build: 2.0.4.2575
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v2.0.3...v2.0.4)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v2.0.4)
* No ReSharper release

### Changed
* Rider: Change completion in shader files to be semi-focussed

### Fixed
* Fix parsing errors in ShaderLab files ([RIDER-8917](https://youtrack.jetbrains.com/issue/RIDER-8917), [RIDER-8914](https://youtrack.jetbrains.com/issue/RIDER-8914))



## 2.0.3-resharper
For ReSharper 2017.2

* Released: 2017-08-31
* Build: 2.0.3.314
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v2.0.0-resharper...v2.0.3-resharper)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v2.0.3-resharper)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/2.0.3.314)

### Changed
- Updated to ReSharper 2017.2 (#193)

### Fixed
- Parse pre-processor directives in ShaderLab (#186)
- Correctly handle property attributes in shader file (#187)
- Parse CGINCLUDE blocks at any point in shader file (#188, #189, #206)
- Parse property reference for BlendOp ([RIDER-8386](https://youtrack.jetbrains.com/issue/RIDER-8386))



## 2.0.3 - 2017-08-31
For Rider 2017.1.1 (RD-171.4456.2813)

* Released: 2017-08-31
* Build: 2.0.3.2540
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v2.0.2...a89dce7c8ba66cd8d6d86bb3dd1c7a82544fe21f) (SHA is equivalent to `v2.0.3`. Possibly tagged wrong branch?)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v2.0.3)

### Fixed
- Parse pre-processor directives in ShaderLab (#186)
- Correctly handle property attributes in shader file (#187)
- Parse CGINCLUDE blocks at any point in shader file (#188, #189, #206)
- Parse property reference for BlendOp ([RIDER-8386](https://youtrack.jetbrains.com/issue/RIDER-8386))



## 2.0.0 for ReSharper
For ReSharper 2017.1 (Based on work in progress 2.0.3 for Rider 2017.1)

* Released: 2017-08-29
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v2.0.0...v2.0.2-resharper)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v2.0.0-resharper)
* [ReSharper release (2017-08-31)](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/2.0.0)

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



## 2.0.2
For Rider 2017.1 RTM. Not released for ReSharper

* Released: 2017-08-03 (ReSharper release delayed until 2017-08-29)
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v2.0.0...v2.0.2)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v2.0.2)

### Added
- Add ability to disable advanced ShaderLab syntax (#183)
- Add support for `HLSL` and `GLSL` blocks in ShaderLab
- Rider: Make sure _Attach to Unity_ run configuration is selected



## 2.0.0
For Rider 2017.1 RC. Not released for ReSharper

* Released: 2017-07-14 (ReSharper release delayed until 2017-08-29)
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



## 1.9.2
For Rider 2017.1 EAP23. Not released for ReSharper

* Released: 2017-08-15
* Not tagged. Don't know commit, so might not even have been released. This is based on Milestone. Might actually be merged in to 2.0.0
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/10?closed=1)
* ReSharper release not required

### Changed
- Rider: Install the Unity3DRider plugin even if Unity references are unresolved (#160, #174)
- Rider: Find Unity3DRider plugin even if install location is moved (#169, #172)



## 1.9.1
For ReSharper 2017.1 and Rider 2017.1 EAP23

* Released: 2017-06-29
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.9.0...v1.9.1)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/9?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.9.1)
* [ReSharper release (2017-07-21)](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/1.9.1)

### Added
- Rider: Display notification if auto-save is enabled with a quick link to disable. This can affect running games in the Unity editor

### Changed
- Improve performance by logging and change tracking for non Unity projects
- Rider: Update to EAP23
- Rider: Add setting to disable install of Unity3dRider plugin (#159)
- Unity editor: Minor improvements to logging, such as configurable log level



## 1.9.0
For ReSharper 2017.1 and Rider 2017.1 EAP22

* Released: 2017-06-15
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



## 1.8.0
For ReSharper 2017.1 and Rider 2017.1 EAP22

* Released: 2017-05-18
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



## 1.7.0
For ReSharper 2017.1 (and Rider EAP 20 or 21)

* Released: 2017-04-05
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.6.2...v1.7.0)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/7?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.7.0)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/1.7.0)

### Added
- Treats `Assertion.Assert` as assertion methods (#129)

### Changed
- Updated to ReSharper 2017.1 (#110)

### Fixed
- Fix incorrect signatures in known API (#128)



## 1.6.2
For ReSharper 2016.3 and Rider EAP19

* Released: 2017-03-22
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.6.1...v1.6.2)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.6.2)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/1.6.2)

### Changed
- Updated to Rider EAP19
- Improve location of "Create serialized field" Quick Fix (#124)



## 1.6.1
For ReSharper 2016.3 and Rider EAP18

* Released: 2017-03-08
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.6.0...v1.6.1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.6.1)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/1.6.1)

### Fixed
- Fix nasty bug that will delete and recreate all `.meta` files when reloading projects. Sorry! (#118)



## 1.6.0
For ReSharper 2016.3 and Rider EAP18

* Released: 2017-03-01
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.5.1-rider...v1.6.0)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/6?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.6.0)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/1.6.0)

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



## 1.5.1-rider
For Rider EAP17

* Released: 2017-02-17
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.5.0-rider...v1.5.1-rider)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.5.1-rider)

### Changed
- Updated to Rider EAP17



## 1.5.0-rider
Initial release for Rider

* Released: 2017-01-02
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.5.0...v1.5.0-rider)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.5.0-rider)

### Added
- Added support for Rider EAP15



## 1.5.0
For ReSharper 2016.3

* Released: 2016-12-30
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.4.0...v1.5.0)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/4?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.5.0)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/1.5.0)

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



## 1.4.0
For ReSharper 2016.2

* Released: 2016-11-18
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.3.0...v1.4.0)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/3?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.4.0)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/1.4.0)

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



## 1.3.0
For ReSharper 2016.2

* Released: 2016-06-26
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.2.1...v1.3.0)
* [Milestone](https://github.com/JetBrains/resharper-unity/milestone/2?closed=1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.3.0)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/1.3.0)

### Added
- Expanded generate code to all Unity class messages, not just `MonoBehaviour` (#20, #29, #44)
- External annotations to improve ReSharper's analysis. E.g. implicit usage and nullability of `Component.gameObject` (#34, #13, #15, #23, #42, #43)
- Code completion, find usages and rename support for `Invoke`, `InvokeRepeating` and `CancelInvoke` (#41)
- Auto-suggest message handler completion when creating methods
- Message handler descriptions for methods and parameters displayed in tooltips and QuickDoc
- "Read more" in QuickDoc navigates to Unity API documentation

### Changed
- Updated to ReSharper 2016.2 (#44, #46)



## 1.2.1
For ReSharper 2016.1

* Released: 2016-04-16
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.2.0...v1.2.1)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.2.1)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/1.2.1)

### Changed
- Updated to ReSharper 2016.1



## 1.2.0
For ReSharper 10

* Released: 2015-11-16
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.1.2...v1.2.0)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.2.0)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/1.2.0)

### Added
- Suppress naming consistency warnings on message handlers
- Add parameters to generated message handlers (#8)
- Automatically set language level to C# 5 (#5)
- ReSharper no longer suggests `Assets` or `Scripts` when checking namespaces



## 1.1.2
For ReSharper 10

* Released: 2015-11-06
* [Commits](https://github.com/JetBrains/resharper-unity/compare/v1.0.0...v1.1.2)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.1.2)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/1.1.2)

### Changed
- Updated to ReSharper 10



## 1.0.0
For ReSharper 9.2. Initial release

* Released: 2015-10-16
* [Commits](https://github.com/JetBrains/resharper-unity/compare/81b6bc5...v1.0.0)
* [GitHub release](https://github.com/JetBrains/resharper-unity/releases/tag/v1.0.0)
* [ReSharper release](https://resharper-plugins.jetbrains.com/packages/JetBrains.Unity/1.0.0)

### Added
- Marks `MonoBehaviour` classes, fields and methods as in use
- Adds _Generate Code_ provider for `MonoBehaviour` message handlers

