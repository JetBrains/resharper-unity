package com.jetbrains.rider.plugins.unity.run.configurations.unityExe

import com.intellij.execution.Executor
import com.intellij.execution.configurations.ConfigurationFactory
import com.intellij.execution.configurations.ConfigurationTypeUtil
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.openapi.project.Project
import com.jetbrains.rider.run.configurations.exe.ExeConfiguration
import com.jetbrains.rider.run.configurations.exe.ExeConfigurationParameters
import com.jetbrains.rider.run.configurations.remote.DotNetRemoteConfiguration
import com.jetbrains.rider.run.configurations.remote.MonoRemoteConfigType

class UnityExeConfiguration(name:String, project: Project, factory: ConfigurationFactory, params: ExeConfigurationParameters)
    : ExeConfiguration(name, project, factory, params) {

    override fun isNative(): Boolean {
        return false
    }

    override fun getState(executor: Executor, environment: ExecutionEnvironment): RunProfileState{
        val executorId = executor.id

        if (executorId == DefaultDebugExecutor.EXECUTOR_ID)
        {
            // this.parameters.envs = this.parameters.envs.plus(Pair("UNITY_GIVE_CHANCE_TO_ATTACH_DEBUGGER", "1")) // https://github.com/JetBrains/resharper-unity/issues/388
            return UnityExeDebugProfileState(this, DotNetRemoteConfiguration(project, ConfigurationTypeUtil.findConfigurationType(MonoRemoteConfigType::class.java).factory, name), environment)
        }

        return super.getState(executor, environment)
    }
}