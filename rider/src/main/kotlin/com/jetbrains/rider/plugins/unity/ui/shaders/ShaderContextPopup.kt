package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.icons.AllIcons
import com.intellij.ide.BrowserUtil
import com.intellij.openapi.actionSystem.ActionGroup
import com.intellij.openapi.actionSystem.ActionPlaces
import com.intellij.openapi.actionSystem.DataContext
import com.intellij.openapi.actionSystem.impl.PresentationFactory
import com.intellij.openapi.ui.popup.IconButton
import com.intellij.ui.ErrorLabel
import com.intellij.ui.InplaceButton
import com.intellij.ui.JBColor
import com.intellij.ui.components.panels.OpaquePanel
import com.intellij.ui.popup.ActionPopupOptions
import com.intellij.ui.popup.PopupFactoryImpl
import com.intellij.ui.popup.list.PopupListElementRenderer
import com.intellij.util.ui.JBUI
import com.intellij.util.ui.UIUtil
import com.jetbrains.rd.util.reactive.IProperty
import com.jetbrains.rider.plugins.unity.UnityBundle
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ShaderContextData
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import icons.UnityIcons
import java.awt.BorderLayout
import javax.swing.JComponent
import javax.swing.JLabel
import javax.swing.JList
import javax.swing.JPanel


class ShaderContextPopup(group: ActionGroup, dataContext: DataContext, currentContextMode: IProperty<ShaderContextData?>) :
    PopupFactoryImpl.ActionGroupPopup(
        null, UnityUIBundle.message("popup.title.include.context.from"), group, dataContext,
        ActionPlaces.getPopupPlace("ShaderContextPopup"), PresentationFactory(),
        ActionPopupOptions.create(false, false, false, true, 10, false) {
            when {
                it is ShaderAutoContextSwitchAction && currentContextMode.value == null -> true
                it is ShaderContextSwitchAction && currentContextMode.value?.path == it.data.path &&
                currentContextMode.value?.startLine == it.data.startLine -> true
                else -> false
            }
        }, null) {
    init {
        setSpeedSearchAlwaysShown()
        title.setButtonComponent(InplaceButton(IconButton(UnityBundle.message("tooltip.help"), AllIcons.Actions.Help)) {
            BrowserUtil.open("https://jb.gg/unity-shader-context")
        }, JBUI.Borders.emptyRight(2))
    }

    override fun getListElementRenderer() = object : PopupListElementRenderer<PopupFactoryImpl.ActionItem>(this) {
        private var myInfoLabel: JLabel? = null
        private var myPosLabel: JLabel? = null

        override fun createItemComponent(): JComponent {
            myTextLabel = ErrorLabel()
            myTextLabel.isOpaque = true
            myTextLabel.border = JBUI.Borders.empty(1)

            myInfoLabel = JLabel()
            myInfoLabel!!.foreground = UIUtil.getLabelDisabledForeground()
            myInfoLabel!!.setOpaque(true)
            myInfoLabel!!.setBorder(JBUI.Borders.empty(1, UIUtil.DEFAULT_HGAP, 1, 1))

            myPosLabel = JLabel()
            myPosLabel!!.foreground = UIUtil.getLabelDisabledForeground()
            myPosLabel!!.setOpaque(true)
            myPosLabel!!.setBorder(JBUI.Borders.empty(1, 0, 1, 1))


            val textPanel: JPanel = OpaquePanel(BorderLayout(), JBColor.WHITE)
            textPanel.add(myTextLabel, BorderLayout.WEST)
            textPanel.add(myPosLabel, BorderLayout.CENTER)
            textPanel.add(myInfoLabel, BorderLayout.EAST)

            return layoutComponent(textPanel)
        }

        override fun customizeComponent(list: JList<out PopupFactoryImpl.ActionItem>?,
                                        value: PopupFactoryImpl.ActionItem?,
                                        isSelected: Boolean) {
            super.customizeComponent(list, value, isSelected)
            myTextLabel.icon = UnityIcons.FileTypes.ShaderLab

            val action = value?.action ?: return
            if (action is ShaderContextSwitchAction) {
                myInfoLabel!!.text = action.data.folder
                myPosLabel!!.text = ":" + action.data.startLine
            }
            else {
                myInfoLabel!!.text = ""
                myPosLabel!!.text = ""
            }
        }
    }
}