package com.jetbrains.rider.plugins.unity.terminal

import com.intellij.openapi.project.Project
import com.intellij.platform.eel.isWindows
import com.intellij.platform.eel.provider.LocalEelDescriptor
import com.jetbrains.rider.plugins.unity.settings.agents.UnityCliEnvironment
import com.jetbrains.rider.plugins.unity.settings.agents.UnityForAgentsFeature
import com.jetbrains.rider.plugins.unity.settings.agents.unityCliDirectoryOffPath
import org.jetbrains.plugins.terminal.startup.MutableShellExecOptions
import org.jetbrains.plugins.terminal.startup.ShellExecOptionsCustomizer

internal class UnityCliShellExecOptionsCustomizer : ShellExecOptionsCustomizer {
    override fun customizeExecOptions(project: Project, shellExecOptions: MutableShellExecOptions) {
        if (!UnityForAgentsFeature.isEnabled()) return
        val descriptor = shellExecOptions.eelDescriptor
        if (descriptor != LocalEelDescriptor || !descriptor.osFamily.isWindows) return
        val directory = UnityCliEnvironment.getInstance().unityCliDirectoryOffPath() ?: return
        shellExecOptions.appendEntryToPATH(directory)
    }
}
