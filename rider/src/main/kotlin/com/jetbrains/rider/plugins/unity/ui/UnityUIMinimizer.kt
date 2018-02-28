package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.actionSystem.ex.ActionManagerEx
import com.intellij.openapi.project.Project
import com.intellij.openapi.startup.StartupActivity
import com.intellij.openapi.wm.ToolWindowAnchor
import com.intellij.openapi.wm.ToolWindowManager

class UnityUIMinimizer : StartupActivity {
    override fun runActivity(project: Project) {
        ensureMinimizedUI(project)
    }

    private fun ensureMinimizedUI(project: Project) {
        val toolWindowManager = ToolWindowManager.getInstance(project)
        toolWindowManager.unregisterToolWindow("NuGet")
        toolWindowManager.unregisterToolWindow("Database")
        toolWindowManager.unregisterToolWindow("Docker")
        toolWindowManager.unregisterToolWindow("Application Servers")
        toolWindowManager.unregisterToolWindow("Remote Host")

        //ActionManagerEx.getInstanceEx().unregisterAction("ActiveConfiguration")
    }

    private fun recoverFullUI(project: Project) {
        val toolWindowManager = ToolWindowManager.getInstance(project)
        toolWindowManager.registerToolWindow("NuGet", true, ToolWindowAnchor.BOTTOM)
        toolWindowManager.registerToolWindow("Database", true, ToolWindowAnchor.RIGHT)
        toolWindowManager.registerToolWindow("Docker", true, ToolWindowAnchor.RIGHT)
        toolWindowManager.registerToolWindow("Application Servers", true, ToolWindowAnchor.RIGHT)
        toolWindowManager.registerToolWindow("Remote Host", true, ToolWindowAnchor.RIGHT)

        //ActionManagerEx.getInstanceEx().unregisterAction("ActiveConfiguration")
    }
}