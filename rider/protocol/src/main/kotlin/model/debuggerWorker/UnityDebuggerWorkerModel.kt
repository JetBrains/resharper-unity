package model.debuggerWorker

import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*
import com.jetbrains.rd.generator.nova.csharp.CSharp50Generator
import com.jetbrains.rd.generator.nova.kotlin.Kotlin11Generator
import com.jetbrains.rider.model.nova.debugger.main.DebuggerWorkerModel

@Suppress("unused")
object UnityDebuggerWorkerModel : Ext(DebuggerWorkerModel) {

    private val unityBundleInfo = structdef {
        field("id", string)
        field("absolutePath", string)
    }

    // Not used in this model, but referenced via debuggerStartInfoBase. Serialisers will be registered along with this
    // model (directly via UnityDebuggerWorkerModel.RegisterDeclaredTypesSerializers() or indirectly via creating a new
    // UnityDebuggerWorkerModel)
    private val unityStartInfoBase = basestruct extends DebuggerWorkerModel.debuggerStartInfoBase {
        field("bundles", immutableList(unityBundleInfo))
        field("packages", immutableList(string))
    }

    // Base type for Mono based players (including IL2CPP)
    private val unityMonoStartInfoBase = basestruct extends unityStartInfoBase {
        field("monoAddress", string.nullable)
        field("monoPort", int)
        field("listenForConnections", bool)
    }

    // Default start info. Performs the same as MonoAttachStartInfo but allows overriding some options for IL2CPP
    private val unityMonoStartInfo = structdef extends unityMonoStartInfoBase {
    }

    private val unityLocalCoreClrStartInfo = structdef extends unityStartInfoBase {
        field("processId", int)
    }

    // Forward Android debugging ports over ADB
    private val unityAndroidAdbStartInfo = structdef extends unityMonoStartInfoBase {
        field("androidSdkRoot", string)
        field("androidDeviceId", string)
    }

    // Start the iOS USB debugging proxy before attaching
    private val unityIosUsbStartInfo = structdef extends unityMonoStartInfoBase {
        field("iosSupportPath", string)
        field("iosDeviceId", string)
    }

    // Local UWP processes need to be allowed to accept incoming socket connections by calling
    // CheckNetIsolation LoopbackExempt -is -n={PackageName}
    private val unityLocalUwpStartInfo = structdef extends unityMonoStartInfoBase {
        field("packageName", string)
    }

    init {
        setting(Kotlin11Generator.Namespace, "com.jetbrains.rider.plugins.unity.model.debuggerWorker")

        // Give the debugger worker model a different namespace to JetBrains.Rider.Model as this has a zone requirement,
        // and we want to avoid zone consistency inspections in the debugger worker plugin (which obviously doesn't have
        // zones)
        setting(CSharp50Generator.Namespace, "JetBrains.Debugger.Model.Plugins.Unity")

        property("showCustomRenderers", bool)
        property("ignoreBreakOnUnhandledExceptionsForIl2Cpp", bool)
        property("forcedTimeoutForAdvanceUnityEvaluation", int)
        property("breakpointTraceOutput", int)
    }

    //structure of this model must be the same as TexturePixelsInfo Plugins/ReSharperUnity/debugger/texture-debugger/TextureUtils.cs
    val unityTextureInfo = classdef{
        field("width", int)
        field("height", int)
        field("pixels", immutableList(int))
        field("originalWidth", int)
        field("originalHeight", int)
        field("graphicsTextureFormat", string)
        field("textureName", string)
        field("hasAlphaChannel", bool)
    }

    var unityTextureAdditionalActionParams = structdef {
        field("evaluationTimeout", int)
        field("frameId", int)
    }

    var unityTextureAdditionalActionResult = classdef{
        field("error", string.nullable)
        field("unityTextureInfo", unityTextureInfo.nullable)
        field("isTerminated", bool)
    }

    val unityTexturePropertiesData = classdef extends DebuggerWorkerModel.additionalObjectPropertiesData {
        call("evaluateTexture", unityTextureAdditionalActionParams, unityTextureAdditionalActionResult)
    }

    val unityPausepointAdditionalDataModel = classdef extends DebuggerWorkerModel.breakpointAdditionalDataModel {
    }
}
