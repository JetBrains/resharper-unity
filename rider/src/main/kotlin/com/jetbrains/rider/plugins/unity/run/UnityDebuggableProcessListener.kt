package com.jetbrains.rider.plugins.unity.run

import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.lifetime.Lifetime

class UnityDebuggableProcessListener(project: Project, lifetime: Lifetime,
                                     onProcessAdded: (UnityProcess) -> Unit,
                                     onProcessRemoved: (UnityProcess) -> Unit) {

    init {
        UnityEditorListener().startListening(project, lifetime, onProcessAdded, onProcessRemoved)
        UnityPlayerListener().startListening(lifetime, onProcessAdded, onProcessRemoved)
        AppleDeviceListener(project, lifetime, onProcessAdded, onProcessRemoved)
        AndroidDeviceListener().startListening(project, lifetime, onProcessAdded, onProcessRemoved)
        UnityRunManagerListener().startListening(project, lifetime, onProcessAdded, onProcessRemoved)
    }
}