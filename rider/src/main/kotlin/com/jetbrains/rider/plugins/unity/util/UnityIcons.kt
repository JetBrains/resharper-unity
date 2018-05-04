package com.jetbrains.rider.plugins.unity.util

import com.intellij.icons.AllIcons
import com.intellij.openapi.util.IconLoader
import com.intellij.ui.AnimatedIcon

class UnityIcons {
    class Icons {
        companion object {
            @JvmField
            val ShaderLabFile = IconLoader.getIcon("/Icons/Shader/Shader.png")

            val UnityLogo = IconLoader.getIcon("/Icons/Logo/UnityLogo.png")

            // TODO: Proper icons!
            @JvmField
            val AttachEditorDebugConfiguration = UnityLogo

            @JvmField
            val AttachAndPlayEditorDebugConfiguration = UnityLogo

            @JvmField
            val ImportantActions = UnityLogo

            @JvmField
            val EditorConnectionStatus = UnityLogo
        }
    }

    class Status{
        companion object {
            @JvmField
            val UnityStatus = IconLoader.getIcon("/Icons/UnityStatus/unityStatus.svg")
            @JvmField
            val UnityStatusPlay = IconLoader.getIcon("/Icons/UnityStatus/unityStatusPlay.svg")

            @JvmField
            val UnityStatusProgress1 = IconLoader.getIcon("/Icons/UnityStatus/unityStatusProgress1.svg")
            @JvmField
            val UnityStatusProgress2 = IconLoader.getIcon("/Icons/UnityStatus/unityStatusProgress2.svg")
            @JvmField
            val UnityStatusProgress3 = IconLoader.getIcon("/Icons/UnityStatus/unityStatusProgress3.svg")
            @JvmField
            val UnityStatusProgress4 = IconLoader.getIcon("/Icons/UnityStatus/unityStatusProgress4.svg")
            @JvmField
            val UnityStatusProgress5 = IconLoader.getIcon("/Icons/UnityStatus/unityStatusProgress5.svg")

            val UnityStatusProgress = AnimatedIcon(150,
                UnityStatusProgress5,
                UnityStatusProgress4,
                UnityStatusProgress3,
                UnityStatusProgress2,
                UnityStatusProgress1)

            @JvmField
            val UnityStatusPlayProgress1 = IconLoader.getIcon("/Icons/UnityStatus/unityStatusPlayProgress1.svg")
            @JvmField
            val UnityStatusPlayProgress2 = IconLoader.getIcon("/Icons/UnityStatus/unityStatusPlayProgress2.svg")
            @JvmField
            val UnityStatusPlayProgress3 = IconLoader.getIcon("/Icons/UnityStatus/unityStatusPlayProgress3.svg")
            @JvmField
            val UnityStatusPlayProgress4 = IconLoader.getIcon("/Icons/UnityStatus/unityStatusPlayProgress4.svg")
            @JvmField
            val UnityStatusPlayProgress5 = IconLoader.getIcon("/Icons/UnityStatus/unityStatusPlayProgress5.svg")

            val UnityStatusPlayProgress = AnimatedIcon(150,
                UnityStatusPlayProgress5,
                UnityStatusPlayProgress4,
                UnityStatusPlayProgress3,
                UnityStatusPlayProgress2,
                UnityStatusPlayProgress1)
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
