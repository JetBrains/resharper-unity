 package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.runners.ExecutionEnvironment
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.model.debuggerWorker.DebuggerStartInfoBase
import com.jetbrains.rider.model.debuggerWorker.DotNetCoreAttachStartInfo
import com.jetbrains.rider.run.AttachDebugProfileStateBase
import com.jetbrains.rider.run.ConsoleKind

class UnityCorAttachDebugProfileState(val processId: Int, executionEnvironment: ExecutionEnvironment) : AttachDebugProfileStateBase(
    executionEnvironment) {
    override val consoleKind: ConsoleKind = ConsoleKind.AttachedProcess
    override val attached: Boolean = true
    override val remoteDebug: Boolean = false

    override suspend fun createModelStartInfo(lifetime: Lifetime): DebuggerStartInfoBase = DotNetCoreAttachStartInfo(processId)
}