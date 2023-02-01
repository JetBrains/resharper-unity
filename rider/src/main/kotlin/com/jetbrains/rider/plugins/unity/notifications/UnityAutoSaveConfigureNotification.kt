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
import com.intellij.util.application
import com.jetbrains.rd.platform.util.idea.LifetimedService
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.isAlive
import com.jetbrains.rd.util.reactive.*
import com.jetbrains.rider.document.getFirstEditor
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer
import com.jetbrains.rider.plugins.unity.model.ScriptCompilationDuringPlay
import com.jetbrains.rider.plugins.unity.model.UnityEditorState
import com.jetbrains.rider.plugins.unity.model.frontendBackend.frontendBackendModel
import com.jetbrains.rider.projectView.SolutionLifecycleHost
import com.jetbrains.rider.projectView.solution

class UnityAutoSaveConfigureNotification(project: Project) : LifetimedService() {
    private val propertiesComponent: PropertiesComponent = PropertiesComponent.getInstance()
    private var lifetimeDefinition = project.lifetime.createNested()
    private val KEY = Key.create<Any>("PromoteAutoSave")

    companion object {
        private const val settingName = "do_not_show_unity_auto_save_notification"
    }

    init {
        SolutionLifecycleHost.getInstance(project).isBackendLoaded.whenTrue(project.lifetime) {
            if (!propertiesComponent.getBoolean(settingName) && UnityProjectDiscoverer.getInstance(project).isUnityProject) {

                val eventMulticaster = EditorFactory.getInstance().eventMulticaster
                val generalSettings = GeneralSettings.getInstance()

                val documentListener: DocumentListener = object : DocumentListener {
                    override fun documentChanged(event: DocumentEvent) {
                        val model = project.solution.frontendBackendModel
                        if (model.unityEditorState.valueOrDefault(UnityEditorState.Disconnected) != UnityEditorState.Play)
                            return

                        if (!model.unityApplicationSettings.scriptCompilationDuringPlay.hasValue)
                            return

                        if (model.unityApplicationSettings.scriptCompilationDuringPlay.valueOrThrow != ScriptCompilationDuringPlay.RecompileAndContinuePlaying)
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
        if (project.solution.frontendBackendModel.unityEditorState.valueOrDefault(UnityEditorState.Disconnected) != UnityEditorState.Play)
            return

        val panel = EditorNotificationPanel(LightColors.RED)
        panel.text = UnityBundle.message("label.you.are.modifying.script.while.unity.editor.being.in.play.mode.this.can.lead.to.loss.state.in.your.running.game")

        @Suppress("DialogTitleCapitalization")
        panel.createActionLabel(UnityBundle.message("link.label.configure.unity.editor")) {
            project.solution.frontendBackendModel.showPreferences.fire()
            lifetimeDefinition.terminate()
        }

        panel.createActionLabel(UnityBundle.message("link.label.do.not.show.again")) {
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