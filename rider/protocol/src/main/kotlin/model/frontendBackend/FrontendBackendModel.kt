package model.frontendBackend

import com.jetbrains.rider.model.nova.ide.SolutionModel
import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*
import com.jetbrains.rd.generator.nova.csharp.CSharp50Generator
import com.jetbrains.rd.generator.nova.kotlin.Kotlin11Generator
import com.jetbrains.rider.model.nova.ide.SolutionModel.RdDocumentId
import model.lib.Library

// frontend <-> backend model, from point of view of frontend, meaning:
// Sink is a one-way signal the frontend subscribes to
// Source is a one-way signal the frontend fires
// Property and Signal are two-way and can be updated/fired on both ends
// Call is an RPC method (with return value) that is called by the frontend/implemented by the backend
// Callback is an RPC method (with return value) that is implemented by the frontend/called by the backend
@Suppress("unused")
object FrontendBackendModel : Ext(SolutionModel.Solution) {
    private val UnitTestLaunchPreference = enum {
        +"NUnit"
        +"EditMode"
        +"PlayMode"
    }

    private val ScriptCompilationDuringPlay = enum {
        +"RecompileAndContinuePlaying"
        +"RecompileAfterFinishedPlaying"
        +"StopPlayingAndRecompile"
    }

    private val UnityApplicationData = structdef {
        field("applicationPath", string)
        field("applicationContentsPath", string)
        field("applicationVersion", string)
        field("requiresRiderPackage", bool)
    }

    private val shaderInternScope = internScope()

    private val shaderContextDataBase = baseclass {

    }

    private val autoShaderContextData = classdef extends shaderContextDataBase {

    }

    private val shaderContextData = classdef extends shaderContextDataBase {
        field("path", string.interned(shaderInternScope))
        field("name", string.interned(shaderInternScope))
        field("folder", string.interned(shaderInternScope))
        field("start", int)
        field("end", int)
        field("startLine", int)
    }


    init {
        setting(Kotlin11Generator.Namespace, "com.jetbrains.rider.model.unity.frontendBackend")
        setting(CSharp50Generator.Namespace, "JetBrains.Rider.Model.Unity.FrontendBackend")

        sink("activateRider", void)
        sink("activateUnityLogView", void)
        sink("showInstallMonoDialog", void)

        property("editorState", Library.EditorState)
        property("unitTestPreference", UnitTestLaunchPreference.nullable)
        property("hideSolutionConfiguration", bool)

        property("unityApplicationData", UnityApplicationData)


        property("editorLogPath", string)
        property("playerLogPath", string)

        property("play", bool)
        property("pause", bool)
        source("step", void)
        source("refresh", bool)
        source("showPreferences", void)

        property("lastPlayTime", long)
        property("lastInitTime", long)

        property("sessionInitialized", bool)

        property("enableShaderLabHippieCompletion", bool)

        // doesn't seem like the best way to do this
        property("externalDocContext", string)

        sink("onUnityLogEvent", Library.LogEvent)

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

        field("backendSettings", aggregatedef("BackendSettings") {
            property("enableDebuggerExtensions", bool)
        })

        property("riderFrontendTests", bool)
        // Note: only called from integration tests
        call("runMethodInUnity", Library.RunMethodData, Library.RunMethodResult)


        call("requestShaderContexts", RdDocumentId, immutableList(shaderContextDataBase))
        call("requestCurrentContext", RdDocumentId, shaderContextDataBase)
        source("setAutoShaderContext", RdDocumentId)
        source("changeContext", structdef ("contextInfo"){
            field("target", RdDocumentId)
            field("path", string.interned(shaderInternScope))
            field("start", int)
            field("end", int)
        })
    }
}