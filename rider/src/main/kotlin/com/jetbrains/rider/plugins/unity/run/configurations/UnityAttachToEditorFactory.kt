package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.configurations.ConfigurationType
import com.intellij.openapi.project.Project
import icons.UnityIcons
import com.jetbrains.rider.run.configurations.DotNetConfigurationFactoryBase

open class UnityAttachToEditorFactory(type: ConfigurationType)
    : DotNetConfigurationFactoryBase<UnityAttachToEditorRunConfiguration>(type) {

    override fun createTemplateConfiguration(project: Project) = UnityAttachToEditorRunConfiguration(project, this)
    override fun isConfigurationSingletonByDefault() = true
    override fun getName() = "Debug Unity Editor"
    override fun getIcon() = UnityIcons.RunConfigurations.AttachAndDebug

    // This value gets written to the config file. By default it defers to getName, which is what happened pre-2018.3.
    // Keep the "Unity Debug" value so that we can load configs created by earlier versions, and earlier versions can
    // load this config
    override fun getId() = "Unity Debug"
}

class UnityAttachToEditorAndPlayFactory(type: ConfigurationType)
    : DotNetConfigurationFactoryBase<UnityAttachToEditorRunConfiguration>(type) {

    override fun createTemplateConfiguration(project: Project) = UnityAttachToEditorRunConfiguration(project, this, true)
    override fun isConfigurationSingletonByDefault() = true
    override fun getName() = "Debug Unity Editor (play mode)"
    override fun getId() = "UNITY_ATTACH_AND_PLAY"
    override fun getIcon() = UnityIcons.RunConfigurations.AttachDebugAndPlay
}
