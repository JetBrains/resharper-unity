package com.jetbrains.resharper.plugins.unity.run.configurations

import com.intellij.execution.ExecutionResult
import com.intellij.execution.Executor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ProgramRunner
import com.intellij.openapi.project.Project
import com.jetbrains.resharper.debugger.DebuggerWorkerProcessHandler
import com.jetbrains.resharper.run.configurations.remote.MonoConnectRemoteProfileState

class UnityAttachToEditorProfileState(remoteConfiguration: UnityAttachToEditorConfiguration, executionEnvironment: ExecutionEnvironment)
    : MonoConnectRemoteProfileState(remoteConfiguration, executionEnvironment) {

    override fun execute(executor: Executor, runner: ProgramRunner<*>, workerProcessHandler: DebuggerWorkerProcessHandler): ExecutionResult {
        val result = super.execute(executor, runner, workerProcessHandler)
        //if (remoteConfiguration.play) {
            // TODO: Tell Unity Editor to switch to play mode
        //}
        return result
    }
}