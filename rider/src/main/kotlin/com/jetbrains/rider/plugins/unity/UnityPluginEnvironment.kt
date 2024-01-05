package com.jetbrains.rider.plugins.unity

import com.jetbrains.rider.RiderEnvironment
import java.io.File

object UnityPluginEnvironment {
    const val pluginId = "com.intellij.resharper.unity"

    /**
     * Returns the plugin file or directory using provided name in the given
     * environment. In case file is located under DotFiles directory, it should
     * be provided as a prefix.
     */
    fun getBundledFile(fileName: String, vararg prefixes: String) : File {
        val prefixPath = prefixes.joinToString(separator = File.separator)
        val fullFileName = if (prefixes.isNotEmpty()) "$prefixPath${File.separator}$fileName" else fileName

        return RiderEnvironment.getBundledPluginFile(fullFileName, pluginId)
    }
}