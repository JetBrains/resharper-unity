package com.jetbrains.rider.plugins.unity.ui

import com.intellij.codeInsight.daemon.DaemonCodeAnalyzer
import com.intellij.ide.util.PropertiesComponent
import com.intellij.openapi.fileEditor.FileEditor
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.defineNestedLifetime
import com.intellij.openapi.util.Key
import com.intellij.openapi.vfs.VfsUtil
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.ui.EditorNotificationPanel
import com.intellij.ui.EditorNotifications
import com.intellij.ui.HyperlinkLabel
import com.intellij.util.io.exists
import com.intellij.util.io.isDirectory
import com.intellij.util.text.VersionComparatorUtil
import com.jetbrains.rd.framework.RdTaskResult
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.reactive.adviseOnce
import com.jetbrains.rd.util.reactive.whenTrue
import com.jetbrains.rider.isUnityProject
import com.jetbrains.rider.model.unity.frontendBackend.frontendBackendModel
import com.jetbrains.rider.plugins.unity.actions.StartUnityAction
import com.jetbrains.rider.plugins.unity.isConnectedToEditor
import com.jetbrains.rider.plugins.unity.toolWindow.UnityToolWindowFactory
import com.jetbrains.rider.plugins.unity.util.UnityInstallationFinder
import com.jetbrains.rider.plugins.unity.util.isUxmlFile
import com.jetbrains.rider.projectDir
import com.jetbrains.rider.projectView.SolutionLifecycleHost
import com.jetbrains.rider.projectView.solution
import java.nio.file.Paths

class UxmlMissingSchemaEditorNotification: EditorNotifications.Provider<EditorNotificationPanel>() {

    companion object {
        private val KEY = Key.create<EditorNotificationPanel>("unity.uxml.missing.schemas.notification.panel")
        private const val DO_NOT_SHOW_VERSION_KEY = "unity.uxml.unsupported.version.do.not.show"
    }

    override fun getKey(): Key<EditorNotificationPanel>  = KEY

    override fun createNotificationPanel(file: VirtualFile, fileEditor: FileEditor, project: Project): EditorNotificationPanel? {

        // We might be called before we have the connection to the Unity editor (via the backend). In which case, we'll
        // show the "Please start Unity message"
        if (project.isUnityProject() && isUxmlFile(file)) {
            // Wait until the solution has finished loading before showing the notification panel. If we show it
            // while it's opening, we'll incorrectly show the "please start Unity" message because the protocols
            // won't have initialised yet.
            val solutionLifecycleHost = SolutionLifecycleHost.getInstance(project)
            if (!solutionLifecycleHost.isBackendLoaded.value) {
                val lifetimeDefinition = project.defineNestedLifetime()
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
                val panel = EditorNotificationPanel()
                panel.text("UXML support requires Unity 2019.1 or above")
                panel.createActionLabel("Don't show again") {
                    // Project level — do not show again for this project
                    PropertiesComponent.getInstance(project).setValue(DO_NOT_SHOW_VERSION_KEY, true)
                    EditorNotifications.getInstance(project).updateAllNotifications()
                }
                return panel
            }

            val schemasFolder = Paths.get(project.projectDir.canonicalPath!!, "UIElementsSchema")
            if (!schemasFolder.exists() || !schemasFolder.isDirectory()) {
                val panel = EditorNotificationPanel()
                panel.text("Generate UIElements schema to get validation and code completion.")

                if (project.isConnectedToEditor()) {
                    var link: HyperlinkLabel? = null
                    link = panel.createActionLabel("Generate schema") {
                        generateSchema(project, panel, link)
                    }
                }
                else {
                    var link: HyperlinkLabel? = null
                    link = panel.createActionLabel("Start Unity and generate schema") {
                        panel.text("Starting Unity. Please wait.")

                        val lifetimeDefinition = project.defineNestedLifetime()
                        project.solution.frontendBackendModel.sessionInitialized.advise(lifetimeDefinition.lifetime) {
                            if (project.isConnectedToEditor()) {
                                generateSchema(project, panel, link)
                                lifetimeDefinition.terminate()
                            }
                        }

                        link?.isVisible = false
                        StartUnityAction.startUnity(project)
                    }
                }

                return panel
            }
        }

        return null
    }

    private fun generateSchema(project: Project, panel: EditorNotificationPanel, link: HyperlinkLabel?) {
        panel.text("Generating. Please wait.")
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
                panel.text("Unable to generate schema. Please check the Unity Console for errors.")
                link?.setHyperlinkText("Try again")
                link?.isVisible = true

                UnityToolWindowFactory.show(project)
            }
        }
    }
}