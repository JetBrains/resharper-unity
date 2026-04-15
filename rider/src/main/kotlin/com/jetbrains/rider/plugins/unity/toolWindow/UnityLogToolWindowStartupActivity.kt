package com.jetbrains.rider.plugins.unity.toolWindow

import com.intellij.openapi.project.Project
import com.intellij.openapi.startup.ProjectActivity
import com.jetbrains.rd.util.reactive.adviseUntil
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService

internal class UnityLogToolWindowStartupActivity : ProjectActivity {
    override suspend fun execute(project: Project) {
        val lifetime = UnityProjectLifetimeService.getLifetime(project)
        UnityProjectDiscoverer.getInstance(project).isUnityProject.adviseUntil(lifetime) { isUnity ->
            if (isUnity) {
                UnityToolWindowFactory.makeAvailable(project)
            }
            isUnity
        }
    }
}
