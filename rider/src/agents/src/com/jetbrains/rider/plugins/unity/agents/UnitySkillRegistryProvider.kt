package com.jetbrains.rider.plugins.unity.agents

import com.intellij.aiagentextensions.skills.SkillWorkspaceContext
import com.intellij.aiagentextensions.skills.extension.AgentSkillRegistryProvider
import com.jetbrains.rider.plugins.unity.isUnityProject
import java.net.URI

internal const val UNITY_SKILL_REGISTRY_SOURCE = "Unity-Technologies/skills"
internal val UNITY_SKILL_REGISTRY = URI("https://github.com/$UNITY_SKILL_REGISTRY_SOURCE/tree/main/skills")

internal class UnitySkillRegistryProvider : AgentSkillRegistryProvider {
    override suspend fun collectRegistryLocations(context: SkillWorkspaceContext): List<URI> {
        val project = context.project ?: return emptyList()
        if (!project.isUnityProject.value) return emptyList()
        return listOf(UNITY_SKILL_REGISTRY)
    }
}
