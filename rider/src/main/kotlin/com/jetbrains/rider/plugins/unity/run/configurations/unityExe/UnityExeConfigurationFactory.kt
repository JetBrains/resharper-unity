package com.jetbrains.rider.plugins.unity.run.configurations.unityExe

import com.intellij.execution.BeforeRunTask
import com.intellij.execution.configurations.ConfigurationFactory
import com.intellij.execution.configurations.ConfigurationType
import com.intellij.execution.configurations.RunConfiguration
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Key
import com.jetbrains.rider.build.tasks.BuildSolutionBeforeRunTask
import com.jetbrains.rider.build.tasks.BuildSolutionBeforeRunTaskProvider
import org.jetbrains.annotations.NotNull

class UnityExeConfigurationFactory(type: ConfigurationType) : ConfigurationFactory(type) {
    override fun getId(): String {
        // super.getId() does the same, but prints a deprecation message
        return name
    }

    override fun configureBeforeRunTaskDefaults(providerID: Key<out BeforeRunTask<BeforeRunTask<*>>>?,
                                                task: BeforeRunTask<out BeforeRunTask<*>>?) {
        if (providerID == BuildSolutionBeforeRunTaskProvider.providerId && task is BuildSolutionBeforeRunTask) {
            task.isEnabled = false
        }
    }

    override fun isConfigurationSingletonByDefault(): Boolean {
        return true
    }

    private fun createParameters(): UnityExeConfigurationParameters {
        return UnityExeConfigurationParameters(
            "",
            "",
            "",
            hashMapOf(),
            false,
            false
        )
    }

    override fun createConfiguration(name: String?, template: RunConfiguration): RunConfiguration =
        UnityExeConfiguration(name ?: "Unity Executable", template.project, this, createParameters())

    override fun createTemplateConfiguration(@NotNull project: Project): RunConfiguration =
        UnityExeConfiguration("Unity Executable", project, this, createParameters())
}
