package com.jetbrains.rider.plugins.unity.run.attach

import com.intellij.xdebugger.impl.ui.attach.dialog.AttachDialogProcessItem
import com.intellij.xdebugger.impl.ui.attach.dialog.extensions.XAttachDialogItemPresentationProvider

class UnityLocalAttachDialogItemPresentationProvider : XAttachDialogItemPresentationProvider {
    override fun isApplicableFor(item: AttachDialogProcessItem) = item.debuggers.any { it is UnityLocalAttachDebugger }
    override fun getPriority() = 10

    override fun getProcessExecutableText(item: AttachDialogProcessItem): String {
        item.dataHolder.getUserData(UnityLocalAttachProcessDebuggerProvider.PROCESS_INFO_KEY)?.let {
            it[item.processInfo.pid]?.let { details ->
                return when {
                           details.instanceId != null -> "${item.processInfo.executableDisplayName} ${details.instanceName ?: details.instanceId}"
                           details.instanceName != null -> "${item.processInfo.executableDisplayName} ${details.instanceName}"
                           else -> item.processInfo.executableDisplayName
                       } + if (details.projectName != null) " (${details.projectName})" else ""
            }
        }

        return super.getProcessExecutableText(item)
    }
}