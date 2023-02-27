package com.jetbrains.rider.plugins.unity.notifications

import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.MessageType
import com.intellij.openapi.wm.WindowManager
import com.intellij.openapi.wm.ex.StatusBarEx
import com.intellij.util.ui.UIUtil
import com.jetbrains.rd.ide.model.Solution
import com.jetbrains.rd.platform.util.idea.LifetimedService
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.protocol.ProtocolExtListener
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel


class DeferredCachesInProgressNotification : LifetimedService() {
    class ProtocolListener : ProtocolExtListener<Solution, FrontendBackendModel> {
        override fun extensionCreated(lifetime: Lifetime, project: Project, parent: Solution, model: FrontendBackendModel) {
            model.showDeferredCachesProgressNotification.adviseNotNull(project.lifetime) {
                UIUtil.invokeLaterIfNeeded {
                    val ideFrame = WindowManager.getInstance().getIdeFrame(project)
                    if (ideFrame != null) {
                        (ideFrame.statusBar as StatusBarEx?)!!.notifyProgressByBalloon(MessageType.WARNING,
                                                                                       UnityBundle.message(
                                                                                           "popup.content.usages.in.assets.are.not.available.during.initial.asset.indexing"))
                    }
                }
            }
        }
    }
}