package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.intellij.execution.filters.Filter
import com.intellij.execution.filters.TextConsoleBuilderFactory
import com.intellij.execution.impl.ConsoleViewImpl
import com.intellij.execution.ui.ConsoleViewContentType
import com.intellij.openapi.extensions.Extensions
import com.intellij.openapi.project.Project
import com.intellij.openapi.util.Disposer
import com.intellij.ui.DoubleClickListener
import com.intellij.ui.JBSplitter
import com.intellij.ui.components.JBScrollPane
import com.intellij.unscramble.AnalyzeStacktraceUtil
import com.jetbrains.rider.plugins.unity.ProjectCustomDataHost
import com.jetbrains.rider.plugins.unity.RdLogEvent
import com.jetbrains.rider.ui.RiderGroupingEvent
import com.jetbrains.rider.ui.RiderSimpleToolWindowWithTwoToolbarsPanel
import com.jetbrains.rider.ui.RiderUI
import com.jetbrains.rider.util.lifetime.Lifetime
import com.jetbrains.rider.util.reactive.whenTrue
import java.awt.BorderLayout
import java.awt.event.KeyAdapter
import java.awt.event.KeyEvent
import java.awt.event.MouseEvent

class UnityLogPanelView(project: Project, val logModel: UnityLogPanelModel, projectCustomDataHost: ProjectCustomDataHost) {
    private val console = TextConsoleBuilderFactory.getInstance()
        .createBuilder(project)
        .filters(*Extensions.getExtensions<Filter>(AnalyzeStacktraceUtil.EP_NAME, project))
        .console as ConsoleViewImpl

    private val eventList = UnityLogPanelEventList().apply {
        addListSelectionListener {
            console.clear()
            if (selectedIndex >= 0) {
                console.print(selectedValue.message + "\n", ConsoleViewContentType.NORMAL_OUTPUT)
                console.print(selectedValue.stackTrace, ConsoleViewContentType.NORMAL_OUTPUT)
                console.scrollTo(0)
            }
        }
        val eventList1 = this
        addKeyListener(object: KeyAdapter() {
            override fun keyPressed(e: KeyEvent?) {
                if (e?.keyCode == KeyEvent.VK_ENTER) {
                    e.consume()
                    getNavigatableForSelected(eventList1, project)?.navigate(true)
                }
            }
        })
        object: DoubleClickListener() {
            override fun onDoubleClick(event: MouseEvent?): Boolean {
                getNavigatableForSelected(eventList1, project)?.navigate(true)
                return true
            }
        }.installOn(this)

        projectCustomDataHost.play.whenTrue(logModel.lifetime) {
            logModel.events.clear()
        }
    }

    private val leftToolbar = UnityLogPanelToolbarBuilder.createLeftToolbar(projectCustomDataHost)

    private val topToolbar = UnityLogPanelToolbarBuilder.createTopToolbar(logModel)

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
        {
            eventList.riderModel.addElement(event)
        }

        if (eventList.isSelectionEmpty)
            eventList.selectedIndex = eventList.itemsCount-1
        eventList.ensureIndexIsVisible(eventList.itemsCount-1)
    }

    init {
        Disposer.register(project, console)
        logModel.onChanged.advise(logModel.lifetime) { refreshList(it) }
        logModel.fire()
    }
}