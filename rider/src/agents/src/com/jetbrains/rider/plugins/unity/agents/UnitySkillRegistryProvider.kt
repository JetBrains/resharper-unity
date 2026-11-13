package com.jetbrains.rider.plugins.unity.agents

import com.intellij.mcpserver.skills.SkillRegistryProvider
import com.intellij.mcpserver.skills.SkillWorkspaceContext
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.settings.agents.UNITY_SKILL_REGISTRY_SOURCE
import java.net.URI

internal val UNITY_SKILL_REGISTRY = URI("https://github.com/$UNITY_SKILL_REGISTRY_SOURCE/tree/main/skills")

internal class UnitySkillRegistryProvider : SkillRegistryProvider {
    override suspend fun registryLocations(context: SkillWorkspaceContext): List<URI> {
        val project = context.project ?: return emptyList()
        if (!project.isUnityProject.value) return emptyList()
        return listOf(UNITY_SKILL_REGISTRY)
    }
}
