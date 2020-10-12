package com.jetbrains.rider.plugins.unity.notifications

import com.intellij.ide.GeneralSettings
import com.intellij.ide.util.PropertiesComponent
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.EditorFactory
import com.intellij.openapi.editor.event.DocumentEvent
import com.intellij.openapi.editor.event.DocumentListener
import com.intellij.openapi.fileEditor.FileEditorManager
import com.intellij.openapi.fileEditor.impl.text.TextEditorProvider
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.createNestedDisposable
import com.intellij.openapi.util.Key
import com.intellij.ui.EditorNotificationPanel
import com.intellij.ui.LightColors
import com.jetbrains.rd.platform.util.application
import com.jetbrains.rd.platform.util.idea.ProtocolSubscribedProjectComponent
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.isAlive
import com.jetbrains.rd.util.lifetime.onTermination
import com.jetbrains.rd.util.reactive.*
import com.jetbrains.rider.UnityProjectDiscoverer
import com.jetbrains.rider.document.getFirstEditor
import com.jetbrains.rider.model.unity.EditorState
import com.jetbrains.rider.model.unity.ScriptCompilationDuringPlay
import com.jetbrains.rider.model.unity.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.SolutionLifecycleHost
import com.jetbrains.rider.projectView.solution

class UnityAutoSaveConfigureNotification(project: Project) : ProtocolSubscribedProjectComponent(project) {
    private val propertiesComponent: PropertiesComponent = PropertiesComponent.getInstance()
    private var lifetimeDefinition = projectComponentLifetime.createNested()
    private val KEY = Key.create<Any>("PromoteAutoSave")

    companion object {
        private const val settingName = "do_not_show_unity_auto_save_notification"
    }

    init {
        SolutionLifecycleHost.getInstance(project).isBackendLoaded.whenTrue(projectComponentLifetime) {
            if (!propertiesComponent.getBoolean(settingName) && UnityProjectDiscoverer.getInstance(project).isUnityProject) {

                val eventMulticaster = EditorFactory.getInstance().eventMulticaster
                val generalSettings = GeneralSettings.getInstance()

                val documentListener: DocumentListener = object : DocumentListener {
                    override fun documentChanged(event: DocumentEvent) {
                        val model = project.solution.frontendBackendModel
                        if (model.editorState.valueOrDefault(EditorState.Disconnected) != EditorState.Play)
                            return

                        if (!model.scriptCompilationDuringPlay.hasValue)
                            return

                        if (model.scriptCompilationDuringPlay.valueOrThrow != ScriptCompilationDuringPlay.RecompileAndContinuePlaying)
                            return

                        if (!lifetimeDefinition.isAlive)
                            return

                        if (generalSettings.isAutoSaveIfInactive || generalSettings.isSaveOnFrameDeactivation) {

                            val editor = event.document.getFirstEditor(project) ?: return
                            showNotification(lifetimeDefinition.lifetime, editor)
                        }
                    }
                }

                eventMulticaster.addDocumentListener(documentListener, it.createNestedDisposable())
            }
        }
    }

    fun showNotification(lifetime: Lifetime, editor: Editor) {
        application.assertIsDispatchThread()

        if (!lifetime.isAlive) return
        val project = editor.project ?: return

        val textEditor = TextEditorProvider.getInstance().getTextEditor(editor)
        if (textEditor.getUserData(KEY) != null) return

        textEditor.putUserData(KEY, Any())

        // Do not show notification, when user leaves play mode and start typing in that moment
        if (project.solution.frontendBackendModel.editorState.valueOrDefault(EditorState.Disconnected) != EditorState.Play)
            return

        val panel = EditorNotificationPanel(LightColors.RED)
        panel.setText("You are modifying a script while Unity Editor is being in Play Mode. This can lead to a loss of the state in your running game.")

        panel.createActionLabel("Configure Unity Editor") {
            project.solution.frontendBackendModel.showPreferences.fire()
            lifetimeDefinition.terminate()
        }

        panel.createActionLabel("Do not show again") {
            propertiesComponent.setValue(settingName, true)
            lifetimeDefinition.terminate()
        }

        panel.createActionLabel("X") {
            lifetimeDefinition.terminate()
        }

        lifetimeDefinition.onTermination {
            if (textEditor.getUserData(KEY) != null) {
                FileEditorManager.getInstance(project).removeTopComponent(textEditor, panel)
                textEditor.putUserData(KEY, null)
            }
        }

        FileEditorManager.getInstance(project).addTopComponent(textEditor, panel)
    }
}