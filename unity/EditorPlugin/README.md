# JetBrains.Rider.Unity.Editor.Plugin

This project can be a little confusing, as it has evolved to support different Unity versions as well as different install methods. This document aims to explain what's going on.

## Overview

The editor plugin is an assembly that is distributed with Rider, and loaded by the Unity editor. Since 2019.2, this is handled by the Rider package, but previously, Rider would install it directly in the `Assets` folder, where it is automatically loaded by Unity. It is responsible for the integration between Rider and the Unity editor. Primarily it creates the "backend/Unity" protocol connection between Rider's ReSharper "backend" and the Unity editor.

Many integration features are enabled by the protocol, as the plugin is running in the editor instance and can call Unity APIs and relay data to and from Rider. Examples include the log viewer, the play controls, unit testing integration, navigate to results find asset usages, as well as simple data transfer such as application install location.

### Editor Package

The editor _plugin_ should not be confused with the editor _package_. The package is shipped by default with Unity 2019.2 and above, and is responsible for opening Unity projects in Rider.

It will generate the project files, locate all Rider install locations, start the correct instance and open files/projects. Most importantly, it will locate the editor _plugin_ and load it.

The package also provides useful functionality that the editor plugin can take advantage of, such as integration with the Unity test runner.

## Versions

There are four assemblies distributed with Rider:

* `EditorPlugin.csproj`/`JetBrains.Rider.Unity.Editor.Plugin.Repacked.dll`
* `EditorPluginUnity56`/`JetBrains.Rider.Unity.Editor.Plugin.Unity56.Repacked.dll`
* `EditorPluginFull`/`JetBrains.Rider.Unity.Editor.Plugin.Full.Repacked.dll`
* `EditorPluginNet46`/`JetBrains.Rider.Unity.Editor.Plugin.Net46.Repacked.dll`

Each assembly is "repacked", meaning that several support assemblies (e.g. for the protocol) have been merged into the main assembly to produce a single distributable assembly.

The assemblies cannot easily be renamed. Older versions of Rider will try to clean up plugins installed to `Assets` when migrating to the package in Unity 2019.2 and above. Changing the name of these files will break that clean up. Similarly, current and older versions of the package will try to load assemblies by these names from a Rider installation. Renaming them would break this.

All projects define the `RIDER_EDITOR_PLUGIN` compilation symbol. This is used in `RiderPathLocator` to share logic between the editor package and the legacy plugin installs. However, the files are currently different and it's not clear if this symbol is needed any longer.

### `JetBrains.Rider.Unity.Editor.Plugin.Repacked.dll`

The first version of the plugin, which supports Unity 4.7.2f1 and above (although Rider really only aims to support Unity 5+). It is (optionally) installed into the `Assets` folder by Rider when opening a Unity project in Unity 5.5 or below. It is compiled against Unity 4.7.2f1 references.

The `EditorPlugin.csproj` project defines the `UNITY_4_7` compilation symbol.

### `JetBrains.Rider.Unity.Editor.Plugin.Unity56.Repacked.dll`

This version is installed into the `Assets` folder by Rider when opening a project that's targeted at Unity editor 5.6 and above, up to and including 2017.2. It is compiled against Unity 5.6.7, and is a superset of the `Plugin.Repacked.dll` version, including additional integration features, primarily support for unit testing.

The `EditorPluginUnity56.csproj` project defines the `UNITY_5_6` compilation symbol.

### `JetBrains.Rider.Unity.Editor.Plugin.Full.Repacked.dll`

Rider will copy this version to the `Assets` folder for Unity 2017.3 and later, up to and including Unity 2019.1. This is the last version that Rider will copy. Unity 2019.2 and later use the editor package to automatically load the plugin.

This assembly is compiled against Unity 2017.3.0f3 and targets the `net35` TFM, like previous versions. These versions require special builds of the RD protocol assemblies, which are merged with the final output.

The `EditorPluginFull.csproj` project defines the `UNITY_2017_3` compilation symbol.

### `JetBrains.Rider.Unity.Editor.Plugin.Net46.Repacked.dll`

This version is built using Unity 2019.2.0f1, and targets `net472`, to support Unity's upgraded .NET 4.6 runtime, introduced as preview in Unity 2017.1 and made the default in Unity 2018.1. It also allows us to use the default RD protocol assemblies.

It is the default version loaded by the editor package. If it doesn't exist (e.g. loading from a version of Rider prior to 2019.3) then the package falls back to `Full.Repacked.dll`. It is never copied to the `Assets` folder.

The `EditorPluginNet46.csproj` project defines the `UNITY_2019_2_OR_NEWER` compilation symbol.
