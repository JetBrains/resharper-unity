package com.jetbrains.rider.plugins.unity.toolWindow.log

import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEvent
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEventMode
import com.jetbrains.rider.plugins.unity.editorPlugin.model.RdLogEventType
import com.jetbrains.rider.util.lifetime.Lifetime
import com.jetbrains.rider.util.reactive.Property
import com.jetbrains.rider.util.reactive.Signal
import com.jetbrains.rider.util.reactive.fire

class UnityLogPanelModel(val lifetime: Lifetime, val project: com.intellij.openapi.project.Project) {
    private val lock = Object()

    inner class TypeFilters {
        private var showErrors = true
        private var showWarnings = true
        private var showMessages = true

        fun getShouldBeShown(type: RdLogEventType) = when (type) {
            RdLogEventType.Error -> showErrors
            RdLogEventType.Warning -> showWarnings
            RdLogEventType.Message -> showMessages
        }

        fun setShouldBeShown(type: RdLogEventType, value: Boolean) = when (type) {
            RdLogEventType.Error -> {
                synchronized(lock) { showErrors = value }
                onChanged.fire()
            }
            RdLogEventType.Warning -> {
                synchronized(lock) { showWarnings = value }
                onChanged.fire()
            }
            RdLogEventType.Message -> {
                synchronized(lock) { showMessages = value }
                onChanged.fire()
            }
        }

        val onChanged = Signal.Void()
    }

    inner class ModeFilters {
        private var showEdit = true
        private var showPlay = true

        fun getShouldBeShown(mode: RdLogEventMode) = when (mode) {
            RdLogEventMode.Edit -> showEdit
            RdLogEventMode.Play -> showPlay
        }

        fun setShouldBeShown(mode: RdLogEventMode, value: Boolean) = when (mode) {
            RdLogEventMode.Edit -> {
                synchronized(lock) { showEdit = value }
                onChanged.fire()
            }
            RdLogEventMode.Play -> {
                synchronized(lock) { showPlay = value }
                onChanged.fire()
            }
        }

        val onChanged = Signal.Void()
    }

    inner class Events {
        val allEvents = ArrayList<RdLogEvent>()

        fun clear() {
            synchronized(lock) { allEvents.clear() }
            selectedItem = null
            onChanged.fire()
            onCleared.fire()
        }

        fun addEvent(event: RdLogEvent) {
            synchronized(lock) {
                if (allEvents.count()>1000)
                {
                    clear()
                }
                allEvents.add(event)
            }

            if (isVisibleEvent(event))
                onAdded.fire(event)
        }

        val onChanged = Signal.Void()
    }

    private fun isVisibleEvent(event: RdLogEvent):Boolean
    {
        return typeFilters.getShouldBeShown(event.type) && modeFilters.getShouldBeShown(event.mode);
    }

    private fun getVisibleEvents(): List<RdLogEvent> {
        synchronized(lock) {
            return events.allEvents
                .filter { isVisibleEvent(it) }
        }
    }

    val typeFilters = TypeFilters()
    val modeFilters = ModeFilters()
    val events = Events()
    var mergeSimilarItems = Property<Boolean>(false)

    val onAdded = Signal<RdLogEvent>()
    val onChanged = Signal<List<RdLogEvent>>()
    val onCleared = Signal.Void()

    fun fire() = onChanged.fire(getVisibleEvents())

    var selectedItem : LogPanelItem? = null

    init {
        typeFilters.onChanged.advise(lifetime) { fire() }
        modeFilters.onChanged.advise(lifetime) { fire() }
        events.onChanged.advise(lifetime) { fire() }
        mergeSimilarItems.advise(lifetime) { fire() }
    }
}