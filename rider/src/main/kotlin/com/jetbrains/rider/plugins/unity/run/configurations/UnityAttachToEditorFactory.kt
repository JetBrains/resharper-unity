package com.jetbrains.rider.plugins.unity.run.configurations

import com.intellij.execution.configurations.ConfigurationType
import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.unity.UnityBundle
import icons.UnityIcons


open class UnityAttachToEditorFactory(type: ConfigurationType) : UnityConfigurationFactoryBase(type) {

    override fun createTemplateConfiguration(project: Project) = UnityAttachToEditorRunConfiguration(project, this)
    override fun isConfigurationSingletonByDefault() = true
    override fun getName() = UnityBundle.message("debug.unity.editor")
    override fun getIcon() = UnityIcons.RunConfigurations.AttachAndDebug

    // This value gets written to the config file. By default it defers to getName, which is what happened pre-2018.3.
    // Keep the "Unity Debug" value so that we can load configs created by earlier versions, and earlier versions can
    // load this config
    override fun getId() = "Unity Debug"
}

class UnityAttachToEditorAndPlayFactory(type: ConfigurationType) : UnityConfigurationFactoryBase(type) {

    override fun createTemplateConfiguration(project: Project) = UnityAttachToEditorRunConfiguration(project, this, true)
    override fun isConfigurationSingletonByDefault() = true
    override fun getName() = UnityBundle.message("name.debug.unity.editor.play.mode")
    override fun getId() = "UNITY_ATTACH_AND_PLAY"
    override fun getIcon() = UnityIcons.RunConfigurations.AttachDebugAndPlay
}
