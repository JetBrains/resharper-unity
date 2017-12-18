package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.ExecutionResult
import com.intellij.execution.Executor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ProgramRunner
import com.intellij.execution.ui.ConsoleViewContentType
import com.intellij.execution.ui.ObservableConsoleView
import com.intellij.openapi.Disposable
import com.intellij.openapi.diagnostic.Logger
import com.intellij.xdebugger.XDebuggerManager
import com.jetbrains.rider.debugger.DebuggerWorkerProcessHandler
import com.jetbrains.rider.debugger.DotNetDebugProcess
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.run.configurations.remote.MonoConnectRemoteProfileState
import com.jetbrains.rider.util.idea.pumpMessages
import com.jetbrains.rider.util.lifetime.Lifetime
import java.time.LocalDateTime

class UnityAttachToEditorProfileState(val remoteConfiguration: UnityAttachToEditorConfiguration, executionEnvironment: ExecutionEnvironment)
    : MonoConnectRemoteProfileState(remoteConfiguration, executionEnvironment) {
    val logger = Logger.getInstance(UnityAttachToEditorProfileState::class.java)

    override fun execute(executor: Executor, runner: ProgramRunner<*>, workerProcessHandler: DebuggerWorkerProcessHandler, lifetime: Lifetime): ExecutionResult {
        val result = super.execute(executor, runner, workerProcessHandler)

//        var debuggerManager = executionEnvironment.project.getComponent(XDebuggerManager::class.java)
//
//        val debugProcess = debuggerManager.currentSession!!.debugProcess as DotNetDebugProcess
//        val disposable:com.intellij.openapi.Disposable = Disposable {  }
//        var time = LocalDateTime.now();
//        debugProcess.debuggerOutputConsole.addChangeListener(ObservableConsoleView.ChangeListener {
//            time = LocalDateTime.now();
//        }, disposable)
//
//        pumpMessages(1000) {
//            LocalDateTime.now().isBefore(time.plusSeconds(1))
//        }

        if (remoteConfiguration.play) {
            logger.info("Pass value to backend, which will push Unity to enter play mode.")
            lifetime.bracket(opening = {
                // pass value to backend, which will push Unity to enter play mode.
                executionEnvironment.project.solution.customData.data["UNITY_AttachEditorAndPlay"] = "true";
            }, closing = {
                executionEnvironment.project.solution.customData.data["UNITY_AttachEditorAndPlay"] = "false"
            })
        }

        return result
    }
}