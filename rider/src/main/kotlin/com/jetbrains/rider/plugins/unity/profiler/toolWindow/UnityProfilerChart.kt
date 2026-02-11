package com.jetbrains.rider.plugins.unity.profiler.toolWindow

import com.intellij.icons.AllIcons
import com.intellij.openapi.actionSystem.*
import com.intellij.openapi.application.EDT
import com.intellij.openapi.project.DumbAwareAction
import com.intellij.openapi.ui.ComboBox
import com.intellij.ui.*
import com.intellij.ui.charts.*
import com.intellij.ui.components.JBLabel
import com.intellij.util.ui.JBUI
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.lifetime.intersect
import com.jetbrains.rd.util.lifetime.isAlive
import com.jetbrains.rider.plugins.unity.model.ProfilerThread
import com.jetbrains.rider.plugins.unity.profiler.UnityProfilerStyle
import com.jetbrains.rider.plugins.unity.profiler.viewModels.UnityProfilerChartViewModel
import com.jetbrains.rider.plugins.unity.ui.UnityUIBundle
import kotlinx.coroutines.*
import java.awt.*
import java.awt.event.MouseAdapter
import java.awt.event.MouseEvent
import java.awt.geom.Path2D
import javax.swing.Icon
import javax.swing.JComponent
import javax.swing.JPanel
import kotlin.math.roundToInt

