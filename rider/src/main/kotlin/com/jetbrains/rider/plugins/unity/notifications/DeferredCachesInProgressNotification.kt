package com.jetbrains.rider.plugins.unity.notifications

import com.intellij.openapi.client.ClientProjectSession
import com.intellij.openapi.components.Service
import com.intellij.openapi.ui.MessageType
import com.intellij.openapi.wm.WindowManager
import com.intellij.openapi.wm.ex.StatusBarEx
import com.intellij.util.ui.UIUtil
import com.jetbrains.rd.protocol.SolutionExtListener
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendModel


@Service(Service.Level.PROJECT)
class DeferredCachesInProgressNotification {
    class ProtocolListener : SolutionExtListener<FrontendBackendModel> {
        override fun extensionCreated(lifetime: Lifetime, session: ClientProjectSession, model: FrontendBackendModel) {
            model.showDeferredCachesProgressNotification.adviseNotNull(lifetime) {
                UIUtil.invokeLaterIfNeeded {
                    val ideFrame = WindowManager.getInstance().getIdeFrame(session.project)
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