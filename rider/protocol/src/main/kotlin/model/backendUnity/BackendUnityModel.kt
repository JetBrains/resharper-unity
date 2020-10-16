package model.backendUnity

import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*
import com.jetbrains.rd.generator.nova.csharp.CSharp50Generator
import model.lib.Library

// backend <-> Unity Editor model, from point of view of backend, meaning:
// Sink is a one-way signal the backend subscribes to (editor fires)
// Source is a one-way signal the backend fires (editor subscribes)
// Property and Signal are two-way and can be updated/fired on both ends. Property is stateful.
// Call is an RPC method (with return value) that is called by the backend/implemented by the Unity Editor
// Callback is an RPC method (with return value) that is implemented by the backend/called by the Unity Editor
@Suppress("unused")
object BackendUnityModel: Root() {

    private var RdOpenFileArgs = structdef {
        field("path", string)
        field("line", int)
        field("col", int)
    }

    private val FindUsagesSessionResult = structdef {
        field("target", string)
        field("elements", array(AssetFindUsagesResultBase))
    }

    private val AssetFindUsagesResultBase = basestruct {
        field("expandInTreeView", bool)
        field("filePath", string)
        field("fileName", string)
        field("extension", string)
    }

    private val AssetFindUsagesResult = structdef extends AssetFindUsagesResultBase {
    }

    private val HierarchyFindUsagesResult = structdef extends AssetFindUsagesResultBase {
        field("pathElements", array(string))
        field("rootIndices", array(int))
    }

    private val TestResult = structdef {
        field("testId", string)
        field("projectName", string)
        field("output", string)
        field("duration", int)
        field("status", enum {
            +"Pending"
            +"Running"
            +"Inconclusive"
            +"Ignored"
            +"Success"
            +"Failure"
        })
        field("parentId", string)
    }

    private val RunResult = structdef {
        field("passed", bool)
    }

    private val TestMode = enum {
        +"Both"
        +"Edit"
        +"Play"
    }

    private val TestFilter = structdef {
        field("assemblyName", string)
        field("testNames", immutableList(string))
        field("groupNames", immutableList(string))
        field("testCategories", immutableList(string))
    }

    private val UnitTestLaunchClientControllerInfo = structdef {
        field("codeBase", string)
        field("codeBaseDependencies", immutableList(string).nullable)
        field("typeName", string)
    }

    private val UnitTestLaunch = classdef {
        field("sessionId", string)
        field("testFilters", immutableList(TestFilter))
        field("testMode", TestMode)
        field("clientControllerInfo", UnitTestLaunchClientControllerInfo.nullable)
        property("runStarted", bool)
        sink("testResult", TestResult)
        sink("runResult", RunResult)
        call("abort", void, bool)
    }

    private val RefreshType = enum {
        +"ForceRequestScriptReload"
        +"Force"
        +"Normal"
    }

    private val CompiledAssembly = structdef {
        field("name", string)
        field("outputPath", string)
    }

    init {
        setting(CSharp50Generator.Namespace, "JetBrains.Rider.Model.Unity.BackendUnity")

        // *************************************************************************************************************
        //
        // WARNING!
        //
        // Be very careful about stateful data, e.g. RD properties or locally cached values
        //
        // Properties are stateful, and are not reset to a default value when a connection is closed. This means the
        // model in the backend will contain stale values when the Unity Editor connection is lost. This will happen
        // every time the Unity Editor reloads an AppDomain (e.g. enters play mode, script compilation, etc) and of
        // course when the Unity Editor is closed.
        //
        // Properties MUST be set to an initial value every time the Unity Editor model is created (on initial editor
        // launch, and on every AppDomain reload).
        //
        // Take care with stale properties in the backend when the Unity Editor connection is lost. These properties
        // will contain stale values, and SHOULD be rest if this stale data is dangerous (e.g. process IDs would be
        // invalid, whereas application paths can be incorrect, but less likely to cause serious issues).
        //
        // Also take care with locally cached values. Make sure to update correctly when the protocol is reset, either
        // using them to set initial values in the protocol, or updated from the protocol.
        //
        // *************************************************************************************************************

        // TODO: Is this useful? Can we just try to call openFileLineCol directly?
        callback("isBackendConnected", void, bool).documentation = "Called from Unity to ensure backend is connected before opening a file"

        // TODO: This should be a simple property, reset when the protocol is lost
        call("getUnityEditorState", void, Library.UnityEditorState).documentation = "Polled from the backend to get what the editor is currently doing"

        // Unity application data. Static for the lifetime of the Unity editor
        property("unityApplicationData", Library.UnityApplicationData)

        // Unity application settings
        property("scriptCompilationDuringPlay", Library.ScriptCompilationDuringPlay)

        // Unity project settings
        property("scriptingRuntime", int).documentation = "Refers to ScriptingRuntimeVersion enum. Obsolete since 2019.3 when legacy Mono was removed"
        property("buildLocation", string).documentation = "Path to the executable of the last built Standalone player, if it exists. Can be empty"

        // Rider application settings (frontend)
        property("riderProcessId", int).documentation = "The process ID of the frontend, set by the backend. Unity uses this in a call to AllowSetForegroundWindow, so that Rider can bring itself to the foreground when opening a file"

        // Play controls. Play and pause are switches, step is an action
        property("play", bool)
        property("pause", bool)
        source("step", void)

        // Logging
        sink("log", Library.LogEvent)
        property("lastPlayTime", long)
        property("lastInitTime", long)

        // Actions called from the backend to Unity
        // (These should probably be calls rather than signals, as they are definitely RPC calls, not events)
        signal("showPreferences", void).documentation = "Opens the preferences dialog in Unity"
        signal("showFileInUnity", string).documentation = "Switches to Unity, focuses the Project view and selects and pings the requested file"
        signal("showUsagesInUnity", AssetFindUsagesResultBase).documentation = "Switches to Unity, focuses the Project view, select and ping either the selected file (prefab) or Inspector object"
        signal("sendFindUsagesSessionResult", FindUsagesSessionResult).documentation = "Sends Find Usages results to Unity, to display in a tool window"

        call("updateUnityPlugin", string, bool)
        call("exitUnity", void, bool)
        call("refresh", RefreshType, void).documentation = "Refresh the asset database"
        call("getCompilationResult", void, bool).documentation = "Called after Refresh to get the compilation result before launching unit tests"
        call("generateUIElementsSchema", void, bool).documentation = "Generates the UIElements schema, if available"
        call("runMethodInUnity", Library.RunMethodData, Library.RunMethodResult)

        // Actions called from Unity to the backend
        callback("openFileLineCol", RdOpenFileArgs, bool).documentation = "Called from Unity to quickly open a file in an existing Rider instance"
        sink("compiledAssemblies", immutableList(CompiledAssembly)).documentation = "Fired from Unity to provide a list of the assemblies compiled by Unity"

        // Unit testing
        property("unitTestLaunch", UnitTestLaunch).documentation = "Set the details of the current unit test session"
        call("runUnitTestLaunch", void, bool).documentation = "Start the unit test session. Results are fired via UnitTestLaunch.TestResult"
    }
}
