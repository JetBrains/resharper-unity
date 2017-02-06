# Unity3dRider

Rider support for Unity:
* Debugging of Unity instances.
    * Use the Run &rarr; "Attach to local process" menu item to list available Unity instances.
    * Run &rarr; "Edit Configurations" &rarr; Add new "Mono remote" configuration to set everything manually.

[From EAP13 - 163.7608] Additional Rider support for Unity via [ReSharper plugin](https://github.com/JetBrains/resharper-unity#readme) (File&rarr;Settings&rarr;Plugins&rarr;Type Unity&rarr;Press "search in repositories"&rarr;ReSharper Unity plugin will be found&rarr;Install.&rarr;Restart Rider.). 

However, Unity does not currently support Rider. This plugin adds that support:

* An "Open C# Project in Rider" item to the Assets menu.
    * With help of [RiderUnity3dConnector](https://github.com/PotterDai/RiderUnity3DConnector/releases) open file is fast and Rider is always focused after opening file. It needs to be installed manually in Rider via Settings->Plugins->Install plugin from disk.
* Double click a C# script file or error message in Unity will open Rider and navigate to the corresponding file and line.
* Runs on Windows, Linux and macOS.
* Sets language level when generating project files
    * Sets language level to C# 4, so that Rider does not suggest C# 6 language features.
    * Sets language level to C# 6 when targeting .NET 4.6 in Unity 5.5.
    * Sets language level to C# 6 or C# 7.0 when using the [CSharp60Support](https://bitbucket.org/alexzzzz/unity-c-5.0-and-6.0-integration/src) plugin.
* Fixes an issue with Rider on Mono 4 where System.Linq references cannot be found (see [RIDER-573](https://youtrack.jetbrains.com/issue/RIDER-573)).
* Ensures `UnityEditor.iOS.XCode` namespaces are referenced correctly (see #15). 
* Support for gmcs.rsp and smcs.rsp keywords define and unsafe (see https://github.com/JetBrains/Unity3dRider/pull/46).

## How to use

**Set Rider as the default External Script Editor**

This only needs to be done once.

1. Open Unity.
2. Go to Edit &rarr; Preferences &rarr; External Tools.
3. Select "Browse" in the External Script Editor dropdown and select the Rider application.
    1. On Windows, navigate to `%APPDATA%\Microsoft\Windows\Start Menu\Programs\JetBrains Toolbox` and select "Rider"
    2. On Mac, select `~/Applications/Jet Brains/Toolbox/Rider.app` or `/Applications/Rider.app`
    3. On Linux, select `rider.sh`

**Install the plugin into your project**

This needs to be done for each project.

1. Copy the folder `Assets/Plugins/Editor/JetBrains` from this repository into `Assets/Plugins/Editor/JetBrains` in your project.

## Roadmap

This plugin is intended to be lightweight and simple, and is not intended to do much more than it currently does. If you have any issues or feature suggestions with regard to plugin functionality inside Unity, please [file an issue](https://github.com/JetBrains/Unity3dRider/issues).

If you have any issues or feature suggestion for Unity functionality inside Rider, please [file an issue with the ReSharper plugin](https://github.com/JetBrains/resharper-unity/issues).

Any issues or feature suggestion for Rider itself, please [file an issue with Rider](https://youtrack.jetbrains.com/issues/RIDER)
