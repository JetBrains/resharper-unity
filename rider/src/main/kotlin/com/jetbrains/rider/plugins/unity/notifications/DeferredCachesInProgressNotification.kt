package com.jetbrains.rider.plugins.unity.notifications

import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.MessageType
import com.intellij.openapi.wm.WindowManager
import com.intellij.openapi.wm.ex.StatusBarEx
import com.intellij.util.ui.UIUtil
import com.jetbrains.rd.platform.util.idea.ProtocolSubscribedProjectComponent
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.solution


class DeferredCachesInProgressNotification(project: Project): ProtocolSubscribedProjectComponent(project) {

    init {
        project.solution.frontendBackendModel.showDeferredCachesProgressNotification.adviseNotNull(projectComponentLifetime) {
            UIUtil.invokeLaterIfNeeded {
                val ideFrame = WindowManager.getInstance().getIdeFrame(project)
                if (ideFrame != null) {
                    (ideFrame.statusBar as StatusBarEx?)!!.notifyProgressByBalloon(MessageType.WARNING,
                                                                                   UnityBundle.message("popup.content.usages.in.assets.are.not.available.during.initial.asset.indexing"))
                }
            }
        }
    }
}