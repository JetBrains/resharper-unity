package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.ExecutionResult
import com.intellij.execution.Executor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ProgramRunner
import com.intellij.execution.ui.ObservableConsoleView
import com.intellij.notification.NotificationListener
import com.intellij.notification.NotificationType
import com.intellij.openapi.Disposable
import com.intellij.openapi.diagnostic.Logger
import com.intellij.xdebugger.XDebuggerManager
import com.intellij.xdebugger.impl.XDebuggerManagerImpl
import com.jetbrains.rider.debugger.DebuggerWorkerProcessHandler
import com.jetbrains.rider.model.rdUnityModel
//import com.jetbrains.rider.run.IDebuggerOutputListener
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.run.configurations.remote.MonoConnectRemoteProfileState
import com.jetbrains.rider.util.idea.pumpMessages
import com.jetbrains.rider.util.lifetime.Lifetime
import com.jetbrains.rider.util.reactive.ISignal
import com.jetbrains.rider.util.reactive.Signal
import com.jetbrains.rider.util.reactive.adviseOnce
import java.time.LocalDateTime

class UnityAttachToEditorProfileState(val remoteConfiguration: UnityAttachToEditorConfiguration, executionEnvironment: ExecutionEnvironment)
    : MonoConnectRemoteProfileState(remoteConfiguration, executionEnvironment) {
    val logger = Logger.getInstance(UnityAttachToEditorProfileState::class.java)

    val debugAttached = Signal<Boolean>()

    override fun execute(executor: Executor, runner: ProgramRunner<*>, workerProcessHandler: DebuggerWorkerProcessHandler, sessionLifetime: Lifetime): ExecutionResult {
        val result = super.execute(executor, runner, workerProcessHandler)

        if (remoteConfiguration.play) {
            debugAttached.adviseOnce(sessionLifetime) {
                logger.info("Pass value to backend, which will push Unity to enter play mode.")
                sessionLifetime.bracket(opening = {
                    // pass value to backend, which will push Unity to enter play mode.
                    executionEnvironment.project.solution.rdUnityModel.data["UNITY_Play"] = true.toString();
                }, closing = {
                    executionEnvironment.project.solution.rdUnityModel.data["UNITY_Play"] = false.toString()
                })
            }
        }

        return result
    }
//
//    override fun getDebuggerOutputEventsListener(): IDebuggerOutputListener {
//        return DebuggerOutputListener(executionEnvironment.project, debugAttached)
//    }
}