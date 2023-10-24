package model.frontendBackend

import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*
import com.jetbrains.rd.generator.nova.csharp.CSharp50Generator
import com.jetbrains.rd.generator.nova.kotlin.Kotlin11Generator
import com.jetbrains.rider.model.nova.ide.SolutionModel
import com.jetbrains.rider.model.nova.ide.SolutionModel.RdDocumentId
import model.lib.Library

// frontend <-> backend model, from point of view of frontend, meaning:
// Sink is a one-way signal the frontend subscribes to
// Source is a one-way signal the frontend fires
// Signal is a two-way signal that either end can subscribe to and fire
// Property and Signal are two-way and can be updated/fired on both ends. Property is stateful.
// Call is an RPC method (with return value) that is called by the frontend/implemented by the backend
// Callback is an RPC method (with return value) that is implemented by the frontend/called by the backend
@Suppress("unused")
object FrontendBackendModel : Ext(SolutionModel.Solution) {

    // TODO [213] share model in library, too late to change model in 212
    private var RdOpenFileArgs = structdef {
        field("path", string)
        field("line", int)
        field("col", int)
    }

    private val UnityPackageSource = enum {
        +"Unknown"
        +"BuiltIn"
        +"Registry"
        +"Embedded"
        +"Local"
        +"LocalTarball"
        +"Git"
    }

    private val UnityPackage = structdef {
        field("id", string)
        field("version", string)
        field("packageFolderPath", string.nullable)
        field("source", UnityPackageSource)
        field("displayName", string)
        field("description", string.nullable)
        field("dependencies", array(structdef("unityPackageDependency") {
            field("id", string)
            field("version", string)
        }))
        field("tarballLocation", string.nullable)
        field("gitDetails", structdef("unityGitDetails") {
            field("url", string)
            field("hash", string.nullable)
            field("revision", string.nullable)
        }.nullable)
    }

    private val UnitTestLaunchPreference = enum {
        +"NUnit"
        +"EditMode"
        +"PlayMode"
        +"Both"
    }

    private val shaderInternScope = internScope()

    // Shader Contexts
    private val shaderContextDataBase = basestruct {}
    private val autoShaderContextData = structdef extends shaderContextDataBase {}
    private val shaderContextData = structdef extends shaderContextDataBase {
        field("path", string.interned(shaderInternScope).attrs(KnownAttrs.NlsSafe))
        field("name", string.interned(shaderInternScope).attrs(KnownAttrs.NlsSafe))
        field("folder", string.interned(shaderInternScope).attrs(KnownAttrs.NlsSafe))
        field("start", int)
        field("end", int)
        field("startLine", int)
    }

    // Shader Variants
    private val rdShaderVariant = structdef {
        field("name", string.interned(shaderInternScope).attrs(KnownAttrs.NlsSafe))
    }
    private val rdShaderVariantSet = classdef {
        set("selectedVariants", string.interned(shaderInternScope)).readonly
        source("selectVariant", string)
        source("deselectVariant", string)
    }

    init {
        setting(Kotlin11Generator.Namespace, "com.jetbrains.rider.plugins.unity.model.frontendBackend")
        setting(CSharp50Generator.Namespace, "JetBrains.Rider.Model.Unity.FrontendBackend")

        property("hasUnityReference", bool).documentation = "True when the current project is a Unity project. Either full Unity project or class library"

        // Connection to Unity editor
        property("unityEditorConnected", bool).documentation = "Is the backend/Unity protocol connected?"
        property("playControlsInitialized", bool).documentation = "Have we got playControls state from Unity?"
        property("unityEditorState", Library.UnityEditorState)

        property("unityApplicationData", Library.UnityApplicationData)
        property("requiresRiderPackage", bool)
        field("unityApplicationSettings", Library.UnityApplicationSettings)
        field("unityProjectSettings", Library.UnityProjectSettings)

        // Settings stored in the backend
        field("backendSettings", aggregatedef("BackendSettings") {
            property("enableShaderLabHippieCompletion", bool)

            property("useUnityYamlMerge", bool)
            property("mergeParameters", string)

            property("enableDebuggerExtensions", bool)
            property("ignoreBreakOnUnhandledExceptionsForIl2Cpp", bool)
        })

        field("playControls", Library.PlayControls)
        field("consoleLogging", Library.ConsoleLogging)

        property("packagesUpdating", bool.nullable)
        map("packages", string, UnityPackage)
        map("discoveredTechnologies", string, bool)
        property("isTechnologyDiscoveringFinished", bool)


        // Unit testing
        property("unitTestPreference", UnitTestLaunchPreference.nullable).documentation = "Selected unit testing mode. Everything is handled by the backend, but this setting is from a frontend combobox"

        // Shader contexts
        map("shaderContexts", RdDocumentId, shaderContextDataBase).readonly
        call("createSelectShaderContextInteraction", RdDocumentId, classdef("selectShaderContextDataInteraction") {
            field("items", immutableList(shaderContextData))
            source("selectItem", int) // -1 for no auto-context
        })

        // Shader variants
        map("shaderVariants", string, rdShaderVariant).readonly
        property("defaultShaderVariantSet", rdShaderVariantSet).readonly

        // Actions called from the frontend to the backend (and/or indirectly, Unity)
        // (These should probably be calls, rather than signal/source/sink, as they are RPC, and not events)
        source("refresh", bool).documentation = "Refresh the asset database. Pass true to force a refresh. False will queue a refresh"
        source("showPreferences", void).documentation = "Tell the Unity model to show the preferences window"
        source("installEditorPlugin", void)
        source("showFileInUnity", string).documentation = "Focus Unity, focus the Project window and select and ping the given file path"
        call("generateUIElementsSchema", void, bool).documentation = "Tell the Unity backend to generate UIElement schema"
        call("hasUnsavedState", void, bool).documentation = "Returns true if the currently open Unity editor has any unsaved state, such as scenes, prefabs, etc."
        call("getAndroidSdkRoot", void, string.nullable).async.documentation = "Get the currently configured Android SDK root location, if available"

        // Actions called from the backend to the frontend
        sink("activateRider", void).documentation = "Tell Rider to bring itself to the foreground. Called when opening a file from Unity"
        sink("activateUnityLogView", void).documentation = "Show the Unity log tool window. E.g. in response to compilation failure"
        sink("showInstallMonoDialog", void)
        sink("startUnity", void)
        sink("notifyAssetModeForceText", void)
        sink("showDeferredCachesProgressNotification", void)
        callback("attachDebuggerToUnityEditor", void, bool).documentation = "Tell the frontend to attach the debugger to the Unity editor. Used for debugging unit tests"
        callback("allowSetForegroundWindow", void, bool).documentation = "Tell the frontend to call AllowSetForegroundWindow for the current Unity editor process ID. Called before the backend tells Unity to show itself"

        // Only used in integration tests
        property("riderFrontendTests", bool)
        call("runMethodInUnity", Library.RunMethodData, Library.RunMethodResult)
        property("isDeferredCachesCompletedOnce", bool)
        property("isUnityPackageManagerInitiallyIndexFinished", bool)

        // Actions called from Unity to the backend
        callback("openFileLineCol", RdOpenFileArgs, bool).documentation = "Called from Unity to quickly open a file in an existing Rider instance"

        // profiler
        call("startProfiling", bool, void).documentation = "Start profiling and enter PlayMode, depending on the param"

        // debug
        call("getScriptingBackend", void, int).documentation = "Mono, IL2CPP, WinRTDotNET"
    }
}