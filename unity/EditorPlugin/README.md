# JetBrains.Rider.Unity.Editor.Plugin

This project can be a little confusing, as it has evolved to support different Unity versions. This document aims to explain what's going on.

## Overview

The editor plugin is an assembly that is distributed with Rider, and loaded by the Rider package in the Unity editor. It is responsible for the integration between Rider and the Unity editor. Primarily it creates the "backend/Unity" protocol connection between Rider's ReSharper "backend" and the Unity editor.

Many integration features are enabled by the protocol, as the plugin is running in the editor instance and can call Unity APIs and relay data to and from Rider. Examples include the log viewer, the play controls, unit testing integration, navigate to results find asset usages, as well as simple data transfer such as application install location.

### JetBrains.Rider.PathLocator

Assembly, which encapsulates logic about locating all Rider install locations and starting Rider with solution and script. Is added as assembly to the Editor Package. It is also distributed as a nuget.

### Editor Package

The editor _plugin_ should not be confused with the editor _package_. The package is shipped by default with Unity 2019.2 and above, and is responsible for opening Unity projects in Rider.

It will generate the project files, locate all Rider install locations, start the correct instance and open files/projects. Most importantly, it will locate the editor _plugin_ and load it.

The package also provides useful functionality that the editor plugin can take advantage of, such as integration with the Unity test runner.

## Versions

There are 2 assemblies distributed with Rider:

* `EditorPlugin.SinceUnity.2019.2.csproj`/`JetBrains.Rider.Unity.Editor.Plugin.Net46.Repacked.dll`
* `EditorPlugin.SinceUnity.CorCLR.csproj`/`JetBrains.Rider.Unity.Editor.Plugin.CorCLR.Repacked.dll`

Each assembly is "repacked", meaning that several support assemblies (e.g. for the protocol) have been merged into the main assembly to produce a single distributable assembly.

The assemblies cannot easily be renamed. Editor Package would try to load one by name.

### `JetBrains.Rider.Unity.Editor.Plugin.Net46.Repacked.dll`

This version is built using Unity 2019.2.0f1, and targets `netstandard2.0`.

It is loaded by the editor package 3.x

The `EditorPlugin.SinceUnity.2019.2.csproj` project defines the `UNITY_2019_2` and `UNITY_2019_2_OR_NEWER` compilation symbols.

### `JetBrains.Rider.Unity.Editor.Plugin.CorCLR.Repacked.dll`

This version is built using Unity 7, and targets `netstandard2.1`.

It is loaded by the editor package 4.x+

The `EditorPlugin.SinceUnity.CorCLR.csproj` project defines the `UNITY_CORCLR_OR_NEWER` and `UNITY_2019_2_OR_NEWER` compilation symbols.