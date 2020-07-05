package com.jetbrains.rider.plugins.unity.run

import com.intellij.openapi.project.Project
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.onTermination

class UnityDebuggableProcessListener(project: Project, lifetime: Lifetime,
                                     onProcessAdded: (UnityProcess) -> Unit,
                                     onProcessRemoved: (UnityProcess) -> Unit) {

    private val editorListener: UnityEditorListener = UnityEditorListener(project, onProcessAdded, onProcessRemoved)
    private val playerListener: UnityPlayerListener = UnityPlayerListener(onProcessAdded, onProcessRemoved)
    private val appleDeviceListener: AppleDeviceListener = AppleDeviceListener(project, onProcessAdded, onProcessRemoved)

    init {
        lifetime.onTermination {
            editorListener.stop()
            playerListener.stop()
            appleDeviceListener.stop()
        }
    }
}