package com.jetbrains.rider.plugins.unity.profiler.toolWindow

import com.intellij.openapi.application.EDT
import com.intellij.openapi.project.Project
import com.intellij.openapi.startup.ProjectActivity
import com.jetbrains.rider.plugins.unity.isUnityProject
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext

/**
 * Makes Unity Profiler tool window available when a Unity project is loaded.
 */
internal class UnityProfilerToolWindowStartupActivity : ProjectActivity {
    override suspend fun execute(project: Project) {
        if (!project.isUnityProject.value) return
        
        withContext(Dispatchers.EDT) {
            UnityProfilerToolWindowFactory.makeAvailable(project)
        }
    }
}
