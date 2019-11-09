package model.backendUnity

import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*

@Suppress("unused")
object BackendUnityModel: Root() {

    private val RdOpenFileArgs = structdef {
        field("path", string)
        field("line", int)
        field("col", int)
    }

    private val RdLogEvent = structdef {
        field("time", long)
        field("type", RdLogEventType)
        field("mode", RdLogEventMode)
        field("message", string)
        field("stackTrace", string)
    }

    private val RdFindUsageResult = structdef {
        field("target", string)
        field("elements", array(RdFindUsageResultElement))
    }

    private val RdFindUsageResultElement = structdef {
        field("isPrefab", bool)
        field("expandInTreeView", bool)
        field("filePath", string)
        field("fileName", string)
        field("pathElements", array(string))
        field("rootIndices", array(int))
    }

    private val RdLogEventType = enum {
        +"Error"
        +"Warning"
        +"Message"
    }

    private val RdLogEventMode = enum {
        +"Edit"
        +"Play"
    }

    private val TestResult = structdef {
        field("testId", string)
        field("projectName", string)
        field("output", string)
        field("duration", int)
        field("status", TestResultStatus)
        field("parentId", string)
    }

    private val TestResultStatus = enum {
        +"Pending"
        +"Running"
        +"Inconclusive"
        +"Ignored"
        +"Success"
        +"Failure"
    }

    private val RunResult = structdef {
        field("passed", bool)
    }

    private val TestMode = enum {
        +"Both"
        +"Edit"
        +"Play"
    }

    private val TestFilter = structdef{
        field("assemblyName", string)
        field("testNames", immutableList(string))
    }

    private val UnitTestLaunch = classdef {
        field("testFilters", immutableList(TestFilter))
        field("testGroups", immutableList(string))
        field("testCategories", immutableList(string))
        field("testMode", TestMode)
        sink("testResult", TestResult)
        sink("runResult", RunResult)
        call("abort", void, bool)
    }

    private val UnityEditorState = enum {
        +"Disconnected"
        +"Idle"
        +"Play"
        +"Pause"
        +"Refresh"
    }

    private val RefreshType = enum {
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
        sink("clearOnPlay", long)
    }
}
