package model.backendUnity

import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*
import com.jetbrains.rd.generator.nova.csharp.CSharp50Generator
import model.lib.Library

// backend <-> Unity Editor model, from point of view of backend, meaning:
// Sink is a one-way signal the backend subscribes to
// Source is a one-way signal the backend fires
// Property and Signal are two-way and can be updated/fired on both ends. Property is stateful.
// Call is an RPC method (with return value) that is called by the backend/implemented by the Unity Editor
// Callback is an RPC method (with return value) that is implemented by the backend/called by the Unity Editor
@Suppress("unused")
object BackendUnityModel: Root() {

    var RdOpenFileArgs = structdef {
        field("path", string)
        field("line", int)
        field("col", int)
    }

    val FindUsagesSessionResult = structdef {
        field("target", string)
        field("elements", array(AssetFindUsagesResultBase))
    }

    val AssetFindUsagesResultBase = basestruct {
        field("expandInTreeView", bool)
        field("filePath", string)
        field("fileName", string)
        field("extension", string)
    }

    val AssetFindUsagesResult = structdef extends AssetFindUsagesResultBase {
    }

    val HierarchyFindUsagesResult = structdef extends AssetFindUsagesResultBase {
        field("pathElements", array(string))
        field("rootIndices", array(int))
    }

    val TestResult = structdef {
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

    val RunResult = structdef {
        field("passed", bool)
    }

    val TestMode = enum {
        +"Both"
        +"Edit"
        +"Play"
    }

    val TestFilter = structdef {
        field("assemblyName", string)
        field("testNames", immutableList(string))
        field("groupNames", immutableList(string))
        field("testCategories", immutableList(string))
    }

    val UnitTestLaunchClientControllerInfo = structdef {
        field("codeBase", string)
        field("codeBaseDependencies", immutableList(string).nullable)
        field("typeName", string)
    }

    val UnitTestLaunch = classdef {
        field("sessionId", string)
        field("testFilters", immutableList(TestFilter))
        field("testMode", TestMode)
        field("clientControllerInfo", UnitTestLaunchClientControllerInfo.nullable)
        property("runStarted", bool)
        sink("testResult", TestResult)
        sink("runResult", RunResult)
        call("abort", void, bool)
    }

    val RefreshType = enum {
        +"ForceRequestScriptReload"
        +"Force"
        +"Normal"
    }

    val CompiledAssembly = structdef {
        field("name", string)
        field("outputPath", string)
    }

    val UnityApplicationData = structdef{
        field("applicationPath", string)
        field("applicationContentsPath", string)
        field("applicationVersion", string)
    }

    init {
        setting(CSharp50Generator.Namespace, "JetBrains.Rider.Model.Unity.BackendUnity")

        callback("isBackendConnected", void, bool).documentation = "Called from Unity to ensure backend is connected before opening a file"

        // TODO: This should be a simple property, reset when the protocol is lost
        call("getUnityEditorState", void, Library.UnityEditorState).documentation = "Polled from the backend to get what the editor is currently doing"

        source("step", void)
        signal("showFileInUnity", string)
        signal("showUsagesInUnity", AssetFindUsagesResultBase)
        signal("sendFindUsagesSessionResult", FindUsagesSessionResult)
        signal("showPreferences", void)

        sink("log", Library.LogEvent)

        callback("openFileLineCol", RdOpenFileArgs, bool)
        call("updateUnityPlugin", string, bool)
        call("refresh", RefreshType, void)
        call("getCompilationResult", void, bool)
        sink("compiledAssemblies", immutableList(CompiledAssembly))

        call("runUnitTestLaunch", void, bool)

        call("runMethodInUnity", Library.RunMethodData, Library.RunMethodResult)

        call("generateUIElementsSchema", void, bool)
        call("exitUnity", void, bool)


        // statefull entities
        // do not forget, that protocol between Rider and Unity could be destroyed when
        // 1) Unity reloads AppDomain (e.g enter playmode, script compilation)
        // 2) Unity lost connection to Rider

        // If your value is not set on protocol initialization or depends on some Unity event,
        // do not forget to store it outside protocol and restore when protocol is recreated
        property("play", bool)
        property("pause", bool)

        property("riderProcessId", int)
        property("unityProcessId", int)

        property("unityApplicationData", UnityApplicationData)
        property("scriptingRuntime", int)

        property("unitTestLaunch", UnitTestLaunch)

        property("editorLogPath", string)
        property("playerLogPath", string)

        property("ScriptCompilationDuringPlay", Library.ScriptCompilationDuringPlay)
        property("lastPlayTime", long)
        property("lastInitTime", long)

        property("buildLocation", string)
    }
}