class UnityProfilerChart(
    private val viewModel: UnityProfilerChartViewModel, 
    private val lifetime: Lifetime) {

    private val chart = lineChart<Double> {
        dataset {
            stepped = LineStepped.NONE
            lineColor = UnityProfilerStyle.chartLine
            stroke = BasicStroke(UnityProfilerStyle.chartLineThickness)
            fillColor = UnityProfilerStyle.chartFill
        }
        ranges {
            yMin = 0.0
            yMax = UnityProfilerChartViewModel.Y_STEPS.last()
            xMin = 0
            xMax = 0
        }
        grid {
            yLines = enumerator(*UnityProfilerChartViewModel.Y_STEPS.toTypedArray())
            yPainter {
                label = ""
                paintLine = false
            }
        }
        margins {
            top = 20
            bottom = 10
            left = 10
            right = 10
        }
        background = ColorUtil.withAlpha(JBColor.WHITE, 0.0)
    }.apply {
        gridColor = UnityProfilerStyle.chartGrid
        val overlay = object : Overlay<CategoryLineChart<Double>>() {
            // Cache fonts to avoid creating new instances on every paint
            private val labelFont12 = JBUI.Fonts.label().deriveFont(JBUI.scale(12f))
            private val labelFont10 = JBUI.Fonts.label().deriveFont(JBUI.scale(10f))

            override fun paintComponent(g: Graphics2D) {
                val chart = chart
                val xy = chart.findMinMax()
                if (!xy.isInitialized) return
                val data = viewModel.frameDurations.value
                if (data.isEmpty()) return
                val gridW = chart.gridWidth
                val gridH = chart.gridHeight

                val xMin = (xy.xMin as Number).toDouble()
                val xMax = (xy.xMax as Number).toDouble()
                val yMin = (xy.yMin as Number).toDouble()
                val yMax = (xy.yMax as Number).toDouble()


                fun drawFrameInfo(globalIndex: Int, color: Color, isPersistent: Boolean) {
                    val localIndex = globalIndex - viewModel.startIndex.value
                    if (localIndex !in data.indices) return
                    val frameMs = data[localIndex]
                    val px = if (xMax > xMin) (gridW * (localIndex.toDouble() - xMin) / (xMax - xMin)).toInt() + chart.margins.left else chart.margins.left
                    val py = (gridH - gridH * (frameMs - yMin) / (yMax - yMin)).toInt() + chart.margins.top

                    val oldStroke = g.stroke
                    if (isPersistent) {
                        g.stroke = BasicStroke(UnityProfilerStyle.chartLineThickness)
                    } else {
                        g.stroke = BasicStroke(1.0f, BasicStroke.CAP_BUTT, BasicStroke.JOIN_MITER, 10.0f, floatArrayOf(5.0f), 0.0f)
                    }

                    g.color = color
                    g.drawLine(px, chart.margins.top, px, chart.height - chart.margins.bottom)

                    if (isPersistent) {
                        val bottomY = chart.height - chart.margins.bottom
                        val trianglePath = Path2D.Double()
                        trianglePath.moveTo(px.toDouble(), bottomY.toDouble())
                        trianglePath.lineTo(px.toDouble() - 4.0, bottomY.toDouble() + 4.0)
                        trianglePath.lineTo(px.toDouble() + 4.0, bottomY.toDouble() + 4.0)
                        trianglePath.closePath()
                        g.fill(trianglePath)
                    }

                    g.stroke = oldStroke

                    if (py >= chart.margins.top) {
                        g.fillOval(px - 2, py - 2, 4, 4)
                    }
                }

                viewModel.selectedFrameIndex.value?.let { drawFrameInfo(it, UnityProfilerStyle.chartSelection, true) }

                fun drawFrameLabel(globalIndex: Int) {
                    val localIndex = globalIndex - viewModel.startIndex.value
                    if (localIndex !in data.indices) return
                    val frameMs = data[localIndex]
                    val px = if (xMax > xMin) (gridW * (localIndex.toDouble() - xMin) / (xMax - xMin)).toInt() + chart.margins.left else chart.margins.left

                    val label = "%.2fms".format(frameMs)
                    g.font = labelFont12
                    val bounds = g.fontMetrics.getStringBounds(label, g)
                    val labelX = (px - bounds.width / 2).toInt().coerceIn(chart.margins.left, chart.width - chart.margins.right - bounds.width.toInt())
                    
                    // Background for label
                    g.color = ColorUtil.withAlpha(UnityProfilerStyle.chartBackground, 0.7)
                    g.fillRoundRect(labelX - 4, chart.margins.top - (bounds.height).toInt() - 2, bounds.width.toInt() + 8, bounds.height.toInt(), 4, 4)

                    g.color = UnityProfilerStyle.chartSelection
                    g.drawString(label, labelX, chart.margins.top - 4)
                }
                viewModel.selectedFrameIndex.value?.let { drawFrameLabel(it) }

                // Draw horizontal grid lines and labels
                val oldStroke = g.stroke
                var lastLabelY: Int? = null
                var labelsDrawn = 0

                val sortedYLines = UnityProfilerChartViewModel.Y_STEPS.sortedDescending()
                for (yValue in sortedYLines) {
                    if (yValue > yMax) continue
                    val py = (gridH - gridH * (yValue.toDouble() - yMin) / (yMax - yMin)).toInt() + chart.margins.top
                    
                    val label = "%.0fms".format(yValue)
                    g.font = labelFont10
                    val currentMetrics = g.fontMetrics
                    val labelW = currentMetrics.stringWidth(label)
                    val labelH = currentMetrics.height
                    val labelX = chart.width - chart.margins.right - labelW - 5
                    val labelY = py + labelH / 2 - currentMetrics.descent
                    
                    // Hide non-informative labels and their gridlines (overlap check)
                    // We want to show at least 2 marks below the max if possible.
                    // "labelsDrawn < 3" ensures we try to show yMax and two lines below it.
                    // On Retina displays, labels might be closer than labelH in pixels, so we allow slight overlap for initial marks.
                    val isInitialLabel = labelsDrawn < 3
                    val minDistance = if (isInitialLabel) labelH * 0.7 else labelH + 2.0
                    
                    if (lastLabelY == null || (labelY - lastLabelY) > minDistance) {
                        g.color = UnityProfilerStyle.chartGrid
                        g.stroke = BasicStroke(1.0f, BasicStroke.CAP_BUTT, BasicStroke.JOIN_MITER, 1.0f, floatArrayOf(2.0f, 2.0f), 0.0f)
                        g.drawLine(chart.margins.left, py, chart.width - chart.margins.right, py)

                        g.color = ColorUtil.withAlpha(UnityProfilerStyle.chartBackground, 0.8)
                        g.fillRoundRect(labelX - 2, py - labelH / 2, labelW + 4, labelH, 4, 4)
                        
                        g.color = UnityProfilerStyle.gridLabelForeground
                        g.drawString(label, labelX, labelY)
                        lastLabelY = labelY
                        labelsDrawn++
                    }
                }
                g.stroke = oldStroke

                mouseLocation?.toChartSpace()?.let { mouse ->
                    val xMin = (xy.xMin as Number).toDouble()
                    val xMax = (xy.xMax as Number).toDouble()
                    if (xMax <= xMin) return@let

                    val localIndex = (xMin + (mouse.x.toDouble() / gridW) * (xMax - xMin)).roundToInt().coerceIn(xMin.toInt(), xMax.toInt())
                    val globalIndex = localIndex + viewModel.startIndex.value
                    drawFrameInfo(globalIndex, UnityProfilerStyle.chartSelection, false)
                    if (globalIndex != viewModel.selectedFrameIndex.value) {
                        drawFrameLabel(globalIndex)
                    }
                }
            }
        }
        overlays = listOf(overlay)

        component.addMouseListener(object : MouseAdapter() {
            override fun mouseClicked(e: MouseEvent) {
                val mouse = e.point
                val chartWidth = component.width - (margins.left + margins.right)
                val xInGrid = mouse.x - margins.left
                if (xInGrid < 0 || xInGrid > chartWidth) return

                val count = viewModel.frameDurations.value.size
                if (count <= 1) return
                val index = (xInGrid.toDouble() / chartWidth * (count - 1)).roundToInt().coerceIn(0, count - 1)
                viewModel.selectFrame(index + viewModel.startIndex.value)
            }
        })

        component.preferredSize = Dimension(0, JBUI.scale(150))
        component.isOpaque = false
    }

    private val chartComponent = object : JPanel(BorderLayout()) {
        init {
            isOpaque = false
            add(chart.component)
        }

        override fun paintComponent(g: Graphics) {
            val g2 = g.create() as Graphics2D
            g2.setRenderingHint(RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_ON)
            g2.color = UnityProfilerStyle.chartBackground
            g2.fillRoundRect(0, 0, width, height, JBUI.scale(12), JBUI.scale(12))
            g2.dispose()
        }
    }

    private val threadSelector = object : ComboBox<ProfilerThread>() {
        override fun getPreferredSize(): Dimension {
            val size = super.getPreferredSize()
            return Dimension(size.width.coerceIn(JBUI.scale(150), JBUI.scale(250)), size.height)
        }
    }.apply {
        renderer = SimpleListCellRenderer.create("", { it.name })
        addActionListener {
            val selected = selectedItem as? ProfilerThread
            if (selected != viewModel.selectedThread.value) {
                viewModel.selectedThread.set(selected)
            }
        }
    }

    private val toolbar: ActionToolbar = createToolbar()

    private val frameLabel = SimpleColoredComponent().apply {
        font = JBUI.Fonts.label().deriveFont(JBUI.scale(12f))
    }

    private val cpuLabel = SimpleColoredComponent().apply {
        font = JBUI.Fonts.label().deriveFont(JBUI.scale(12f))
    }

    private val splitButton = UnityProfilerModeButton(viewModel, lifetime)

    private val statusLabel = JBLabel(UnityUIBundle.message("unity.profiler.integration.data.up.to.date")).apply {
        foreground = UnityProfilerStyle.gridLabelForeground
    }

    private val headerPanel = JPanel(FlowLayout(FlowLayout.LEFT, JBUI.scale(8), JBUI.scale(5))).apply {
        isOpaque = false
        add(splitButton)
        add(statusLabel)
    }

    private val rootComponent = JBUI.Panels.simplePanel()
        .addToTop(headerPanel)
        .addToCenter(chartComponent)
        .addToBottom(JBUI.Panels.simplePanel()
            .addToLeft(JPanel().apply {
                layout = FlowLayout(FlowLayout.LEFT, JBUI.scale(5), 0)
                isOpaque = false
                add(threadSelector)
                add(toolbar.component)
                add(frameLabel)
                add(cpuLabel)
            })
            .apply {
                border = JBUI.Borders.empty(5, 0)
            }
        )

    // Coroutine scope for debouncing chart updates
    private val coroutineScope = CoroutineScope(SupervisorJob()).also {
        lifetime.onTermination { it.cancel() }
    }

    // Debounce jobs to prevent excessive repaints
    private var selectedFrameUpdateJob: Job? = null
    private var frameDurationsUpdateJob: Job? = null

    init {
        setupBindings()
    }

    /**
     * Sets up reactive bindings between ViewModel properties and UI components.
     * Uses debouncing for chart updates and manages thread selector state.
     */
    private fun setupBindings() {
        // Verify lifetimes are active before intersecting to prevent dead lifetime
        require(viewModel.lifetime.isAlive) { "ViewModel lifetime is not active" }
        require(lifetime.isAlive) { "Chart lifetime is not active" }

        val chartLifetime = viewModel.lifetime.intersect(lifetime)

        setupFrameSelectionBinding(chartLifetime)
        setupFrameDurationsBinding(chartLifetime)
        setupThreadSelectionBinding(chartLifetime)
        setupThreadNamesBinding(chartLifetime)

        // Initialize thread selector with current values
        updateThreadSelectorItems()
    }

    private fun setupFrameSelectionBinding(chartLifetime: Lifetime) {
        viewModel.selectedFrameIndex.advise(chartLifetime) { index ->
            // Debounce chart updates to prevent excessive repaints (16ms = ~60fps)
            selectedFrameUpdateJob?.cancel()
            selectedFrameUpdateJob = coroutineScope.launch(Dispatchers.EDT) {
                delay(16)
                chart.update()
                updateFooter(index)
            }
        }
    }

    private fun setupFrameDurationsBinding(chartLifetime: Lifetime) {
        viewModel.frameDurationsUpdated.advise(chartLifetime) {
            // Debounce chart updates to prevent excessive repaints (16ms = ~60fps)
            frameDurationsUpdateJob?.cancel()
            frameDurationsUpdateJob = coroutineScope.launch(Dispatchers.EDT) {
                delay(16)
                val count = viewModel.frameDurations.value.size
                chart.ranges.yMax = viewModel.chartYMax.value
                chart.ranges.xMin = 0
                chart.ranges.xMax = if (count > 1) count - 1 else 0
                chart.grid.xOrigin = count
                chart.getDataset().values = viewModel.frameDurations.value
                chart.update()
                updateFooter(viewModel.selectedFrameIndex.value)
            }
        }
    }

    private fun setupThreadSelectionBinding(chartLifetime: Lifetime) {
        viewModel.selectedThread.advise(chartLifetime) { thread ->
            if (threadSelector.selectedItem != thread) {
                threadSelector.selectedItem = thread
            }
            threadSelector.prototypeDisplayValue = thread ?: ProfilerThread(-1, "")
            threadSelector.revalidate()
        }
    }

    private fun setupThreadNamesBinding(chartLifetime: Lifetime) {
        viewModel.threadNamesUpdated.advise(chartLifetime) {
            updateThreadSelectorItems()
        }
    }

    /**
     * Reusable action for frame navigation (previous/next).
     * Reduces code duplication while maintaining clear separation of concerns.
     */
    private class FrameNavigationAction(
        text: String,
        icon: Icon,
        private val viewModel: UnityProfilerChartViewModel,
        private val direction: Direction
    ) : DumbAwareAction(text, null, icon) {

        enum class Direction { PREVIOUS, NEXT }

        override fun actionPerformed(e: AnActionEvent) {
            when (direction) {
                Direction.PREVIOUS -> viewModel.selectPreviousFrame()
                Direction.NEXT -> viewModel.selectNextFrame()
            }
        }

        override fun update(e: AnActionEvent) {
            val current = viewModel.selectedFrameIndex.value
            e.presentation.isEnabled = when (direction) {
                Direction.PREVIOUS -> current != null && current > viewModel.startIndex.value
                Direction.NEXT -> current != null && current < viewModel.lastIndex.value
            }
        }

        override fun getActionUpdateThread() = ActionUpdateThread.EDT
    }

    private fun createToolbar(): ActionToolbar {
        val actionGroup = DefaultActionGroup().apply {
            add(FrameNavigationAction(
                UnityUIBundle.message("unity.profiler.toolwindow.previous.frame"),
                AllIcons.Actions.Play_back,
                viewModel,
                FrameNavigationAction.Direction.PREVIOUS
            ))
            add(FrameNavigationAction(
                UnityUIBundle.message("unity.profiler.toolwindow.next.frame"),
                AllIcons.Actions.Play_forward,
                viewModel,
                FrameNavigationAction.Direction.NEXT
            ))
        }
        val toolbar = ActionManager.getInstance().createActionToolbar("UnityProfilerChart", actionGroup, true)
        toolbar.targetComponent = chart.component
        return toolbar
    }

    private fun updateFooter(index: Int?) {
        if (viewModel.frameDurations.value.isEmpty()) {
            frameLabel.clear()
            cpuLabel.clear()
            return
        }

        val selectedIndex = index ?: (viewModel.startIndex.value + viewModel.frameDurations.value.lastIndex)
        val currentMs = viewModel.getFrameDuration(selectedIndex) ?: 0.0
        val totalCount = viewModel.lastIndex.value

        val msText = "%.2f ms".format(currentMs)
        val frameValue = "${selectedIndex + 1}/${totalCount + 1}"

        val labelAttr = SimpleTextAttributes(SimpleTextAttributes.STYLE_BOLD, JBColor.foreground())
        val valueAttr = SimpleTextAttributes(SimpleTextAttributes.STYLE_PLAIN, UnityProfilerStyle.gridLabelForeground)

        updateLabel(frameLabel, "Frame: ", frameValue, labelAttr, valueAttr)
        updateLabel(cpuLabel, "CPU: ", msText, labelAttr, valueAttr)

        toolbar.updateActionsImmediately()
    }

    private fun updateLabel(
        label: SimpleColoredComponent,
        text: String,
        value: String,
        labelAttr: SimpleTextAttributes,
        valueAttr: SimpleTextAttributes
    ) {
        label.clear()
        label.append(text, labelAttr)
        label.append(value, valueAttr)
    }

    private fun updateThreadSelectorItems() {
        threadSelector.model = CollectionComboBoxModel(viewModel.threadNames.value, viewModel.selectedThread.value)
    }

    val component: JComponent get() = rootComponent
}

