package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEventMode
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEventType

data class LogPanelItem(val time : Long,
                   val type : RdLogEventType,
                   val mode : RdLogEventMode,
                   val message : String,
                   val stackTrace : String,
                   val count: Int)