package com.jetbrains.rider.plugins.unity.log.ui

import com.intellij.execution.filters.TextConsoleBuilderFactory
import com.intellij.execution.impl.ConsoleViewImpl
import com.intellij.execution.ui.ConsoleViewContentType
import com.intellij.ide.OccurenceNavigator
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.SimpleToolWindowPanel
import com.intellij.openapi.util.Disposer
import com.jetbrains.rider.plugins.unity.RdLogEventType
import com.jetbrains.rider.projectView.ProjectModelViewHost
import com.jetbrains.rider.util.lifetime.Lifetime
import java.awt.CardLayout
import javax.swing.JPanel

class LogPanel(project: Project, projectModelViewHost: ProjectModelViewHost, lifetime: Lifetime)
    : SimpleToolWindowPanel(false), OccurenceNavigator {
    companion object{
        private val consolePanelName = "console"
       // private val buildEventsPanelName = "events"
    }

    private val console = TextConsoleBuilderFactory.getInstance().createBuilder(project).console as ConsoleViewImpl
    //private val buildEvents = RiderBuildResultTreePanel(project, projectModelViewHost)
    var isConsoleActivated: Boolean = true
        private set
//    private val hasErrors: Boolean get() = buildEvents.hasErrors
//    val hasEvents: Boolean get() = buildEvents.hasEvents

    private var currentOccurrenceNavigator: OccurenceNavigator? = null
    private val layout = CardLayout()
    private val container: JPanel

    init {
        // TODO: Remove when fixed EditorFactoryImpl editor checking
        Disposer.register(project, console)
        setProvideQuickActions(true)
        container = JPanel(layout).apply {
            add(console.component, consolePanelName)
            //add(buildEvents, buildEventsPanelName)
        }
        showConsole()
        setContent(container)
        lifetime.add {
            console.dispose()
        }
    }

    fun clearConsole() = console.clear()

//    fun clearTree() {
//        buildEvents.clearTree()
//    }

    fun showConsole() {
        if (isConsoleActivated)
            return
        currentOccurrenceNavigator = console
        layout.show(container, consolePanelName)
        isConsoleActivated = true
    }

//    fun showEvents() {
//        if (!isConsoleActivated)
//            return
//        currentOccurrenceNavigator = buildEvents
//        buildEvents.activate()
//        layout.show(container, buildEventsPanelName)
//        isConsoleActivated = false
//    }

//    fun addBuildEvent(buildEvent: BuildEvent) {
//        buildEvents.addBuildEvent(buildEvent)
//    }

    private fun messageKindToConsoleContentType(type: RdLogEventType): ConsoleViewContentType =
        when (type) {
            RdLogEventType.Error -> ConsoleViewContentType.ERROR_OUTPUT
            RdLogEventType.Warning -> ConsoleViewContentType.NORMAL_OUTPUT
            RdLogEventType.Message -> ConsoleViewContentType.NORMAL_OUTPUT
        }

    fun addOutputMessage(message: String, messageKind: RdLogEventType) {
        console.print(message, messageKindToConsoleContentType(messageKind))
    }

//    fun addStatusMessage(message: String) {
//        console.print(message, if (hasErrors) ConsoleViewContentType.ERROR_OUTPUT else ConsoleViewContentType.NORMAL_OUTPUT)
//    }

    override fun hasNextOccurence(): Boolean {
        val navigator = currentOccurrenceNavigator ?: return false
        return navigator.hasNextOccurence()
    }

    override fun hasPreviousOccurence(): Boolean {
        val navigator = currentOccurrenceNavigator ?: return false
        return navigator.hasPreviousOccurence()
    }

    override fun goNextOccurence(): OccurenceNavigator.OccurenceInfo? {
        val navigator = currentOccurrenceNavigator ?: return null
        return navigator.goNextOccurence()
    }

    override fun goPreviousOccurence(): OccurenceNavigator.OccurenceInfo? {
        val navigator = currentOccurrenceNavigator ?: return null
        return navigator.goPreviousOccurence()
    }

    override fun getNextOccurenceActionName(): String? {
        val navigator = currentOccurrenceNavigator ?: return null
        return navigator.nextOccurenceActionName
    }

    override fun getPreviousOccurenceActionName(): String? {
        val navigator = currentOccurrenceNavigator ?: return null
        return navigator.previousOccurenceActionName
    }
}