package com.jetbrains.rider.plugins.unity.profiler.toolWindow

import com.intellij.openapi.application.EDT
import com.intellij.openapi.project.Project
import com.intellij.openapi.startup.ProjectActivity
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch

/**
 * Makes Unity Profiler tool window available when a Unity project is loaded.
 */
internal class UnityProfilerToolWindowStartupActivity : ProjectActivity {
    override suspend fun execute(project: Project) {
        val lifetime = UnityProjectLifetimeService.getLifetime(project)

        UnityProjectDiscoverer.getInstance(project).isUnityProject.advise(lifetime) { isUnity ->
            if (isUnity) {
                lifetime.coroutineScope.launch(Dispatchers.EDT) {
                    UnityProfilerToolWindowFactory.makeAvailable(project)
                }
            }
        }
    }
}
