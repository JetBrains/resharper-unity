package com.jetbrains.rider.plugins.unity.notifications

import com.intellij.ide.impl.ProjectUtil
import com.intellij.ide.projectView.ProjectView
import com.intellij.notification.*
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.components.ProjectComponent
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.openapi.wm.impl.welcomeScreen.WelcomeFrame
import com.intellij.util.ui.EdtInvocationManager
import com.jetbrains.rider.UnityProjectDiscoverer
import com.jetbrains.rider.model.RdExistingSolution
import com.jetbrains.rider.model.RdVirtualSolution
import com.jetbrains.rider.plugins.unity.explorer.UnityExplorer
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.projectView.SolutionManager
import com.jetbrains.rider.projectView.solutionDescription
import javax.swing.event.HyperlinkEvent

class OpenUnityProjectAsFolderNotification(private val project: Project, private val unityProjectDiscoverer: UnityProjectDiscoverer)
    : ProjectComponent {

    companion object {
        private val notificationGroupId = NotificationGroup("Unity project open", NotificationDisplayType.STICKY_BALLOON, true)
    }

    override fun projectOpened() {
        // Do nothing if we're not in Unity folders, or we are, but we're a proper .sln based solution
        if (!unityProjectDiscoverer.isUnityProjectFolder || project.solutionDescription is RdExistingSolution) return

        val solutionDescription = project.solutionDescription
        if (solutionDescription is RdVirtualSolution) {

            val content = if (solutionDescription.projectFilePaths.isEmpty()) {
                "This looks like a Unity project. C# and Unity specific functionality is not available when the project is opened as a folder." +
                    " Please <a href=\"close\">close</a> and reopen through the Unity editor, or by opening a .sln file."
            }
            else
                "This looks like a Unity project. C# and Unity specific functionality is not available when only a single project is opened." +
                    " Please <a href=\"close\">close</a> and reopen through the Unity editor, or by opening a .sln file."
            val title = "Unity functionality unavailable"
            val notification = Notification(notificationGroupId.displayId, title, content, NotificationType.WARNING)
            notification.setListener { _, hyperlinkEvent ->

                if (hyperlinkEvent.eventType != HyperlinkEvent.EventType.ACTIVATED) return@setListener

                if (hyperlinkEvent.description == "close") {
                    ProjectUtil.closeAndDispose(project)
                    WelcomeFrame.showIfNoProjectOpened()
                }
            }

            val baseDir: VirtualFile = project.projectDir
            val solutionFile = baseDir.findChild(baseDir.name + ".sln")
            if (solutionFile != null && solutionFile.exists()) {
                notification.addAction(object : NotificationAction("Reopen as Unity project") {
                    override fun actionPerformed(e: AnActionEvent, n: Notification) {
                        // SolutionManager doesn't close the current project if focusOpenInNewFrame is set to true,
                        // and if it's set to false, we get prompted if we want to open in new or same frame. We
                        // don't care - we want to close this project, so new frame or reusing means nothing
                        e.project?.let { ProjectUtil.closeAndDispose(it) }
                        val newProject = SolutionManager.openExistingSolution(null, true, solutionFile)

                        // Opening as folder saves settings to `.idea/.idea.{folder}`. This includes the last selected
                        // solution view pane, which will be file system. A Unity generated solution will use the
                        // same settings folder, so will read the last selected solution view pane and fail to show
                        // the Unity explorer view. We'll override that saved value here, and make Unity Explorer
                        // the currently selected value. See RIDER-17865
                        EdtInvocationManager.getInstance().invokeLater {
                            val projectView = ProjectView.getInstance(newProject)
                            projectView.changeView(UnityExplorer.ID)
                        }
                    }
                })
            }

            Notifications.Bus.notify(notification, project)
        }
    }
}