package model.rider

import com.jetbrains.rider.model.nova.ide.SolutionModel
import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*

// frontend <-> backend model, from point of view of frontend, meaning:
// Sink is a one-way signal the frontend subscribes to
// Source is a one-way signal the frontend fires
// Property and Signal are two-way and can be updated/fired on both ends
// Call is an RPC method (with return value) that is called by the frontend/implemented by the backend
// Callback is an RPC method (with return value) that is implemented by the frontend/called by the backend
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
        +"ConnectedPause"
        +"ConnectedRefresh"
    }

    private val ScriptCompilationDuringPlay = enum {
        +"RecompileAndContinuePlaying"
        +"RecompileAfterFinishedPlaying"
        +"StopPlayingAndRecompile"
    }

    val UnityApplicationData = structdef {
        field("applicationPath", string)
        field("applicationContentsPath", string)
        field("applicationVersion", string)
        field("requiresRiderPackage", bool)
    }

    init {
        sink("activateRider", void)
        sink("activateUnityLogView", void)
        sink("showInstallMonoDialog", void)

        property("editorState", EditorState)
        property("unitTestPreference", UnitTestLaunchPreference.nullable)
        property("hideSolutionConfiguration", bool)

        property("unityApplicationData", UnityApplicationData)

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
        sink("showDeferredCachesProgressNotification", void)
        property("isDeferredCachesCompletedOnce", bool)

        property("ScriptCompilationDuringPlay", ScriptCompilationDuringPlay)
        source("enableYamlParsing", void)

        signal("showFileInUnity", string)
        property("unityProcessId", int)

        sink("onEditorModelOutOfSync", void)
        callback("attachDebuggerToUnityEditor", void, bool)
        callback("allowSetForegroundWindow", void, bool)

        call("generateUIElementsSchema", void, bool)

        property("useUnityYamlMerge", bool)
        property("mergeParameters", string)

        property("buildLocation", string)
    }
}