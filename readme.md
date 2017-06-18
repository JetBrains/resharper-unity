# Unity Support for ReSharper and Rider

The "Unity Support" plugin adds [Unity](http://unity3d.com/) specific functionality to [ReSharper](https://www.jetbrains.com/resharper/) and [Rider](https://www.jetbrains.com/rider/).

Rider is JetBrains' cross platform .NET IDE, based on ReSharper and the IntelliJ Platform. It can be used on Windows, Mac and Linux and together with the [Unity3dRider](https://github.com/JetBrains/resharper-unity/tree/master/resharper/src/resharper-unity/Unity3dRider) Unity plugin, can replace the default MonoDevelop editor with an IDE providing rich code navigation, inspections and refactorings.

## Features

The plugin adds knowledge of Unity based classes to ReSharper/Rider's analysis:

* The plugin knows about all Unity based classes (`MonoBehaviour`, `ScriptableObject`, `EditorWindow`, etc.) and their event functions via analysis of the Unity API surface and documentation.
* Support for Unity API versions 5.0 - 5.6, as well as 2017.1.

Event functions:

* [Event functions](https://docs.unity3d.com/Manual/EventFunctions.html) and fields implicitly used by Unity are marked with an icon in the gutter.
* Empty event functions are marked as dead code, with a Quick Fix to remove the method.
* When [Solution Wide Analysis](https://www.jetbrains.com/help/resharper/2016.2/Code_Analysis__Solution-Wide_Analysis.html) is enabled, implicitly used fields and event functions are marked as in use. Fields are highlighted if they aren't accessed in your code.

  <img src="docs/field_not_accessed.png" width="442">

* A new "Generate Unity event function" menu item is added to the <kbd>Alt</kbd>+<kbd>Insert</kbd> Generate Code menu, to generate event functions via GUI. This action is also available from <kbd>Alt</kbd>+<kbd>Enter</kbd> on a Unity based class's name.

    <img src="docs/generate_menu.png" width="156">

    <img src="docs/generate_dialog.png" width="442">

* Auto complete will suggest event function names when declaring methods in Unity based classes, and expand to include method signature. Simply start typing an event function within a class deriving from a known Unity class, such as `MonoBehaviour`.

  <img src="docs/auto_complete_message.png" width="392">

* Incorrect method signatures and return types are shown as warnings, with a Quick Fix to create the correct signature.
* Optional parameters are called out in a tooltip, and marked as unused if not used in the body of the method, e.g. `OnCollisionEnter(Collision collision)`.
* Suppress naming consistency warnings for known Unity event functions. E.g. ReSharper no longer suggests that `AnimatorIK` be renamed to `AnimatorIk`.
* Descriptions for event functions and parameters in Unity based classes are shown in tooltips and [QuickDoc](https://www.jetbrains.com/help/resharper/2016.2/Coding_Assistance__Quick_Documentation.html). To show the information in tooltips, ReSharper's "Colour identifiers" and "Replace Visual Studio tooltips" setting must be enabled (search for them in settings). Alternatively, use the excellent [Enhanced Tooltip](https://github.com/MrJul/ReSharper.EnhancedTooltip#readme) plugin.

  <img src="docs/quickdoc.png" width="500">

* "Read more" in [QuickDoc](https://www.jetbrains.com/help/resharper/2016.2/Coding_Assistance__Quick_Documentation.html) will navigate to the Unity API documentation, locally if available, or via the Unity website.

Coroutines and invokable methods:

* Event functions that can be coroutines are called out in tooltips.
* Context Action on methods that can be coroutines to convert method signature to/from coroutine.
* Warnings for unused coroutine return values.
* Code completion, find usages and rename support for string literals in `MonoBehaviour.Invoke`, `IsInvoking`, `InvokeRepeating` and `CancelInvoke`. Also supports `StartCoroutine` and `StopCoroutine`.

  <img src="docs/invoke_completion.png" width="209">

Networking:

* Code completion, find usages and rename support for string literals in `[SyncVar(hook = "OnValueChanged")]`.
* Highlight usage of `SyncVarAttribute` in any class other than `NetworkBehaviour` as an error.

Inspections and Quick Fixes:

* Empty event functions are shown as dead code, with a quick fix to remove the method.
* Using the `SyncVarAttribute` inside any class other than `NetworkBehaviour` is treated as an error.
* Inspection and Quick Fix to use `CompareTag` instead of string comparison.

  <img src="docs/compare_tag.gif" width="509">

* "Create serialized field" from usage of unresolved symbol.

  <img src="docs/create_serialized_field_from_usage.png" width="358">

* Inspections and Quick Fixes for incorrect event function signatures and return types.

  <img src="docs/incorrect_signature.png" width="627">

* Inspections and Quick Fixes for incorrect method or static constructor signatures for `InitializeOnLoad` attributes.
* Inspections for incorrectly calling `new` on a `MonoBehaviour` or `ScriptableObject`. Quick Fixes will convert to calls to `GameObject.AddComponent<T>()` and `ScriptableObject.CreateInstance<T>()`.
* Inspection for unused coroutine return value.

[External Annotations](https://www.jetbrains.com/help/resharper/2016.2/Code_Analysis__External_Annotations.html):

* Treat code marked with attributes from UnityEngine.dll, UnityEngine.Networking.dll and UnityEditor.dll as implicitly used.
* Mark `Component.gameObject` and `Object.name` as not-nullable.
* `Debug.Assert` marked as assertion method to help null-value analysis (e.g. "value cannot be null" after `Debug.Assert(x != null)`)
* `Debug.AssertFormat`, `LogFormat`, etc. gets string formatting helper functionality.
* `Assertions.Assert` methods marked as assertion methods to help null-value analysis.
* `EditorTestsWithLogParser.ExpectLogLineRegex` gets regular expression helper functionality.
* Various attributes now require the class they are applied to derive from a specific base type. E.g. `[CustomEditor]` requires a base class of `Editor`).

Other:

* Synchronise .meta files on creation, deletion, rename and refactoring.
* Automatically sets correct C# language version, if not already specified in .csproj - ReSharper will no longer suggest code fixes that won't compile! Supports the default C# 4 compiler, Unity 5.5's optional C# 6 compiler and the C# 6/7.0 compiler in the [CSharp60Support](https://bitbucket.org/alexzzzz/unity-c-5.0-and-6.0-integration/src) plugin.
* Disables the `Assets` and `Assets\Scripts` folders from being considered as ["namespace providers"](https://www.jetbrains.com/help/resharper/2016.2/CheckNamespace.html). This means ReSharper will no longer suggest to include `Assets` or `Scripts` in the namespace of your code.
* Support for `UnityEngine.Color` and `UnityEngine.Color32`. The colour is highlighted, and hitting <kbd>Alt</kbd>+<kbd>Enter</kbd> will open the colour palette editor to modify the colour. Also supports named colours and `Color.HSVToRGB`.

  <img src="docs/colours.png" width="425">

### Rider specific functionality

The plugin also adds some functionality just for Rider:

* The `Library` and `Temp` folders are automatically excluded from Rider's full text search, used for the "Find in Path" feature. These folders can become very large, and can take a long time to index if not excluded.
* Rider will automatically create an "Attach to Unity Editor" run configuration. When the debug button is clicked, Rider will automatically attach to the editor and start debugging. Rider will look for a `Library/EditorInstance.json` file, created by Unity 2017.1, or by the latest versions of the [Unity3dRider plugin](https://github.com/JetBrains/Unity3dRider#readme). If the file doesn't exist and only a single instance of Unity is running, Rider will attach to this instance. If multiple instances are running, Rider will prompt for which instance to attach to.

  <img src="docs/attach_to_editor_run_config.png" width="514">

Please [suggest new features in the issues](https://github.com/JetBrains/resharper-unity/issues)!

## Installing

To install into ReSharper:

* Use ReSharper's Extension Manager (ReSharper &rarr; Extension Manager), search for "Unity" and install. Restart, and it'll just start working.

To install into Rider:

* Install from the "featured plugins" page of the welcome screen.
* Or, go to the Plugins page in Preferences, click Install JetBrains Plugin and search for "Unity". Rider will need to be restarted.

Please watch the repo for updates, or follow [@citizenmatt](https://twitter.com/citizenmatt), [@resharper](https://twitter.com/resharper) or [@JetBrainsRider](https://twitter.com/JetBrainsRider) on twitter for updates.

## Roadmap

Check the [milestones](https://github.com/JetBrains/resharper-unity/milestones) for plans, and please [raise an issue](https://github.com/JetBrains/resharper-unity/issues) with feature requests or bugs.
