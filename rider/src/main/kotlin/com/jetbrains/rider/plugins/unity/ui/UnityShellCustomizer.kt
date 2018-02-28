package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.components.AbstractProjectComponent
import com.intellij.openapi.project.Project

class UnityShellCustomizer(val project : Project) : AbstractProjectComponent(project) {
    override fun projectOpened() {
        super.projectOpened()
    }
}