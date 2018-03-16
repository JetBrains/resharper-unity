package com.jetbrains.rider.plugins.unity.util

import com.intellij.openapi.util.IconLoader

class UnityIcons {
    class Icons {
        companion object {
            @JvmField
            val ShaderLabFile = IconLoader.getIcon("/Icons/Shader/Shader.png")

            // TODO: Proper icons!
            @JvmField
            val AttachEditorDebugConfiguration = IconLoader.getIcon("/Icons/Logo/UnityLogo.png")
        }
    }

    class Unity {
        companion object {
            @JvmField
            val UnityEdit = IconLoader.getIcon("/Icons/_UNITY_/UnityEdit.svg")
            @JvmField
            val UnityPlay = IconLoader.getIcon("/Icons/_UNITY_/UnityPlay.svg")
        }
    }

    class Actions {
        companion object {
            @JvmField
            val Execute = IconLoader.getIcon("/Icons/actions/execute.svg")
            @JvmField
            val ForceRefresh = IconLoader.getIcon("/Icons/actions/forceRefresh.svg")
            @JvmField
            val GC = IconLoader.getIcon("/Icons/actions/gc.svg")
            @JvmField
            val Pause = IconLoader.getIcon("/Icons/actions/pause.svg")
            @JvmField
            val SplitHorizontally = IconLoader.getIcon("/Icons/actions/splitHorizontally.svg")
            @JvmField
            val ToggleSoftWrap = IconLoader.getIcon("/Icons/actions/toggleSoftWrap.svg")
            @JvmField
            val Step = IconLoader.getIcon("/Icons/actions/step.svg")
        }
    }

    class General {
        companion object {
            @JvmField
            val Settings = IconLoader.getIcon("/Icons/general/settings.svg")
        }
    }

    class Ide {
        companion object {
            @JvmField
            val Settings = IconLoader.getIcon("/Icons/ide/warning.svg")
            @JvmField
            val Info = IconLoader.getIcon("/Icons/ide/info.svg")
            @JvmField
            val Error = IconLoader.getIcon("/Icons/ide/error.svg")
        }
    }

    class Toolwindows {
        companion object {
            @JvmField
            val ToolWindowUnityLog = IconLoader.getIcon("/Icons/toolwindows/toolWindowUnityLog.svg")
        }
    }
}
