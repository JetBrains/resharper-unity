# Unity Support for ReSharper and Rider

This plugin adds [Unity](http://unity3d.com/) specific functionality to [ReSharper](https://www.jetbrains.com/resharper/) and [Rider](https://www.jetbrains.com/rider/).

Rider is JetBrains' cross platform .NET IDE, which uses ReSharper out of process to provide language support - which is why it can run ReSharper plugins. See below for installation details.

Please see the [Unity3dRider](https://www.github.com/JetBrains/Unity3dRider#readme) plugin to add support to the Unity editor to open C# projects, files and error messages in Rider.

## Features

* [Event functions](https://docs.unity3d.com/Manual/EventFunctions.html) and fields implicitly used by Unity are marked with an icon in the gutter.
* When [Solution Wide Analysis](https://www.jetbrains.com/help/resharper/2016.2/Code_Analysis__Solution-Wide_Analysis.html) is enabled, implicitly used fields and event functions are marked as in use. Fields are highlighted if they aren't accessed in your code.

  <img src="docs/field_not_accessed.png" width="442">

* The plugin knows about all Unity based classes (`MonoBehaviour`, `ScriptableObject`, `EditorWindow`, etc.) and their event functions via analysis of the Unity API surface and documentation.
* Support for Unity API versions 5.2 - 5.5.
* <kbd>Alt</kbd>+<kbd>Insert</kbd> on Unity based classes to generate event functions methods via GUI.

    <img src="docs/generate_menu.png" width="156">

    <img src="docs/generate_dialog.png" width="442">

* Generate also available from <kbd>Alt</kbd>+<kbd>Enter</kbd> context menu on Unity based classes.
* Auto complete will suggest event function names when declaring methods in Unity based classes, and expand to include method signature. Simply start typing an event function within a class deriving from a known Unity class, such as `MonoBehaviour`.

  <img src="docs/auto_complete_message.png" width="392">

* Descriptions for event functions and parameters in Unity based classes are shown in tooltips and [QuickDoc](https://www.jetbrains.com/help/resharper/2016.2/Coding_Assistance__Quick_Documentation.html). To show the information in tooltips, ReSharper's "Colour identifiers" and "Replace Visual Studio tooltips" setting must be enabled (search for them in settings). Alternatively, use the excellent [Enhanced Tooltip](https://github.com/MrJul/ReSharper.EnhancedTooltip#readme) plugin.

  <img src="docs/quickdoc.png" width="500">

* "Read more" in [QuickDoc](https://www.jetbrains.com/help/resharper/2016.2/Coding_Assistance__Quick_Documentation.html) will navigate to the Unity API documentation, locally if available, or via the Unity website.
* Code completion, find usages and rename support for string literals in `MonoBehaviour.Invoke`, `IsInvoking`, `InvokeRepeating` and `CancelInvoke`. Also supports `StartCoroutine` and `StopCoroutine`.

  <img src="docs/invoke_completion.png" width="209">

* Inspection and Quick Fix to use `CompareTag` instead of string comparisons.

  <img src="docs/compare_tag.gif" width="509">

* Suppress naming consistency warnings for known Unity event functions. E.g. ReSharper no longer suggests that `AnimatorIK` be renamed to `AnimatorIk`.
* Disables the `Assets` and `Assets\Scripts` folders from being considered as ["namespace providers"](https://www.jetbrains.com/help/resharper/2016.2/CheckNamespace.html). This means ReSharper will no longer suggest to include `Assets` or `Scripts` in the namespace of your code.
* Automatically sets correct C# language version, if not already specified in .csproj - ReSharper will no longer suggest code fixes that won't compile! Supports the default C# 4 compiler, Unity 5.5's optional C# 6 compiler and the C# 6/7.0 compiler in the [CSharp60Support](https://bitbucket.org/alexzzzz/unity-c-5.0-and-6.0-integration/src) plugin.
* Support for `UnityEngine.Color` and `UnityEngine.Color32`. The colour is highlighted, and hitting <kbd>Alt</kbd>+<kbd>Enter</kbd> will open the colour palette editor to modify the colour. Also supports named colours and `Color.HSVToRGB`.

  <img src="docs/colours.png" width="425">

* [External annotations](https://www.jetbrains.com/help/resharper/2016.2/Code_Analysis__External_Annotations.html) to mark items as implicitly used, assertion methods and also provide string formatting assistance for logging methods.

Please feel free to [suggest new features in the issues](https://github.com/JetBrains/resharper-unity/issues)!

## Installing

To install into ReSharper:
* Use ReSharper's Extension Manager (ReSharper &rarr; Extension Manager), search for "Unity" and install. Restart, and it'll just start working.

To install into Rider:
* Install from the "featured plugins" page of the welcome screen.
* Or, go to the Plugins settings page and install from there.

Please watch the repo or follow [@citizenmatt](https://twitter.com/citizenmatt) and [@slavikt](https://twitter.com/slavikt) on twitter for updates.

## Roadmap

There is no roadmap as such. I am not a Unity developer, so do not know what the common pain points are. If you'd like to suggest a feature, please [raise an issue](https://github.com/JetBrains/resharper-unity/issues).
