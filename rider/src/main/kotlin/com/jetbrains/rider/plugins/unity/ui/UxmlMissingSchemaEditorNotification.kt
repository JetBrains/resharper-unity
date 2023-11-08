package com.jetbrains.rider.plugins.unity.ui

import com.intellij.codeInsight.daemon.DaemonCodeAnalyzer
import com.intellij.ide.util.PropertiesComponent
import com.intellij.openapi.fileEditor.FileEditor
import com.intellij.openapi.project.Project
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.EditorNotificationPanel
import com.intellij.ui.EditorNotificationProvider
import com.intellij.ui.EditorNotifications
import com.intellij.ui.HyperlinkLabel
import com.intellij.util.io.isDirectory
import com.intellij.util.text.VersionComparatorUtil
import com.jetbrains.rd.framework.RdTaskResult
import com.intellij.openapi.rd.util.lifetime
import com.jetbrains.rd.util.reactive.adviseOnce
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.actions.StartUnityAction
import com.jetbrains.rider.plugins.unity.isConnectedToEditor
import com.jetbrains.rider.plugins.unity.isUnityProject
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.toolWindow.UnityToolWindowFactory
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.plugins.unity.util.isUxmlFile
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.projectView.SolutionLifecycleHost
import com.jetbrains.rider.projectView.solution
import java.nio.file.Paths
import java.util.function.Function
import javax.swing.JComponent
import kotlin.io.path.notExists

class UxmlMissingSchemaEditorNotification: EditorNotificationProvider {

    companion object {
        private const val DO_NOT_SHOW_VERSION_KEY = "unity.uxml.unsupported.version.do.not.show"
    }

    override fun collectNotificationData(project: Project, file: VirtualFile): Function<in FileEditor, out JComponent?>? {
        // We might be called before we have the connection to the Unity editor (via the backend). In which case, we'll
        // show the "Please start Unity message"
        if (!project.isUnityProject() || !isUxmlFile(file)) return null

        // Wait until the solution has finished loading before showing the notification panel. If we show it
        // while it's opening, we'll incorrectly show the "please start Unity" message because the protocols
        // won't have initialised yet.
        val solutionLifecycleHost = SolutionLifecycleHost.getInstance(project)
        if (!solutionLifecycleHost.isBackendLoaded.value) {
            val lifetimeDefinition = UnityProjectLifetimeService.getNestedLifetimeDefinition(project)
            solutionLifecycleHost.isBackendLoaded.whenTrue(lifetimeDefinition.lifetime) {
                EditorNotifications.getInstance(project).updateAllNotifications()
                lifetimeDefinition.terminate()
            }

            return null
        }

        // Proper support begins in 2019.1 (types moved to non-experimental namespaces)
        // Schema generation first appears in 2018.2 (in experimental namespace)
        // We'll support 2018.2+ but recommend 2019.1+
        val unityVersion: String? = UnityInstallationFinder.getInstance(project).getApplicationVersion()
        if (unityVersion != null && VersionComparatorUtil.compare(unityVersion, "2018.2.0") == -1) {
            if (PropertiesComponent.getInstance(project).getBoolean(DO_NOT_SHOW_VERSION_KEY, false)) {
                return null
            }

            return Function {
                EditorNotificationPanel().also { panel ->
                    panel.text(UnityUIBundle.message("uxml.support.requires.unity.or.above"))
                    panel.createActionLabel(UnityUIBundle.message("don.t.show.again")) {
                        // Project level — do not show again for this project
                        PropertiesComponent.getInstance(project).setValue(DO_NOT_SHOW_VERSION_KEY, true)
                        EditorNotifications.getInstance(project).updateAllNotifications()
                    }
                }
            }
        }

        val schemasFolder = Paths.get(project.projectDir.canonicalPath!!, "UIElementsSchema")
        if (schemasFolder.notExists() || !schemasFolder.isDirectory()) {
            return Function {
                EditorNotificationPanel().also { panel ->
                    panel.text(UnityUIBundle.message("label.generate.uielements.schema.to.get.validation.code.completion"))

                    if (project.isConnectedToEditor()) {
                        var link: HyperlinkLabel? = null
                        link = panel.createActionLabel(UnityUIBundle.message("link.label.generate.schema")) {
                            generateSchema(project, panel, link)
                        }
                    } else {
                        var link: HyperlinkLabel? = null
                        link = panel.createActionLabel(UnityUIBundle.message("link.label.start.unity.generate.schema")) {
                            @Suppress("DialogTitleCapitalization")
                            panel.text(UnityUIBundle.message("label.starting.unity.please.wait"))

                            val lifetimeDefinition = UnityProjectLifetimeService.getNestedLifetimeDefinition(project)
                            project.solution.frontendBackendModel.unityEditorConnected.whenTrue(lifetimeDefinition.lifetime) {
                                generateSchema(project, panel, link)
                                lifetimeDefinition.terminate()
                            }

                            link?.isVisible = false
                            StartUnityAction.startUnity(project)
                        }
                    }
                }
            }
        }

        return null
    }

    private fun generateSchema(project: Project, panel: EditorNotificationPanel, link: HyperlinkLabel?) {
        panel.text(UnityUIBundle.message("label.generating.please.wait"))
        link?.isVisible = false

        project.solution.frontendBackendModel.generateUIElementsSchema.start(project.lifetime, Unit).result.adviseOnce(project.lifetime) {
            if (it is RdTaskResult.Success && it.value) {
                EditorNotifications.getInstance(project).updateAllNotifications()

                // Because the files are created while the app still has focus, the VFS doesn't see the updates, so
                // force a refresh. We have to scan the whole project dir because the VFS doesn't know anything
                // about the newly created UIElementsSchema directory. We have to use the VFS instead of nio File
                // because we need a VirtualFile to create an XmlFile to represent the schema and also to act as a
                // trackable dependency for the cached schema value.
                VfsUtil.markDirtyAndRefresh(false, true, true, project.projectDir)
                DaemonCodeAnalyzer.getInstance(project).restart()
            } else {
                // This is either an exception in UxmlSchemaGenerator, an exception in the protocol, or we're unable to
                // find the UxmlSchemaGenerator class via reflection.
                panel.text(UnityUIBundle.message("label.unable.to.generate.schema.please.check.unity.console.for.errors"))
                link?.setHyperlinkText(UnityUIBundle.message("link.label.try.again"))
                link?.isVisible = true

                UnityToolWindowFactory.show(project)
            }
        }
    }

}