package model.editorPlugin

import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*

@Suppress("unused")
object EditorPluginModel: Root() {

    var RdOpenFileArgs = structdef {
        field("path", string)
        field("line", int)
        field("col", int)
    }
    val RdLogEvent = structdef {
        field("time", long)
        field("type", RdLogEventType)
        field("mode", RdLogEventMode)
        field("message", string)
        field("stackTrace", string)
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

    val AssetFindUsagesResult = structdef extends  AssetFindUsagesResultBase {
    }

    val HierarchyFindUsagesResult = structdef extends  AssetFindUsagesResultBase {
        field("pathElements", array(string))
        field("rootIndices", array(int))
    }

    val RdLogEventType = enum {
        +"Error"
        +"Warning"
        +"Message"
    }

    val RdLogEventMode = enum {
        +"Edit"
        +"Play"
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

    val UnityEditorState = enum {
        +"Disconnected"
        +"Idle"
        +"Play"
        +"Pause"
        +"Refresh"
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

    val RunMethodData = structdef{
        field("assemblyName", string)
        field("typeName", string)
        field("methodName", string)
    }

    val RunMethodResult =  classdef{
        field("success", bool)
        field("message", string)
        field("stackTrace", string)
    }

    init {
        source("step", void)
        signal("showFileInUnity", string)
        signal("showUsagesInUnity", AssetFindUsagesResultBase)
        signal("sendFindUsagesSessionResult", FindUsagesSessionResult)
        signal("showPreferences", void)



        sink("log", RdLogEvent)

        callback("isBackendConnected", void, bool)
        call("getUnityEditorState", void, UnityEditorState)
        callback("openFileLineCol", RdOpenFileArgs, bool)
        call("updateUnityPlugin", string, bool)
        call("refresh", RefreshType, void)
        call("getCompilationResult", void, bool)
        sink("compiledAssemblies", immutableList(CompiledAssembly))

        call("runUnitTestLaunch", void, bool)

        call("runMethodInUnity", RunMethodData, RunMethodResult)



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

        property("ScriptCompilationDuringPlay", int)
        property("lastPlayTime", long)
        property("lastInitTime", long)

        property("buildLocation", string)
    }
}
