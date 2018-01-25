package com.jetbrains.rider.plugins.unity.util

import com.intellij.openapi.util.IconLoader

class UnityIcons {
    companion object {
        val ShaderLabFile = IconLoader.getIcon("/Icons/Shader/Shader.png")

        // TODO: Proper icons!
        val AttachEditorDebugConfiguration = IconLoader.getIcon("/Icons/Logo/UnityLogo.png")

        val Logo = IconLoader.getIcon("/Icons/Logo/UnityLogo.png")

        val RefreshInUnity = IconLoader.getIcon("/Icons/UnityLogView/forceRefresh.png")
        val PlayInUnity = IconLoader.getIcon("/Icons/UnityLogView/progressResume.png")
        val PauseInUnity = IconLoader.getIcon("/Icons/UnityLogView/progressPause.png")
        val StopInUnity = IconLoader.getIcon("/Icons/UnityLogView/stop.png")
        val StepInUnity = IconLoader.getIcon("/Icons/UnityLogView/stop.png")
    }
}
