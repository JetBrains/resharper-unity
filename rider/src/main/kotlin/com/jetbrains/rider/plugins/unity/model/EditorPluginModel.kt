package com.jetbrains.rider.plugins.unity.model

import com.jetbrains.rider.generator.nova.*
import com.jetbrains.rider.generator.nova.PredefinedType.*
import com.jetbrains.rider.generator.nova.csharp.CSharp50Generator
import com.jetbrains.rider.generator.nova.kotlin.Kotlin11Generator
import com.jetbrains.rider.generator.nova.util.syspropertyOrEmpty
import java.io.File

object EditorPluginModel: Root(
    CSharp50Generator(FlowTransform.AsIs, "JetBrains.Platform.Unity.EditorPluginModel", File("../resharper/src/resharper-unity/Rider")),
    CSharp50Generator(FlowTransform.Reversed, "JetBrains.Platform.Unity.EditorPluginModel", File("../unity/JetBrains.Rider.Unity.Editor/EditorPlugin/NonUnity")),
    Kotlin11Generator(FlowTransform.AsIs, "com.jetbrains.rider.plugins.unity.editorPlugin.model", File("src/main/kotlin/com/jetbrains/rider"))
){
    var RdOpenFileArgs = structdef{
        field("path", string)
        field("line", int)
        field("col", int)
    }
    val RdLogEvent = structdef {
        field("type", RdLogEventType)
        field("mode", RdLogEventMode)
        field("message", string)
        field("stackTrace", string)
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

    val UnityLogModelInitialized = classdef {
        sink("log", RdLogEvent)
    }

    val TestResult = structdef {
        field("testId", string)
        field("status", enum {
            +"Pending"
            +"Running"
            +"Passed"
            +"Failed"
        })
    }

    val UnitTestLaunch = classdef {
        field("testNames", immutableList(string))
        field("testGroups", immutableList(string))
        field("testCategories", immutableList(string))
        sink("testResult", TestResult)
    }

    val UnityEditorState = enum {
        +"Disconnected"
        +"Idle"
        +"Play"
        +"Refresh"
    }

    init {
        property("play", bool)
        property("pause", bool)
        call("step", void, void)

        property("unityPluginVersion", string)
        property("riderProcessId", int)

        property("applicationPath", string)
        property("applicationVersion", string)

        property("logModelInitialized", UnityLogModelInitialized)

        callback("isBackendConnected", void, bool)
        call("getUnityEditorState", void, UnityEditorState)
        callback("openFileLineCol", RdOpenFileArgs, bool)
        call("updateUnityPlugin", string, bool)
        call("refresh", bool, void)

        property("unitTestLaunch", UnitTestLaunch)
    }
}