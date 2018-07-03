package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.intellij.ui.SearchTextField
import com.intellij.util.ui.JBUI
import com.jetbrains.rider.ui.RiderUI
import java.awt.Dimension
import java.awt.event.KeyEvent

class LogSmartSearchField() : SearchTextField(false) { //val viewModel: RiderNuGetFacade
    init {
        // TODO: consult aakinshin or grann when this inevitably breaks
        RiderUI.setHeight(this, 25)
        minimumSize = Dimension(JBUI.scale(100), minimumSize.height)

        font = RiderUI.BigFont
        textEditor.emptyText.isShowAboveCenter = true

        RiderUI.overrideKeyStroke(textEditor, "shift ENTER", { transferFocusBackward() })
        RiderUI.overrideKeyStroke(textEditor, "ENTER", { goToList() })
    }

    /**
     * Trying to navigate to the first element in the brief list
     * @return true in case of success; false if the list is empty
     */
    var goToList: () -> Boolean = { false }

    var focusGained: () -> Unit = {}
    override fun onFocusGained() = focusGained()

    override fun preprocessEventForTextField(e: KeyEvent): Boolean {
        if (e.keyCode == KeyEvent.VK_DOWN || e.keyCode == KeyEvent.VK_PAGE_DOWN) {
            goToList() // trying to navigate to the least instead of "show history"
            e.consume() // suppress default "show history" logic anyway
            return true
        }
        return super.preprocessEventForTextField(e)
    }

    override fun getBackground() = RiderUI.HeaderBackgroundColor
}