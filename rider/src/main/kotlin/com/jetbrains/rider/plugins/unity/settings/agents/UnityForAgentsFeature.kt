package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.openapi.util.registry.Registry

/**
 * The kill switch of the Unity for Agents feature.
 */
object UnityForAgentsFeature {
    const val ENABLED_KEY: String = "rider.unity.agents.enabled"

    const val SUGGEST_CLI_INSTALL_KEY: String = "rider.unity.suggest.cli.install"

    const val MCP_SECTION_KEY: String = "rider.unity.agents.mcp.enabled"

    fun isEnabled(): Boolean = Registry.`is`(ENABLED_KEY, false)

    fun isCliSuggestionEnabled(): Boolean = isEnabled() && Registry.`is`(SUGGEST_CLI_INSTALL_KEY, true)

    fun isMcpSectionEnabled(): Boolean = isEnabled() && Registry.`is`(MCP_SECTION_KEY)
}
