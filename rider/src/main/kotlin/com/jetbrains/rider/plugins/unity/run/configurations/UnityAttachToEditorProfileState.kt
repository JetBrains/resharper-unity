package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.ExecutionResult
import com.intellij.execution.Executor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.execution.runners.ProgramRunner
import com.intellij.openapi.diagnostic.Logger
import com.jetbrains.rider.debugger.DebuggerWorkerProcessHandler
import com.jetbrains.rider.model.rdUnityModel
import com.jetbrains.rider.plugins.unity.run.UnityDebuggerOutputListener
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.run.IDebuggerOutputListener
import com.jetbrains.rider.run.configurations.remote.MonoConnectRemoteProfileState
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.Signal
import com.jetbrains.rd.util.reactive.adviseOnce

class UnityAttachToEditorProfileState(private val remoteConfiguration: UnityAttachToEditorRunConfiguration, executionEnvironment: ExecutionEnvironment)
    : MonoConnectRemoteProfileState(remoteConfiguration, executionEnvironment) {
    private val logger = Logger.getInstance(UnityAttachToEditorProfileState::class.java)
    private val debugAttached = Signal<Boolean>()
    private val project = executionEnvironment.project

    override fun execute(executor: Executor, runner: ProgramRunner<*>, workerProcessHandler: DebuggerWorkerProcessHandler, lifetime: Lifetime): ExecutionResult {
        val result = super.execute(executor, runner, workerProcessHandler)

        if (remoteConfiguration.play) {
            debugAttached.adviseOnce(lifetime) {
                if (it)
                {
                    logger.info("Pass value to backend, which will push Unity to enter play mode.")
                    lifetime.bracket(opening = {
                        // pass value to backend, which will push Unity to enter play mode.
                        executionEnvironment.project.solution.rdUnityModel.play.set(true)
                    }, terminationAction = {
                        executionEnvironment.project.solution.rdUnityModel.play.set(false)
                    })
                }
            }
        }

        return result
    }

    override fun getDebuggerOutputEventsListener(): IDebuggerOutputListener {
        debugAttached.fire(true)
        return UnityDebuggerOutputListener(project, remoteConfiguration.address, "Unity Editor", true)
    }
}