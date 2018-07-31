package model.rider

import com.jetbrains.rider.model.nova.ide.SolutionModel
import com.jetbrains.rider.generator.nova.*
import com.jetbrains.rider.generator.nova.PredefinedType.*

@Suppress("unused")
object RdUnityModel : Ext(SolutionModel.Solution) {
    private val UnitTestLaunchPreference = enum {
        +"NUnit"
        +"EditMode"
    }

    private val EditorState = enum {
        +"Disconnected"
        +"ConnectedIdle"
        +"ConnectedPlay"
        +"ConnectedRefresh"
    }

    init {
        map("data", string, string)

        sink("activateRider", void)

        property("editorState", EditorState)
        property("unitTestPreference", UnitTestLaunchPreference.nullable)
        property("hideSolutionConfiguration", bool)

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

        sink("onUnityLogEvent", structdef("logEntry") {
            field("type", int)
            field("mode", int)
            field("ticks", long)
            field("message", string)
            field("stackTrace", string)
        })
    }
}