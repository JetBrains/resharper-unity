package com.jetbrains.resharper.plugins.unity.run.configurations

import com.intellij.execution.configurations.ConfigurationType
import com.intellij.openapi.project.Project
import com.jetbrains.resharper.run.configurations.DotNetConfigurationFactoryBase

class UnityAttachToEditorFactory(type: ConfigurationType)
    : DotNetConfigurationFactoryBase<UnityAttachToEditorConfiguration>(type) {

    override fun createTemplateConfiguration(project: Project) = UnityAttachToEditorConfiguration(project, this)
    override fun isConfigurationSingletonByDefault() = true
}
