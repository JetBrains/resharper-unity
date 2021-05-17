package model.debuggerWorker

import com.jetbrains.rd.generator.nova.*
import com.jetbrains.rd.generator.nova.PredefinedType.*
import com.jetbrains.rd.generator.nova.csharp.CSharp50Generator
import com.jetbrains.rd.generator.nova.kotlin.Kotlin11Generator
import com.jetbrains.rider.model.nova.debugger.main.DebuggerWorkerModel

@Suppress("unused")
class UnityDebuggerWorkerModel : Ext(DebuggerWorkerModel) {
    init {
        setting(Kotlin11Generator.Namespace, "com.jetbrains.rider.model.unity.debuggerWorker")
        setting(CSharp50Generator.Namespace, "JetBrains.Rider.Model.Unity.DebuggerWorker")

        property("showCustomRenderers", bool)
    }
}