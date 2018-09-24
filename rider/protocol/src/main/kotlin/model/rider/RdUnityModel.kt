package model.rider

import com.jetbrains.rider.model.nova.ide.SolutionModel
import com.jetbrains.rider.generator.nova.*
import com.jetbrains.rider.generator.nova.PredefinedType.*

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

    init {
        sink("activateRider", void)

        property("editorState", EditorState)
        property("unitTestPreference", UnitTestLaunchPreference.nullable)
        property("hideSolutionConfiguration", bool)

        property("applicationPath", string)
        property("applicationContentsPath", string)

        property("editorLogPath", string)
        property("playerLogPath", string)

        property("play", bool)
        property("pause", bool)

        source("step", void)
        source("refresh", bool)

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

        // It's a Unity project, but not necessarily loaded correctly (e.g. it might be opened as folder)
        property("isUnityProjectFolder", bool)
        // These values will be false unless we've opened a .sln file. Note that the "sidecar" project is a solution that
        // lives in the same folder as generated unity project (not the same as a class library project, which could live
        // anywhere)
        property("isUnityProject", bool)
        property("isUnityGeneratedProject", bool)
        property("hasUnityReference", bool)
    }
}