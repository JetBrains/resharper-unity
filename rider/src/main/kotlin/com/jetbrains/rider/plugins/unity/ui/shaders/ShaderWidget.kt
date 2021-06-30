package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.actionSystem.impl.SimpleDataContext
import com.intellij.openapi.editor.impl.EditorImpl
import com.intellij.openapi.fileEditor.FileEditorManagerEvent
import com.intellij.openapi.fileEditor.FileEditorManagerListener
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.createNestedDisposable
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.openapi.wm.CustomStatusBarWidget
import com.intellij.openapi.wm.StatusBarWidget
import com.intellij.openapi.wm.StatusBarWidget.Multiframe
import com.intellij.openapi.wm.impl.status.EditorBasedWidget
import com.jetbrains.rd.ide.model.RdDocumentId
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.SequentialLifetimes
import com.jetbrains.rd.util.reactive.IProperty
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rdclient.document.getDocumentId
import com.jetbrains.rider.plugins.unity.UnityProjectDiscoverer
import com.jetbrains.rider.cpp.fileType.CppFileType
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ShaderContextData
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ShaderContextDataBase
import com.jetbrains.rider.plugins.unity.FrontendBackendHost
import icons.UnityIcons
import java.awt.BorderLayout
import java.awt.event.MouseEvent
import java.awt.event.MouseListener
import javax.swing.JComponent
import javax.swing.JLabel
import javax.swing.JPanel


class ShaderWidget(project: Project) : EditorBasedWidget(project), CustomStatusBarWidget, Multiframe {

    private val statusBarComponent = JPanel(BorderLayout())
    private val label = JLabel(UnityIcons.FileTypes.ShaderLab)
    private val requestLifetime = SequentialLifetimes(project.lifetime)
    private val currentContextMode : IProperty<ShaderContextData?> = Property(null)

    companion object {
        private fun getContextPresentation(data : ShaderContextData) = "${data.name}:${data.startLine}"
    }

    init {
        label.text = "..."
        statusBarComponent.isVisible = false
        statusBarComponent.add(label)
        label.addMouseListener(object : MouseListener {
            override fun mouseClicked(e: MouseEvent?) {
            }

            override fun mousePressed(e: MouseEvent?) {
            }

            override fun mouseReleased(e: MouseEvent?) {
                showPopup(label)
            }

            override fun mouseEntered(e: MouseEvent?) {
            }

            override fun mouseExited(e: MouseEvent?) {
            }

        })

        if (UnityProjectDiscoverer.getInstance(project).isUnityProject) {

            project.messageBus.connect(project.lifetime.createNestedDisposable())
                .subscribe(FileEditorManagerListener.FILE_EDITOR_MANAGER, this)

            currentContextMode.advise(project.lifetime) {
                if (it == null) {
                    label.text = "Auto"
                    label.toolTipText = "Default file and symbol context"
                } else {
                    label.text = getContextPresentation(it)
                    label.toolTipText = "File and symbol context derived from include at ${getContextPresentation(it)}"
                }
            }
        }
    }

    override fun selectionChanged(event: FileEditorManagerEvent) {
        if (UnityProjectDiscoverer.getInstance(project).isUnityProject)
            updateState((editor as? EditorImpl)?.virtualFile)
    }

    private fun updateState(file: VirtualFile?) {

        val lifetimeDef = requestLifetime.next()
        val host = FrontendBackendHost.getInstance(project)

        if (file == null || file.fileType !is CppFileType) {
            statusBarComponent.isVisible = false
            return
        }

        if (editor == null) {
            statusBarComponent.isVisible = false
            return
        }

        val id = editor?.document?.getDocumentId(project)
        if (id == null) {
            statusBarComponent.isVisible = false
            return
        }

        host.model.requestCurrentContext.start(lifetimeDef.lifetime, id).result.advise(lifetimeDef.lifetime) {
            val result = it.unwrap()
            statusBarComponent.isVisible = true
            if (result is ShaderContextData)
                currentContextMode.value = result
            else
                currentContextMode.value = null
        }
    }

    override fun ID(): String = "ShaderWidget"

    override fun getComponent(): JComponent {
        return statusBarComponent
    }

    override fun copy(): StatusBarWidget {
        return ShaderWidget(project)
    }


    fun showPopup(label: JLabel) {
        val lt: Lifetime = Lifetime.Eternal
        val id = editor?.document?.getDocumentId(project)
        if (id == null)
            return
        val host = FrontendBackendHost.getInstance(project)
        host.model.requestShaderContexts.start(lt, id).result.advise(lt) {
            val items = it.unwrap()
            val actions = createActions(host, id, items)
            val group = DefaultActionGroup().apply {

                addAll(actions)
            }
            val popup = ShaderContextPopup(group, SimpleDataContext.getProjectContext(project), currentContextMode)
            popup.showInCenterOf(label)
        }
    }

    private fun createActions(host: FrontendBackendHost, id: RdDocumentId, items: List<ShaderContextDataBase>): List<AnAction> {
        val result = mutableListOf<AnAction>(ShaderAutoContextSwitchAction(project, id, host, currentContextMode))
        for (item in items) {
            result.add(ShaderContextSwitchAction(project, id, host, item as ShaderContextData, currentContextMode))
        }
        return result
    }
}