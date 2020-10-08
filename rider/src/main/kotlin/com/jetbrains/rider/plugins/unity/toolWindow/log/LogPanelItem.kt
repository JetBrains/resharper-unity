package com.jetbrains.rider.plugins.unity.toolWindow.log

// TODO: This type is a copy of RdLogEventType from the backendUnity model
enum class LogEventType {
    Error,
    Warning,
    Message;

    companion object {
        fun fromRdLogEventTypeInt(value: Int): LogEventType {
            return when (value) {
                0 -> Error
                1 -> Warning
                2 -> Message
                else -> Error
            }
        }
    }
}

enum class LogEventMode {
    Edit,
    Play;

    companion object {
        fun fromRdLogEventModeInt(value: Int): LogEventMode {
            return when (value) {
                0 -> Edit
                1 -> Play
                else -> Edit
            }
        }
    }
}

data class LogPanelItem(val time : Long,
                   val type : LogEventType,
                   val mode : LogEventMode,
                   val message : String,
                   val stackTrace : String,
                   val count: Int)