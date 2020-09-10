package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.actionSystem.*
import com.intellij.openapi.actionSystem.impl.SimpleDataContext
import com.intellij.openapi.project.Project
import com.intellij.openapi.wm.CustomStatusBarWidget
import com.intellij.openapi.wm.StatusBar
import com.intellij.openapi.wm.StatusBarWidget
import com.intellij.openapi.wm.StatusBarWidget.Multiframe
import com.intellij.openapi.wm.impl.status.EditorBasedWidget
import com.intellij.ui.ErrorLabel
import com.intellij.ui.JBColor
import com.intellij.ui.components.panels.OpaquePanel
import com.intellij.ui.popup.ActionPopupStep
import com.intellij.ui.popup.PopupFactoryImpl
import com.intellij.ui.popup.list.PopupListElementRenderer
import com.intellij.util.FontUtil
import com.intellij.util.ui.JBUI
import com.intellij.util.ui.UIUtil.DEFAULT_HGAP
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.fire
import com.jetbrains.rdclient.document.getFirstEditableEntityId
import com.jetbrains.rider.model.ContextInfo
import com.jetbrains.rider.model.EditableEntityId
import com.jetbrains.rider.model.ShaderContextData
import com.jetbrains.rider.plugins.unity.UnityHost
import icons.UnityIcons
import java.awt.BorderLayout
import java.awt.event.MouseEvent
import java.awt.event.MouseListener
import java.io.File
import java.nio.file.Path
import javax.swing.*
import kotlin.time.days


class ShaderWidget(project: Project) : EditorBasedWidget(project), CustomStatusBarWidget, Multiframe {
    private var myStatusBar: StatusBar? = null


    private class SolutionAnalysisStatusBarTextPanel : JLabel()

    private val statusBarComponent = JPanel(BorderLayout())
    private val label = JLabel(UnityIcons.FileTypes.ShaderLab)
    init {
        label.text = "..."
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
    }

    override fun ID(): String = "ShaderWidget"
//    override fun copy(): StatusBarWidget {
//        return ShaderWidget(project)
//    }

    override fun getComponent(): JComponent {
        return statusBarComponent
    }




    override fun copy(): StatusBarWidget {
        return ShaderWidget(project)
    }


     fun showPopup(label: JLabel) {

        val lt : Lifetime = Lifetime.Eternal
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


    private fun createActions(host: UnityHost, id: EditableEntityId, items: List<ShaderContextData>) : List<ShaderContextSwitchAction> {
        val result = mutableListOf<ShaderContextSwitchAction>()
        for (item in items) {
            result.add(ShaderContextSwitchAction(project, id, host, item))
        }
        return result
    }
//    override fun getSelectedValue(): String? {
//        return "Auto"
//    }

    private class ShaderContextSwitchAction(val project: Project, val id: EditableEntityId, val host: UnityHost, val data: ShaderContextData) : AnAction(data.name + "(${data.start}-${data.end})") {
        override fun actionPerformed(p0: AnActionEvent) {
            host.model.changeContext.fire(ContextInfo(id, data.path, data.start, data.end))

        }
    }

    private class ShaderContextPopup(private val group: ActionGroup, private val dataContext: DataContext) :
        PopupFactoryImpl.ActionGroupPopup("Shader Context", group, dataContext, false, false,
            false, true, null, 10, null, null)
    {
        init {
            setSpeedSearchAlwaysShown()
        }

        override fun getListElementRenderer() = object : PopupListElementRenderer<PopupFactoryImpl.ActionItem>(this) {
            private var myInfoLabel: ErrorLabel? = null

            override fun createItemComponent(): JComponent {
                myTextLabel = ErrorLabel()
                myTextLabel.isOpaque = true
                myTextLabel.border = JBUI.Borders.empty(1)

                myInfoLabel = ErrorLabel()
                myInfoLabel!!.setOpaque(true)
                myInfoLabel!!.setBorder(JBUI.Borders.empty(1, DEFAULT_HGAP, 1, 1))
                myInfoLabel!!.setFont(FontUtil.minusOne(myInfoLabel!!.getFont()))

                val textPanel: JPanel = OpaquePanel(BorderLayout(),  JBColor.WHITE)
                textPanel.add(myTextLabel, BorderLayout.WEST)
                textPanel.add(myInfoLabel, BorderLayout.CENTER)

                return layoutComponent(textPanel)
            }

            override fun customizeComponent(list: JList<out PopupFactoryImpl.ActionItem>?, value: PopupFactoryImpl.ActionItem?, isSelected: Boolean) {
                super.customizeComponent(list, value, isSelected)
                val action = value?.action ?: return
                if (action is ShaderContextSwitchAction) {
                    myInfoLabel!!.setText(action.data.folder);
                }
            }
        }
    }

}