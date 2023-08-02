package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.actionSystem.AnAction
import com.intellij.openapi.actionSystem.DefaultActionGroup
import com.intellij.openapi.actionSystem.impl.SimpleDataContext
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.project.Project
import com.jetbrains.rd.ide.model.RdDocumentId
import com.jetbrains.rd.platform.util.lifetime
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.SequentialLifetimes
import com.jetbrains.rd.util.reactive.IProperty
import com.jetbrains.rd.util.reactive.Property
import com.jetbrains.rdclient.document.getFirstDocumentId
import com.jetbrains.rider.cpp.fileType.HlslHeaderFileType
import com.jetbrains.rider.cpp.fileType.HlslSourceFileType
import com.jetbrains.rider.editors.resolveContextWidget.RiderResolveContextWidget
import com.jetbrains.rider.plugins.unity.FrontendBackendHost
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ShaderContextData
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ShaderContextDataBase
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import icons.UnityIcons
import org.jetbrains.annotations.Nls
import java.awt.BorderLayout
import java.awt.Component
import java.awt.event.MouseAdapter
import java.awt.event.MouseEvent
import javax.swing.JLabel
import javax.swing.JPanel


class ShaderWidget(val project: Project, val editor: Editor) : JPanel(BorderLayout()), RiderResolveContextWidget {
    private val label = JLabel(UnityIcons.FileTypes.ShaderLab)
    private val requestLifetime = SequentialLifetimes(project.lifetime)
    private val currentContextMode : IProperty<ShaderContextData?> = Property(null)
    private var documentId: RdDocumentId? = null

    companion object {
        @Nls
        private fun getContextPresentation(data : ShaderContextData) = "${data.name}:${data.startLine}"
    }

    init {
        label.text = "..."
        isVisible = false
        add(label)
        label.addMouseListener(object : MouseAdapter() {
            override fun mouseReleased(e: MouseEvent?) {
                showPopup(label)
            }
        })

        currentContextMode.advise(project.lifetime) {
            if (it == null) {
                label.text = UnityUIBundle.message("auto")
                label.toolTipText = UnityUIBundle.message("default.file.and.symbol.context")
            } else {
                label.text = getContextPresentation(it)
                label.toolTipText = UnityUIBundle.message("file.and.symbol.context.derived.from.include.at.context", getContextPresentation(it))
            }
            updateState()
        }
    }

    private fun getHlslDocumentId(): RdDocumentId? {
        val file = editor.virtualFile
        if (file == null || !file.fileType.let { it == HlslSourceFileType || it == HlslHeaderFileType }) {
            return null
        }

        return editor.document.getFirstDocumentId(project)
    }

    private fun updateState() {
        val newDocumentId = getHlslDocumentId()
        if (newDocumentId == documentId) return

        val lifetime = requestLifetime.next().lifetime
        documentId = newDocumentId
        if (newDocumentId == null) {
            isVisible = false
            return
        }

        val host = FrontendBackendHost.getInstance(project)
        host.model.requestCurrentContext.start(lifetime, newDocumentId).result.advise(lifetime) {
            val result = it.unwrap()
            isVisible = true
            if (result is ShaderContextData)
                currentContextMode.value = result
            else
                currentContextMode.value = null
        }
    }

    override val component: Component = this
    override fun update() = updateState()

    fun showPopup(label: JLabel) {
        val lt: Lifetime = Lifetime.Eternal
        val id = editor.document.getFirstDocumentId(project) ?: return
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