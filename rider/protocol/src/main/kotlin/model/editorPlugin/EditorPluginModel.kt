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

    val RdFindUsageResult = structdef {
        field("target", string)
        field("elements", array(RdFindUsageResultElement))
    }

    val RdFindUsageResultElement = structdef {
        field("isPrefab", bool)
        field("expandInTreeView", bool)
        field("filePath", string)
        field("fileName", string)
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
        +"Edit"
        +"Play"
    }

    val TestFilter = structdef{
        field("assemblyName", string)
        field("testNames", immutableList(string))
    }

    val UnitTestLaunch = classdef {
        field("testFilters", immutableList(TestFilter))
        field("testGroups", immutableList(string))
        field("testCategories", immutableList(string))
        field("testMode", TestMode)
        sink("testResult", TestResult)
        sink("runResult", RunResult)
        call("abort", void, bool)
    }

    val UnityEditorState = enum {
        +"Disconnected"
        +"Idle"
        +"Play"
        +"Refresh"
    }

    val RefreshType = enum {
        +"ForceRequestScriptReload"
        +"Force"
        +"Normal"
    }

    init {
        property("play", bool)
        property("pause", bool)
        source("step", void)
        signal("showFileInUnity", string)
        signal("showGameObjectOnScene", RdFindUsageResultElement)
        signal("findUsageResults", RdFindUsageResult)
        signal("showPreferences", void)

        property("unityPluginVersion", string)
        property("riderProcessId", int)
        property("unityProcessId", int)

        property("applicationPath", string)
        property("applicationContentsPath", string)
        property("applicationVersion", string)
        property("scriptingRuntime", int)

        sink("log", RdLogEvent)

        callback("isBackendConnected", void, bool)
        call("getUnityEditorState", void, UnityEditorState)
        callback("openFileLineCol", RdOpenFileArgs, bool)
        call("updateUnityPlugin", string, bool)
        call("refresh", RefreshType, void)
        call("getCompilationResult", void, bool)

        property("unitTestLaunch", UnitTestLaunch)
        source("runUnitTestLaunch", void)

        property("fullPluginPath", string)

        property("editorLogPath", string)
        property("playerLogPath", string)

        property("ScriptCompilationDuringPlay", int)
    }
}
