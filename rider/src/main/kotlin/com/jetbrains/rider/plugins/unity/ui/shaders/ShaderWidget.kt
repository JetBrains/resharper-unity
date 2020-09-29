package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.actionSystem.*
import com.intellij.openapi.actionSystem.impl.SimpleDataContext
import com.intellij.openapi.editor.impl.EditorImpl
import com.intellij.openapi.fileEditor.FileEditorManager
import com.intellij.openapi.fileEditor.FileEditorManagerEvent
import com.intellij.openapi.fileEditor.FileEditorManagerListener
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.createNestedDisposable
import com.intellij.openapi.vfs.VirtualFile
import com.intellij.openapi.wm.CustomStatusBarWidget
import com.intellij.openapi.wm.StatusBar
import com.intellij.openapi.wm.StatusBarWidget
import com.intellij.openapi.wm.StatusBarWidget.Multiframe
import com.intellij.openapi.wm.impl.status.EditorBasedWidget
import com.intellij.ui.ErrorLabel
import com.intellij.ui.JBColor
import com.intellij.ui.components.panels.OpaquePanel
import com.intellij.ui.popup.PopupFactoryImpl
import com.intellij.ui.popup.list.PopupListElementRenderer
import com.intellij.util.FontUtil
import com.intellij.util.ui.JBUI
import com.intellij.util.ui.UIUtil
import com.intellij.util.ui.UIUtil.DEFAULT_HGAP
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.SequentialLifetimes
import com.jetbrains.rdclient.document.getFirstEditableEntityId
import com.jetbrains.rider.UnityProjectDiscoverer
import com.jetbrains.rider.cpp.fileType.CppFileType
import com.jetbrains.rider.model.ContextInfo
import com.jetbrains.rider.model.EditableEntityId
import com.jetbrains.rider.model.ShaderContextData
import com.jetbrains.rider.model.ShaderContextDataBase
import com.jetbrains.rider.plugins.unity.UnityHost
import com.jetbrains.rider.plugins.unity.ideaInterop.fileTypes.shaderLab.ShaderLabFileType
import icons.UnityIcons
import java.awt.BorderLayout
import java.awt.event.MouseEvent
import java.awt.event.MouseListener
import javax.swing.JComponent
import javax.swing.JLabel
import javax.swing.JList
import javax.swing.JPanel


class ShaderWidget(project: Project) : EditorBasedWidget(project), CustomStatusBarWidget, Multiframe {

    private val statusBarComponent = JPanel(BorderLayout())
    private val label = JLabel(UnityIcons.FileTypes.ShaderLab)
    private val requestLifetime = SequentialLifetimes(project.lifetime)

    init {
        label.text = "..."
        statusBarComponent.isVisible = false
        statusBarComponent.add(label)
        statusBarComponent.addMouseListener(object : MouseListener {
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

        // do nothing for not unity projects
        if (UnityProjectDiscoverer.getInstance(project).isUnityProject) {
            project.messageBus.connect(project.lifetime.createNestedDisposable())
                .subscribe(FileEditorManagerListener.FILE_EDITOR_MANAGER, object : FileEditorManagerListener {
                    override fun selectionChanged(event: FileEditorManagerEvent) {
                        updateState((editor as? EditorImpl)?.virtualFile)
                    }
                })
        }
    }

    private fun updateState(file: VirtualFile?) {

        val lifetimeDef = requestLifetime.next()
        val host = UnityHost.getInstance(project)

        if (file == null || file.fileType !is CppFileType) {
            statusBarComponent.isVisible = false
            return
        }

        if (editor == null) {
            statusBarComponent.isVisible = false
            return
        }

        val id = editor?.document?.getFirstEditableEntityId(project)
        if (id == null) {
            statusBarComponent.isVisible = false
            return
        }


        host.model.requestCurrentContext.start(lifetimeDef.lifetime, id).result.advise(lifetimeDef.lifetime) {
            val result = it.unwrap()
            statusBarComponent.isVisible = true
            if (result is ShaderContextData)
                label.text = "${result.name} (${result.start}-${result.end})"
            else
                label.text = "Auto"
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
        val id = editor?.document?.getFirstEditableEntityId(project)
        if (id == null)
            return
        val host = UnityHost.getInstance(project)
        host.model.requestShaderContexts.start(lt, id).result.advise(lt) {
            val items = it.unwrap()
            val actions = createActions(host, id, items)
            val group = DefaultActionGroup().apply {

                addAll(actions)
            }
            val popup = ShaderContextPopup(group, SimpleDataContext.getProjectContext(project))
            popup.showInCenterOf(label)
        }
    }

    private fun createActions(host: UnityHost, id: EditableEntityId, items: List<ShaderContextDataBase>): List<ShaderContextSwitchAction> {
        val result = mutableListOf<ShaderContextSwitchAction>()
        for (item in items) {
            result.add(ShaderContextSwitchAction(project, id, host, item as ShaderContextData, label))
        }
        return result
    }
}