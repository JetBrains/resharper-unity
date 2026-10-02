package com.jetbrains.rider.plugins.unity.settings.agents

import com.intellij.openapi.components.Service
import com.intellij.openapi.components.service
import com.intellij.util.concurrency.annotations.RequiresEdt
import java.util.concurrent.CopyOnWriteArrayList
import javax.swing.JComponent

@Service(Service.Level.APP)
class UnityForAgentsPageVisibility {
    private val pages = CopyOnWriteArrayList<JComponent>()

    fun register(page: JComponent) {
        pages.add(page)
    }

    fun unregister(page: JComponent) {
        pages.remove(page)
    }

    @RequiresEdt
    fun isAnyPageShowing(): Boolean = pages.any { it.isVisible && it.isShowing }

    companion object {
        fun getInstance(): UnityForAgentsPageVisibility = service()
    }
}
