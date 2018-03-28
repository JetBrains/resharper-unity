package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.configurations.ConfigurationType
import com.intellij.openapi.project.Project
import com.jetbrains.rider.run.configurations.DotNetConfigurationFactoryBase

open class UnityAttachToEditorFactory(type: ConfigurationType)
    : DotNetConfigurationFactoryBase<UnityAttachToEditorRunConfiguration>(type) {

    override fun createTemplateConfiguration(project: Project) = UnityAttachToEditorRunConfiguration(project, this)
    override fun isConfigurationSingletonByDefault() = true
    override fun getName(): String {
        return "Unity Debug"
    }
}

class UnityAttachToEditorAndPlayFactory(type: ConfigurationType)
    : UnityAttachToEditorFactory(type) {

    override fun createTemplateConfiguration(project: Project) = UnityAttachToEditorRunConfiguration(project, this, true)
    override fun getName(): String {
        return "Unity Debug and Play"
    }
}
