package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.intellij.execution.filters.Filter
import com.intellij.execution.filters.TextConsoleBuilderFactory
import com.intellij.execution.impl.ConsoleViewImpl
import com.intellij.execution.ui.ConsoleViewContentType
import com.intellij.openapi.extensions.Extensions
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Disposer
import com.intellij.ui.JBSplitter
import com.intellij.ui.components.JBScrollPane
import com.intellij.unscramble.AnalyzeStacktraceUtil
import com.jetbrains.rider.plugins.unity.ProjectCustomDataHost
import com.jetbrains.rider.plugins.unity.RdLogEvent
import com.jetbrains.rider.ui.RiderSimpleToolWindowWithTwoToolbarsPanel
import com.jetbrains.rider.ui.RiderUI
import java.awt.BorderLayout

class UnityLogPanelView(project: Project, val model: UnityLogPanelModel, projectCustomDataHost: ProjectCustomDataHost) {
    private val console = TextConsoleBuilderFactory.getInstance()
        .createBuilder(project)
        .filters(*Extensions.getExtensions<Filter>(AnalyzeStacktraceUtil.EP_NAME, project))
        .console as ConsoleViewImpl

    private val eventList = UnityLogPanelEventList().apply {
        addListSelectionListener {
            console.clear()
            if (selectedIndex >= 0) {
                console.print(selectedValue.stackTrace, ConsoleViewContentType.NORMAL_OUTPUT)
                console.scrollTo(0)
            }
        }
    }

    private val leftToolbar = UnityLogPanelToolbarBuilder.createLeftToolbar(projectCustomDataHost)

    private val topToolbar = UnityLogPanelToolbarBuilder.createTopToolbar(model)

    private val content = JBSplitter().apply {
        proportion = 1f / 2
        firstComponent = JBScrollPane(eventList)
        secondComponent = RiderUI.borderPanel {
            add(console.component, BorderLayout.CENTER)
            console.editor.settings.isCaretRowShown = true
            console.editor.settings.isUseSoftWraps = true
            console.clear()
            console.allowHeavyFilters()
        }
    }

    val panel = RiderSimpleToolWindowWithTwoToolbarsPanel(leftToolbar, topToolbar, content)

    // TODO: optimize
    private fun refreshList(newEvents: List<RdLogEvent>) {
        eventList.riderModel.clear()
        for (event in newEvents)
            eventList.riderModel.addElement(event)
    }

    init {
        Disposer.register(project, console)
        model.onChanged.advise(model.lifetime) { refreshList(it) }
        model.fire()
    }
}