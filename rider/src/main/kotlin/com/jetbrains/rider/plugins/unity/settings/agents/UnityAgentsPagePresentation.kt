package com.jetbrains.rider.plugins.unity.settings.agents

data class UnityAgentsPagePresentation(
    val cli: UnityCliPresentation,
    val pipeline: UnityPipelinePresentation,
    /** Null when the Unity MCP section is switched off. */
    val mcp: UnityMcpSectionPresentation?,
)
