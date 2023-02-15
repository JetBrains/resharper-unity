package com.jetbrains.rider.plugins.unity.run

import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.lifetime.Lifetime

class UnityDebuggableProcessListener(project: Project, lifetime: Lifetime,
                                     onProcessAdded: (UnityProcess) -> Unit,
                                     onProcessRemoved: (UnityProcess) -> Unit) {
    init {
        UnityEditorListener(project, lifetime, onProcessAdded, onProcessRemoved)
        UnityPlayerListener(lifetime, onProcessAdded, onProcessRemoved)
        AppleDeviceListener(project, lifetime, onProcessAdded, onProcessRemoved)
        AndroidDeviceListener(project, lifetime, onProcessAdded, onProcessRemoved)
    }
}