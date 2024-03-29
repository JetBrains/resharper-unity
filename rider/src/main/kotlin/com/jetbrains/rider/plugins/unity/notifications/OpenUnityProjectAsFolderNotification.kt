package com.jetbrains.rider.plugins.unity.notifications

import com.intellij.ide.BrowserUtil
import com.intellij.ide.util.PropertiesComponent
import com.intellij.notification.*
import com.intellij.openapi.actionSystem.AnActionEvent
import com.intellij.openapi.application.ApplicationInfo
import com.intellij.openapi.application.EDT
import com.intellij.openapi.project.Project
import com.intellij.openapi.project.ex.ProjectManagerEx
import com.intellij.openapi.rd.util.launchBackground
import com.intellij.openapi.rd.util.launchNonUrgentBackground
import com.intellij.openapi.rd.util.startOnUiAsync
import com.intellij.openapi.rd.util.withUiContext
import com.intellij.openapi.startup.ProjectActivity
import com.intellij.openapi.util.NlsSafe
import com.intellij.openapi.vfs.isFile
import com.intellij.openapi.wm.impl.welcomeScreen.WelcomeFrame
import com.intellij.platform.backend.workspace.WorkspaceModel
import com.jetbrains.rd.ide.model.RdExistingSolution
import com.jetbrains.rd.ide.model.RdVirtualSolution
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.SequentialLifetimes
import com.jetbrains.rd.util.reactive.adviseNotNull
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rdclient.util.idea.toVirtualFile
import com.jetbrains.rider.model.RdUnloadProjectDescriptor
import com.jetbrains.rider.model.RdUnloadProjectState
import com.jetbrains.rider.plugins.unity.*
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.ui.hasTrueValue
import com.jetbrains.rider.plugins.unity.util.EditorInstanceJson
import com.jetbrains.rider.plugins.unity.util.EditorInstanceJsonStatus
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.plugins.unity.workspace.hasPackage
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.projectView.SolutionManager
import com.jetbrains.rider.projectView.solution
import com.jetbrains.rider.projectView.solutionDescription
import com.jetbrains.rider.projectView.solutionDirectory
import com.jetbrains.rider.projectView.workspace.ProjectModelEntity
import com.jetbrains.rider.projectView.workspace.ProjectModelEntityVisitor
import com.jetbrains.rider.projectView.workspace.getSolutionEntity
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import org.jetbrains.annotations.Nls

class OpenUnityProjectAsFolderNotification : ProjectActivity {

    companion object {
        private const val GROUP_ID = "Unity project open"
        private const val settingName = "do_not_show_unity_editor_plugin_out_of_support_notification"
    }

    private fun hasUnloadedProjects(project: Project): Boolean {
        val visitor = object : ProjectModelEntityVisitor() {
            var unloadedProjects = 0

            override fun visitUnloadedProject(entity: ProjectModelEntity): Result {
                val state = (entity.descriptor as RdUnloadProjectDescriptor).state
                if (state == RdUnloadProjectState.LoadFailed)
                    unloadedProjects++
                return Result.Stop
            }
        }

        val solutionEntity = WorkspaceModel.getInstance(project).getSolutionEntity() ?: return true
        visitor.visit(solutionEntity)

        return visitor.unloadedProjects > 0
    }

