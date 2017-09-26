package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.configurations.ConfigurationType
import com.intellij.openapi.project.Project
import com.jetbrains.rider.run.configurations.DotNetConfigurationFactoryBase

open class UnityAttachToEditorFactory(type: ConfigurationType)
    : DotNetConfigurationFactoryBase<UnityAttachToEditorConfiguration>(type) {

    override fun createTemplateConfiguration(project: Project) = UnityAttachToEditorConfiguration(project, this)
    override fun isConfigurationSingletonByDefault() = true
}

class UnityAttachToEditorAndPlayFactory(type: ConfigurationType)
    : UnityAttachToEditorFactory(type) {

    override fun createTemplateConfiguration(project: Project) = UnityAttachToEditorConfiguration(project, this, true)
    override fun isConfigurationSingletonByDefault() = true
}
