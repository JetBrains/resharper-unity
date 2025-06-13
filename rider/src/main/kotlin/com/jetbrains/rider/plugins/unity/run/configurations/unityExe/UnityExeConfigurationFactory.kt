package com.jetbrains.rider.plugins.unity.run.configurations.unityExe

import com.intellij.execution.BeforeRunTask
import com.intellij.execution.configurations.ConfigurationType
import com.intellij.execution.configurations.RunConfiguration
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Key
import com.jetbrains.rider.build.tasks.BuildSolutionBeforeRunTask
import com.jetbrains.rider.build.tasks.BuildSolutionBeforeRunTaskProvider
import com.jetbrains.rider.plugins.unity.run.configurations.UnityConfigurationFactoryBase
import com.jetbrains.rider.run.configurations.exe.ExeConfigurationParameters
import org.jetbrains.annotations.NotNull

class UnityExeConfigurationFactory(type: ConfigurationType) : UnityConfigurationFactoryBase(type) {
    override fun getId(): String {
        // For compatibility with Rider 2020.1
        return "Unity Executable"
    }

    override fun configureBeforeRunTaskDefaults(providerID: Key<out BeforeRunTask<BeforeRunTask<*>>>?,
                                                task: BeforeRunTask<out BeforeRunTask<*>>?) {
        if (providerID == BuildSolutionBeforeRunTaskProvider.providerId && task is BuildSolutionBeforeRunTask) {
            task.isEnabled = false
        }
    }

    override fun createTemplateConfiguration(@NotNull project: Project): RunConfiguration =
        UnityExeConfiguration(this.name, project, this, ExeConfigurationParameters(
            exePath = "",
            programParameters = "",
            workingDirectory = "",
            envs = hashMapOf(),
            isPassParentEnvs = true,
            useExternalConsole = false,
            mixedModeDebugging = false
        ))
}
