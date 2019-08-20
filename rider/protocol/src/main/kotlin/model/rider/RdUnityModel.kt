package model.rider

import com.jetbrains.rider.model.nova.ide.SolutionModel
import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*

@Suppress("unused")
object RdUnityModel : Ext(SolutionModel.Solution) {
    private val UnitTestLaunchPreference = enum {
        +"NUnit"
        +"EditMode"
        +"PlayMode"
    }

    private val EditorState = enum {
        +"Disconnected"
        +"ConnectedIdle"
        +"ConnectedPlay"
        +"ConnectedRefresh"
    }

    private val ScriptCompilationDuringPlay = enum {
        +"RecompileAndContinuePlaying"
        +"RecompileAfterFinishedPlaying"
        +"StopPlayingAndRecompile"
    }

    private val FindUsageResult = structdef {
        field("target", string)
        field("elements", array(FindUsageResultElement))
    }

    private val FindUsageResultElement = structdef {
        field("isPrefab", bool)
        field("expandInTreeView", bool)
        field("filePath", string)
        field("fileName", string)
        field("pathElements", array(string))
        field("rootIndices", array(int))
    }

    init {
        sink("activateRider", void)
        sink("activateUnityLogView", void)
        sink("showInstallMonoDialog", void)

        property("editorState", EditorState)
        property("unitTestPreference", UnitTestLaunchPreference.nullable)
        property("hideSolutionConfiguration", bool)

        property("applicationPath", string)
        property("applicationContentsPath", string)
        property("applicationVersion", string)

        property("editorLogPath", string)
        property("playerLogPath", string)

        property("play", bool)
        sink("clearOnPlay", long)
        property("pause", bool)

        source("step", void)
        source("refresh", bool)
        source("showPreferences", void)

        property("sessionInitialized", bool)

        property("enableShaderLabHippieCompletion", bool)

        // doesn't seem like the best way to do this
        property("externalDocContext", string)

        sink("onUnityLogEvent", structdef("editorLogEntry") {
            field("type", int)
            field("mode", int)
            field("ticks", long)
            field("message", string)
            field("stackTrace", string)
        })

        source("installEditorPlugin", void)

        property("hasUnityReference", bool)

        sink("startUnity", void)
        sink("notifyYamlHugeFiles", void)
        sink("notifyAssetModeForceText", void)
        property("ScriptCompilationDuringPlay", ScriptCompilationDuringPlay)
        source("enableYamlParsing", void)

        signal("findUsageResults", FindUsageResult)
        signal("showGameObjectOnScene", FindUsageResultElement)
        signal("showFileInUnity", string)
        property("unityProcessId", int)

        sink("onEditorModelOutOfSync", void)
        callback("attachDebuggerToUnityEditor", void, bool)
    }
}