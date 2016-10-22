# Unity3dRider

## How to use:
1. Open Unity
2. Menu `Edit` -> `Preferences` -> `External Tools`
3. `External Script Editor` -> `Browse` -> Select `Rider.exe` / `.lnk`, `rider.sh` or `Rider.app` depending on the OS
4. Put the folder `Assets/Plugins/Editor/Rider` from this repository into `Assets/Plugins/Editor/Rider` in your project.
5. Double clicking a file or error message in Unity should open Rider and navigate to the corresponding file and line.

The plugin should work on Windows, Linux and macOS.

## Common problems solved by this plugin
1. Basic Open Solution and Navigate to file and line
2. Rider on mono 4 - RIDER-573 System.Linq can not be found in a new Unity project
3. If Unity mono runtime is 2 and plugin CSharp60Support is not used -> LangVersion is set to 5.0, which prevents Rider to suggest C# 6 language improvements

## Useful info
1. To debug Unity in Rider call `Run`-> `Attach to local processes`
