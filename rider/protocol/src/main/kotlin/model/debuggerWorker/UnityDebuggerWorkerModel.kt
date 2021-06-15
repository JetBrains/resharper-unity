package model.debuggerWorker

import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*
import com.jetbrains.rd.generator.nova.csharp.CSharp50Generator
import com.jetbrains.rd.generator.nova.kotlin.Kotlin11Generator
import com.jetbrains.rider.model.nova.debugger.main.DebuggerWorkerModel

@Suppress("unused")
object UnityDebuggerWorkerModel : Ext(DebuggerWorkerModel) {

    // Not used in this model, but referenced via debuggerStartInfoBase. Serialisers will be registered along with this
    // model (directly via UnityDebuggerWorkerModel.RegisterDeclaredTypesSerializers() or indirectly via creating a new
    // UnityDebuggerWorkerModel)
    private val unityStartInfoBase = basestruct extends DebuggerWorkerModel.debuggerStartInfoBase {
        field("monoAddress", string.nullable)
        field("monoPort", int)
        field("listenForConnections", bool)
    }

    // Default start info. Performs the same as MonoAttachStartInfo but allows overriding some options for IL2CPP
    private val unityStartInfo = structdef extends unityStartInfoBase {
    }

    // Start the iOS USB debugging proxy before attaching
    private val unityIosUsbStartInfo = structdef extends unityStartInfoBase {
        field("iosSupportPath", string)
        field("iosDeviceId", string)
    }

    // Local UWP processes need to be allowed to accept incoming socket connections by calling
    // CheckNetIsolation LoopbackExempt -is -n={PackageName}
    private val unityLocalUwpStartInfo = structdef extends unityStartInfoBase {
        field("packageName", string)
    }

    init {
        setting(Kotlin11Generator.Namespace, "com.jetbrains.rider.model.unity.debuggerWorker")
        setting(CSharp50Generator.Namespace, "JetBrains.Rider.Model.Unity.DebuggerWorker")

        property("showCustomRenderers", bool)
        property("ignoreBreakOnUnhandledExceptionsForIl2Cpp", bool)
    }
}