package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.openapi.actionSystem.ActionGroup
import com.intellij.openapi.actionSystem.DataContext
import com.intellij.ui.ErrorLabel
import com.intellij.ui.JBColor
import com.intellij.ui.components.panels.OpaquePanel
import com.intellij.ui.popup.PopupFactoryImpl
import com.intellij.ui.popup.list.PopupListElementRenderer
import com.intellij.util.FontUtil
import com.intellij.util.ui.JBUI
import com.intellij.util.ui.UIUtil
import icons.UnityIcons
import java.awt.BorderLayout
import javax.swing.JComponent
import javax.swing.JLabel
import javax.swing.JList
import javax.swing.JPanel

class ShaderContextPopup(private val group: ActionGroup, private val dataContext: DataContext) :
    PopupFactoryImpl.ActionGroupPopup("Shader Context", group, dataContext, false, false,
        false, true, null, 10, null, null)
{
    init {
        setSpeedSearchAlwaysShown()
    }

    override fun getListElementRenderer() = object : PopupListElementRenderer<PopupFactoryImpl.ActionItem>(this) {
        private var myInfoLabel: JLabel? = null

        override fun createItemComponent(): JComponent {
            myTextLabel = ErrorLabel()
            myTextLabel.isOpaque = true
            myTextLabel.border = JBUI.Borders.empty(1)

            myInfoLabel = JLabel()
            myInfoLabel!!.foreground = UIUtil.getLabelDisabledForeground()
            myInfoLabel!!.setOpaque(true)
            myInfoLabel!!.setBorder(JBUI.Borders.empty(1, UIUtil.DEFAULT_HGAP, 1, 1))
            myInfoLabel!!.setFont(FontUtil.minusOne(myInfoLabel!!.getFont()))

            val textPanel: JPanel = OpaquePanel(BorderLayout(), JBColor.WHITE)
            textPanel.add(myTextLabel, BorderLayout.WEST)
            textPanel.add(myInfoLabel, BorderLayout.EAST)

            return layoutComponent(textPanel)
        }

        override fun customizeComponent(list: JList<out PopupFactoryImpl.ActionItem>?, value: PopupFactoryImpl.ActionItem?, isSelected: Boolean) {
            super.customizeComponent(list, value, isSelected)
            myTextLabel.icon = UnityIcons.FileTypes.ShaderLab

            val action = value?.action ?: return
            if (action is ShaderContextSwitchAction) {
                myInfoLabel!!.setText(action.data.folder);
            }
        }
    }
}