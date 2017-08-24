package com.jetbrains.rider.plugins.unity.util

fun convertPortToDebuggerPort(port: Int): Int {
    return port % 1000 + 56000
}

fun convertPortToDebuggerPort(port: Long): Int {
    return (port % 1000).toInt() + 56000
}