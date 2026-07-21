package com.jetbrains.rider.plugins.unity.run.configurations.unityExe

import com.intellij.execution.CantRunException
import com.intellij.execution.Executor
import com.intellij.execution.configurations.ConfigurationFactory
import com.intellij.execution.configurations.ConfigurationTypeUtil
import com.intellij.execution.configurations.RunConfiguration
import com.intellij.execution.configurations.RunProfileState
import com.intellij.execution.executors.DefaultDebugExecutor
import com.intellij.execution.runners.ExecutionEnvironment
import com.intellij.openapi.project.Project
import com.jetbrains.rider.debugger.IMixedModeDebugAwareRunProfile
import com.jetbrains.rider.multiPlatform.RiderMultiPlatformBundle
import com.jetbrains.rider.plugins.unity.model.frontendBackend.UnityScriptingBackend
import com.jetbrains.rider.plugins.unity.util.UnityPlayerRuntimeDetector
import com.jetbrains.rider.run.RiderRunBundle
import com.jetbrains.rider.run.configurations.exe.ExeConfiguration
import com.jetbrains.rider.run.configurations.exe.ExeConfigurationParameters
import com.jetbrains.rider.run.configurations.remote.DotNetRemoteConfiguration
import com.jetbrains.rider.run.configurations.remote.MonoRemoteConfigType
import com.jetbrains.rider.run.dotNetCore.DotNetCoreDebugProfile
import com.jetbrains.rider.runtime.DotNetExecutable
import com.jetbrains.rider.runtime.RiderDotNetActiveRuntimeHost
import org.jetbrains.concurrency.Promise
import java.nio.file.Path

class UnityExeConfiguration(name: String,
                            project: Project,
                            factory: ConfigurationFactory,
                            params: ExeConfigurationParameters)
    : ExeConfiguration(name, project, factory, params, true), IMixedModeDebugAwareRunProfile {

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

        if (executorId == DefaultDebugExecutor.EXECUTOR_ID){
            val backend = UnityPlayerRuntimeDetector.getInstance(project).detect(Path.of(parameters.exePath))
            return if (backend == UnityScriptingBackend.CoreCLR)
                getDotNetCoreDebugProfile(environment)
            else{
                val monoRemoteConfigFactory = ConfigurationTypeUtil.findConfigurationType(MonoRemoteConfigType::class.java).factory
                val remoteConfiguration = DotNetRemoteConfiguration(project, monoRemoteConfigFactory, name)
                UnityExeDebugProfileState(this, remoteConfiguration, environment)
            }
        }

        return super.getRunProfileStateAsync(executor, environment)
    }

    fun getDotNetCoreDebugProfile(environment:ExecutionEnvironment):DotNetCoreDebugProfile{
        val activeRuntimeHost = RiderDotNetActiveRuntimeHost.getInstance(environment.project)
        val dotNetCoreRuntime = activeRuntimeHost.dotNetCoreRuntime.value ?: throw CantRunException(
            RiderMultiPlatformBundle.message("rider.mac.unable.to.get.runtime.information.message"))
        return UnityDotNetCoreDebugProfile(dotNetCoreRuntime, toDotNetExecutable(), environment, dotNetCoreRuntime.cliExePath)
    }

    private fun toDotNetExecutable(): DotNetExecutable {
        // copy the env vars map in case someone might mutate it per run and we don't want to persist those changes between the runs
        // e.g., currently DpaExtension does this by adding entries to DOTNET_DiagnosticPorts, so giving it a copy prevents them from accumulating
        val envVars = parameters.envs.toMutableMap();
        return DotNetExecutable(parameters.exePath,
                                null,
                                parameters.workingDirectory,
                                parameters.programParameters,
                                parameters.terminalMode,
                                envVars,
                                false,
                                { _, _, _ -> },
                                null,
                                "",
                                true,
                                mixedModeDebugging = parameters.mixedModeDebugging
        )
    }

    @Suppress("UsagesOfObsoleteApi")
    @Deprecated("Please, override 'getRunProfileStateAsync' instead")
    override fun getStateAsync(executor: Executor, environment: ExecutionEnvironment): Promise<RunProfileState> {
        @Suppress("DEPRECATION")
        throw UnsupportedOperationException(RiderRunBundle.message("obsolete.synchronous.api.is.used.message", UnityExeConfiguration::getStateAsync.name))
    }

    override fun useMixedDebugMode(): Boolean = parameters.mixedModeDebugging
}
