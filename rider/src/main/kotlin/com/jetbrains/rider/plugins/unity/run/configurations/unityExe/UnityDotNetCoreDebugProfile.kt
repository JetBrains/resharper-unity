package com.jetbrains.rider.plugins.unity.run.configurations.unityExe

import com.intellij.execution.runners.ExecutionEnvironment
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.model.debuggerWorker.DebuggerWorkerModel
import com.jetbrains.rider.model.debuggerWorker.DotNetCoreExeStartInfoBase
import com.jetbrains.rider.model.debuggerWorker.DotNetCoreInfo
import com.jetbrains.rider.model.debuggerWorker.EncInfo
import com.jetbrains.rider.model.debuggerWorker.RdDebuggerPath
import com.jetbrains.rider.model.debuggerWorker.StringPair
import com.jetbrains.rider.plugins.unity.model.debuggerWorker.UnityDotNetCoreExeStartInfo
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.run.configurations.bindDebuggerWorkerSettings
import com.jetbrains.rider.plugins.unity.run.configurations.getUnityProjectData
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.run.dotNetCore.DotNetCoreDebugProfile
import com.jetbrains.rider.runtime.DotNetExecutable
import com.jetbrains.rider.runtime.dotNetCore.DotNetCoreRuntime
import java.nio.file.Path

internal class UnityDotNetCoreDebugProfile(
    dotNetRuntime: DotNetCoreRuntime,
    dotNetExecutable: DotNetExecutable,
    executionEnvironment: ExecutionEnvironment,
    currentDotNetCliExePath: Path
) : DotNetCoreDebugProfile(dotNetRuntime, dotNetExecutable, executionEnvironment, currentDotNetCliExePath) {

    override fun bindSettings(lifetime: Lifetime, workerModel: DebuggerWorkerModel) {
        executionEnvironment.project.solution.frontendBackendModel.bindDebuggerWorkerSettings(workerModel, lifetime)
        super.bindSettings(lifetime, workerModel)
    }

    override fun createDotNetCoreExeStartInfo(dotNetCoreInfo: DotNetCoreInfo, encInfo: EncInfo?, exePath: RdDebuggerPath, workingDirectory: RdDebuggerPath, arguments: String, environmentVariables: List<StringPair>, runtimeArguments: String, executeAsIs: Boolean): DotNetCoreExeStartInfoBase {
        val projectData = getUnityProjectData(executionEnvironment.project)
        return UnityDotNetCoreExeStartInfo(projectData, dotNetCoreInfo, encInfo, exePath, workingDirectory, arguments, environmentVariables, runtimeArguments, executeAsIs)
    }

}
