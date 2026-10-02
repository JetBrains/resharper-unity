package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.openapi.options.Configurable
import com.intellij.openapi.options.ConfigurableProvider
import com.intellij.openapi.project.Project

class UnityForAgentsConfigurableProvider(private val project: Project) : ConfigurableProvider() {
    override fun canCreateConfigurable(): Boolean = UnityForAgentsFeature.isEnabled() && !project.isDefault

    override fun createConfigurable(): Configurable = UnityForAgentsConfigurable(project)
}
