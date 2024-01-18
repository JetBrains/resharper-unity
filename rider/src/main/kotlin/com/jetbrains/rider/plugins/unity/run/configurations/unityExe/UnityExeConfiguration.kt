package com.jetbrains.rider.plugins.unity.run.configurations.unityExe

import com.intellij.execution.Executor
import com.intellij.execution.configurations.ConfigurationFactory
import com.intellij.execution.configurations.ConfigurationTypeUtil
import com.intellij.execution.configurations.RunConfiguration
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.openapi.project.Project
import com.jetbrains.rider.run.RiderRunBundle
import com.jetbrains.rider.run.configurations.exe.ExeConfiguration
import com.jetbrains.rider.run.configurations.exe.ExeConfigurationParameters
import com.jetbrains.rider.run.configurations.remote.DotNetRemoteConfiguration
import com.jetbrains.rider.run.configurations.remote.MonoRemoteConfigType
import org.jetbrains.concurrency.Promise

class UnityExeConfiguration(name: String,
                            project: Project,
                            factory: ConfigurationFactory,
                            params: ExeConfigurationParameters)
    : ExeConfiguration(name, project, factory, params) {

    override fun isNative(): Boolean {
        return false
    }

    override fun clone(): RunConfiguration {
        val newConfiguration = UnityExeConfiguration(name, project, factory!!, parameters.copy())
        newConfiguration.doCopyOptionsFrom(this)
        copyCopyableDataTo(newConfiguration)
        return newConfiguration
    }

    override suspend fun getRunProfileStateAsync(executor: Executor, environment: ExecutionEnvironment): RunProfileState {
        val executorId = executor.id

        if (executorId == DefaultDebugExecutor.EXECUTOR_ID)
            return UnityExeDebugProfileState(this, DotNetRemoteConfiguration(project, ConfigurationTypeUtil.findConfigurationType(
                MonoRemoteConfigType::class.java).factory, name), environment)

        return super.getRunProfileStateAsync(executor, environment)
    }

    @Suppress("UsagesOfObsoleteApi")
    @Deprecated("Please, override 'getRunProfileStateAsync' instead")
    override fun getStateAsync(executor: Executor, environment: ExecutionEnvironment): Promise<RunProfileState> {
        @Suppress("DEPRECATION")
        throw UnsupportedOperationException(RiderRunBundle.message("obsolete.synchronous.api.is.used.message", UnityExeConfiguration::getStateAsync.name))
    }
}