    override suspend fun execute(project: Project) {
        val lifetime = UnityProjectLifetimeService.getLifetime(project)
        withContext(Dispatchers.EDT) {
            if (project.isDisposed || project.isDefault) return@withContext

            project.solution.isLoaded.whenTrue(lifetime) { lt ->
                val model = project.solution.frontendBackendModel
                val solutionDescription = project.solutionDescription
                val title = UnityBundle.message("notification.title.advanced.unity.integration.unavailable")
                val notificationGroupId = NotificationGroupManager.getInstance().getNotificationGroup(GROUP_ID)
                val marketingVersion = ApplicationInfo.getInstance().fullVersion
                val content = UnityBundle.message(
                    "notification.content.make.sure.jetbrains.rider.editor.installed.in.unity.s.package.manager.rider.set.as.external.editor",
                    marketingVersion)
                if (solutionDescription is RdExistingSolution) { // proper solution
                    lt.launchNonUrgentBackground {
                        if (!project.isUnityProject.value)
                            return@launchNonUrgentBackground

                        if (!PropertiesComponent.getInstance(project).getBoolean(settingName)) {
                            val nestedLifetimeDef = lt.createNested()
                            val sequentialLifetimes = SequentialLifetimes(nestedLifetimeDef)
                            // RIDER-105806 Drop the EditorPlugin functionality for Unity versions prior to 2019.2
                            UnityInstallationFinder.getInstance(project).requiresRiderPackage.adviseNotNull(nestedLifetimeDef) {
                                val notificationLifetime = sequentialLifetimes.next().lifetime
                                if (it) return@adviseNotNull
                                val outOfSupportTitle = UnityBundle.message("notification.title.out.of.support.unity.version.integration")
                                val outOfSupportContent = UnityBundle.message("unity.version.out.of.support.notification.message", "2019.2")
                                val notification = Notification(notificationGroupId.displayId, outOfSupportTitle, outOfSupportContent,
                                                                NotificationType.WARNING)
                                notification.setSuggestionType(true) // such a Notification would be removed from the NotificationToolWindow
                                notification.addAction(object : NotificationAction(
                                    UnityBundle.message("read.more")) {
                                    override fun actionPerformed(e: AnActionEvent, notification: Notification) {
                                        val url = "https://youtrack.jetbrains.com/issue/RIDER-105806"
                                        BrowserUtil.browse(url)
                                    }
                                })
                                notification.addAction(object : NotificationAction(
                                    UnityBundle.message("link.label.do.not.show.again")) {
                                    override fun actionPerformed(e: AnActionEvent, notification: Notification) {
                                        PropertiesComponent.getInstance(project).setValue(settingName, true)
                                        nestedLifetimeDef.terminate()
                                    }
                                })
                                notificationLifetime.startOnUiAsync {
                                    Notifications.Bus.notify(notification, project)
                                    notificationLifetime.onTermination { notification.expire() }
                                }
                            }
                        }

                        // Sometimes in Unity "External Script Editor" is set to "Open by file extension"
                        // We check that Library/EditorInstance.json is present, but protocol connection was not initialized
                        // also check that all projects are loaded fine
                        if (EditorInstanceJson.getInstance(project).status == EditorInstanceJsonStatus.Valid
                            && !model.unityEditorConnected.valueOrDefault(false)
                            && !hasUnloadedProjects(project)
                            && UnityInstallationFinder.getInstance(project).requiresRiderPackage.hasTrueValue()
                        ) {
                            val notification = Notification(notificationGroupId.displayId, title, content, NotificationType.WARNING)
                            notification.setSuggestionType(
                                true) // such a Notification would be removed from the NotificationToolWindow, fixes RIDER-98129 Notification "Advanced integration" behaviour
                            Notifications.Bus.notify(notification, project)
                            withContext(Dispatchers.EDT){
                                model.unityEditorConnected.whenTrue(lt) { notification.expire() }
                            }
                        }
                    }
                }
                else if (solutionDescription is RdVirtualSolution) { // opened as folder
                    lt.launchNonUrgentBackground {
                        if (!(project.isUnityProjectFolder.value
                              || UnityProjectDiscoverer.searchUpForFolderWithUnityFileStructure(project.projectDir).first))
                            return@launchNonUrgentBackground

                        @Nls(capitalization = Nls.Capitalization.Sentence)
                        val adviceText = UnityBundle.message(
                            "advice.pleas.close.and.reopen.through.the.unity.editor.or.by.opening.a.sln.file")

                        @Nls(capitalization = Nls.Capitalization.Sentence)
                        val mainText =
                            if (solutionDescription.projectFilePaths.isEmpty())
                                UnityBundle.message("features.are.not.available.when.the.project.is.opened.as.a.folder")
                            else
                                UnityBundle.message("specific.features.are.not.available.when.only.single.project.opened")

                        // todo: hasPackage is unreliable, when PackageManager is still in progress
                        // Revisit this after PackageManager is moved to backend
                        // MTE: There is an inherent race condition here. Packages can be updated at any time, so we can't
                        // be sure that PackageManager is fully loaded at this time.
                        @NlsSafe
                        val contentWoSolution =
                            if (!project.isUnityProjectFolder.value || // means searchUpForFolderWithUnityFileStructure is true
                                (UnityInstallationFinder.getInstance(project).requiresRiderPackage.hasTrueValue()
                                 && !WorkspaceModel.getInstance(project).hasPackage("com.unity.ide.rider"))
                            ) {
                                """$mainText       
            <ul style="margin-left:10px">
              <li>$adviceText</li>
              <li>$content</li>
            </ul>
            """
                            }
                            else {
                                "$mainText $adviceText"
                            }

                        val notification = Notification(notificationGroupId.displayId,
                                                        UnityBundle.message("notification.title.this.looks.like.unity.project"),
                                                        contentWoSolution, NotificationType.WARNING)

                        notification.addAction(object : NotificationAction(
                            UnityBundle.message("close.solution")) {
                            override fun actionPerformed(e: AnActionEvent, notification: Notification) {
                                lt.startOnUiAsync {
                                    ProjectManagerEx.getInstanceEx().closeAndDispose(project)
                                    WelcomeFrame.showIfNoProjectOpened()
                                }
                            }
                        })

                        val baseDir = project.solutionDirectory.toVirtualFile(false) ?: error(
                            "Virtual file not found for solution directory: ${project.solutionDirectory}")
                        val solutionFile = baseDir.findChild(baseDir.name + ".sln")
                        if (solutionFile != null && solutionFile.isFile) {
                            notification.addAction(object : NotificationAction(
                                UnityBundle.message("notification.content.reopen.as.unity.project")) {
                                override fun actionPerformed(e: AnActionEvent, n: Notification) {
                                    // SolutionManager doesn't close the current project if focusOpenInNewFrame is set to true,
                                    // and if it's set to false, we get prompted if we want to open in new or same frame. We
                                    // don't care - we want to close this project, so new frame or reusing means nothing
                                    lt.startOnUiAsync {
                                        e.project?.let { ProjectManagerEx.getInstanceEx().closeAndDispose(it) }
                                    }
                                    Lifetime.Eternal.launchBackground {
                                        SolutionManager.openExistingSolution(null, true, solutionFile, true, true)
                                        ?: return@launchBackground

                                    }
                                }
                            })
                        }

                        withUiContext { Notifications.Bus.notify(notification, project) }
                    }
                }
            }
        }
    }
}