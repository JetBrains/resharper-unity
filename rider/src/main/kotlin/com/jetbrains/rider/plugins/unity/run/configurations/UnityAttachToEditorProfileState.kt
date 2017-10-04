package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.ExecutionResult
import com.intellij.execution.Executor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ProgramRunner
import com.jetbrains.rider.debugger.DebuggerWorkerProcessHandler
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.run.configurations.remote.MonoConnectRemoteProfileState
import com.jetbrains.rider.util.idea.getLogger

class UnityAttachToEditorProfileState(val remoteConfiguration: UnityAttachToEditorConfiguration, executionEnvironment: ExecutionEnvironment)
    : MonoConnectRemoteProfileState(remoteConfiguration, executionEnvironment) {
    private val logger = getLogger<UnityAttachToEditorProfileState>()

    override fun execute(executor: Executor, runner: ProgramRunner<*>, workerProcessHandler: DebuggerWorkerProcessHandler): ExecutionResult {
        val result = super.execute(executor, runner, workerProcessHandler)

        if (remoteConfiguration.play) {
            logger.info("Pass value to backend, which will push Unity to enter play mode.")
            //JOptionPane.showMessageDialog(null, text, "InfoBox: " + "", JOptionPane.INFORMATION_MESSAGE);

            executionEnvironment.project.solution.customData.data["UNITY_ProcessId"] = remoteConfiguration.pid!!.toString()
            // pass value to backend, which will push Unity to enter play mode.
            executionEnvironment.project.solution.customData.data["UNITY_AttachEditorAndRun"] = "true";
        }

        return result
    }
}