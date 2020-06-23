package com.jetbrains.rider.plugins.unity.run.configurations.unityExe

import com.intellij.execution.Executor
import com.intellij.execution.configurations.ConfigurationFactory
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.executors.DefaultRunExecutor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.openapi.project.Project
import com.jetbrains.rider.run.configurations.dotNetExe.DotNetExeConfiguration
import com.jetbrains.rider.run.configurations.dotNetExe.DotNetExeConfigurationParameters
import com.jetbrains.rider.run.configurations.exe.ExeRunProfileState
import com.jetbrains.rider.run.configurations.remote.DotNetRemoteConfiguration

class UnityExeConfiguration(name:String, project: Project, factory: ConfigurationFactory, params: DotNetExeConfigurationParameters)
    : DotNetExeConfiguration(name, project, factory, params) {

    override fun getState(executor: Executor, environment: ExecutionEnvironment): RunProfileState{
        val executorId = executor.id

        if (executorId == DefaultRunExecutor.EXECUTOR_ID)
            return ExeRunProfileState(parameters, environment)
        if (executorId == DefaultDebugExecutor.EXECUTOR_ID)
        {
            // this.parameters.envs = this.parameters.envs.plus(Pair("UNITY_GIVE_CHANCE_TO_ATTACH_DEBUGGER", "1")) // https://github.com/JetBrains/resharper-unity/issues/388
            return UnityExeAttachProfileState(this, DotNetRemoteConfiguration(project, this.factory!!, name), environment)
        }

        return super.getState(executor, environment)
    }
}