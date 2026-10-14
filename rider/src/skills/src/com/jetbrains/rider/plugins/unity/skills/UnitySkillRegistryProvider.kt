package com.jetbrains.rider.plugins.unity.skills

import com.intellij.aiagentextensions.skills.extension.AgentSkillRegistryProvider
import com.intellij.openapi.project.Project
import com.jetbrains.rider.plugins.unity.isUnityProject
import java.net.URI

internal class UnitySkillRegistryProvider : AgentSkillRegistryProvider {
    override suspend fun collectRegistryLocations(project: Project): List<URI> {
        if (!project.isUnityProject.value) return emptyList()
        return listOf(UNITY_SKILL_REGISTRY)
    }
}

private val UNITY_SKILL_REGISTRY = URI("https://github.com/Unity-Technologies/skills/tree/main/skills")
