package com.jetbrains.rider.plugins.unity.profiler.toolWindow

import com.intellij.openapi.components.service
import com.intellij.openapi.project.Project
import com.intellij.openapi.startup.ProjectActivity
import com.jetbrains.rd.util.reactive.adviseUntil
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.profiler.UnityProfilerUsagesDaemon

/**
 * Makes Unity Profiler tool window available when a Unity project is loaded, and ensures
 * [UnityProfilerUsagesDaemon] is initialized so its currentSnapshot advise is registered
 * before profiling data arrives.
 */
internal class UnityProfilerToolWindowStartupActivity : ProjectActivity {
    override suspend fun execute(project: Project) {
        val lifetime = UnityProjectLifetimeService.getLifetime(project)

        UnityProjectDiscoverer.getInstance(project).isUnityProject.adviseUntil(lifetime) { isUnity ->
            if (isUnity) {
                UnityProfilerToolWindowFactory.makeAvailable(project)
                // Eagerly initialize the daemon so currentSnapshot.advise is registered.
                // Without this, auto-open won't fire if profiling data arrives before any
                // file is opened (which happens when the test runs in isolation, or when a
                // recording is loaded before navigating to source).
                project.service<UnityProfilerUsagesDaemon>()
            }
            isUnity
        }
    }
}
