package com.jetbrains.rider.plugins.unity.profiler.lineMarkers

import com.intellij.openapi.Disposable
import com.intellij.openapi.application.EDT
import com.intellij.openapi.components.service
import com.intellij.openapi.diagnostic.Logger
import com.intellij.openapi.diagnostic.runAndLogException
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.colors.EditorFontType
import com.intellij.openapi.editor.ex.EditorGutterComponentEx
import com.intellij.openapi.editor.ex.util.EditorUIUtil
import com.intellij.openapi.editor.impl.EditorImpl
import com.intellij.openapi.editor.markup.ActiveGutterRenderer
import com.intellij.openapi.editor.markup.LineMarkerRendererEx
import com.intellij.openapi.project.Project
import com.intellij.openapi.ui.popup.JBPopupFactory
import com.intellij.openapi.ui.popup.util.RoundedCellRenderer
import com.intellij.openapi.util.Disposer
import com.intellij.openapi.util.NlsSafe
import com.intellij.ui.ColorUtil
import com.intellij.ui.ExperimentalUI
import com.intellij.ui.JBColor
import com.intellij.ui.SimpleListCellRenderer
import com.intellij.ui.awt.RelativePoint
import com.intellij.ui.components.JBLabel
import com.intellij.ui.scale.JBUIScale
import com.intellij.util.ui.JBUI
import com.intellij.util.ui.UIUtil
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rider.plugins.unity.UnityProjectLifetimeService
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendProfilerModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ModelUnityProfilerSampleInfo
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ParentCalls
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ProfilerGutterMarkRenderSettings
import com.jetbrains.rider.plugins.unity.profiler.UnityProfilerUsagesDaemon
import kotlinx.coroutines.CoroutineStart
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.launch
import java.awt.BorderLayout
import java.awt.Color
import java.awt.Component
import java.awt.Font
import java.awt.FontMetrics
import java.awt.Graphics
import java.awt.Graphics2D
import java.awt.Point
import java.awt.Rectangle
import java.awt.event.MouseEvent
import java.util.Collections
import java.util.Locale
import java.util.WeakHashMap
import javax.swing.JList
import javax.swing.JPanel
import javax.swing.ListCellRenderer
import kotlin.math.max
import kotlin.math.roundToInt

class UnityProfilerActiveLineMarkerRenderer(
    val sampleInfo: ModelUnityProfilerSampleInfo,
    val profilerModel: FrontendBackendProfilerModel,
    val project: Project,
    val lifetime: Lifetime,
) : LineMarkerRendererEx, ActiveGutterRenderer {

    private val labelText: String = formatFixedWidthDuration(sampleInfo.milliseconds)

    override fun getPosition(): LineMarkerRendererEx.Position = LineMarkerRendererEx.Position.LEFT

    override fun paint(editor: Editor, g: Graphics, r: Rectangle) {
        logger.runAndLogException {
            val displaySettings = sampleInfo.renderSettings
            if (displaySettings == ProfilerGutterMarkRenderSettings.Hidden) return

            val baseColor = computeLineColorByPercentage(sampleInfo.framePercentage)
            val g2d = g as Graphics2D

            when (displaySettings) {
                ProfilerGutterMarkRenderSettings.Default -> {
                    val font = getCurrentFont(editor)
                    g2d.font = font
                    val fm = editor.component.getFontMetrics(font)
                    getOrCreateAggregator(editor, displaySettings)?.update(
                        this,
                        calculateReservationWidth(editor),
                        displaySettings
                    )
                    paintStandard(editor, g2d, r, baseColor, fm)
                }

                ProfilerGutterMarkRenderSettings.Minimized -> {
                    val emptyLength = 2 * H_GAP_ABSOLUTE + H_LEFT_MARGIN_ABSOLUTE + H_RIGHT_MARGIN_ABSOLUTE
                    getOrCreateAggregator(editor, displaySettings)?.update(this, emptyLength, displaySettings)
                    paintMinimized(editor, g2d, r, baseColor)
                }

                else -> {
                    logger.warn("Unknown display settings: $displaySettings")
                }
            }
        }
    }

    // Draws the full marker with text label
    private fun paintStandard(
        editor: Editor,
        g2d: Graphics2D,
        r: Rectangle,
        baseColor: Color,
        fm: FontMetrics,
    ) {
        // Compute geometry matching LineMarker: text inside a rounded rectangle aligned to the right edge of the marker area.
        val textWidth = fm.stringWidth(labelText)
        val roundRectHeight = r.height - 2 * vGap(editor)
        val roundRectWidth = textWidth + 2 * hGap(editor)
        val startX = r.x + r.width - roundRectWidth - hRightMargin(editor)
        val startY = r.y + vGap(editor)

        val round = if (ExperimentalUI.isNewUI()) scale(editor, 4) else scale(editor, 2)

        // Background
        g2d.color = baseColor
        g2d.fillRoundRect(startX, startY, roundRectWidth, roundRectHeight, 2 * round, 2 * round)

        // Text
        g2d.color = if (ColorUtil.isDark(baseColor)) JBColor.WHITE else JBColor.BLACK
        val baseline = startY + roundRectHeight / 2 + fm.ascent * 2 / 5
        g2d.drawString(labelText, startX + hGap(editor), baseline)
    }

    // Draws the compact pill without text
    private fun paintMinimized(
        editor: Editor,
        g2d: Graphics2D,
        r: Rectangle,
        baseColor: Color,
    ) {
        val roundRectHeight = r.height - 2 * vGap(editor)
        val roundRectWidth = 1 * hGap(editor)
        val startX = r.x + r.width - roundRectWidth - hRightMargin(editor)
        val startY = r.y + vGap(editor)

        val round = if (ExperimentalUI.isNewUI()) scale(editor, 4) else scale(editor, 2)

        g2d.color = baseColor
        g2d.fillRoundRect(startX, startY, roundRectWidth, roundRectHeight, 2 * round, 2 * round)
    }

    override fun getTooltipText(): String = sampleInfo.toolTip

    override fun canDoAction(e: MouseEvent): Boolean = true

    override fun doAction(editor: Editor, e: MouseEvent) {
        logger.runAndLogException {
            // Record usage for simple local statistics
            project.service<UnityProfilerUsagesDaemon>().showPopupAction()
            
            val parents = sampleInfo.parents?.takeIf { it.isNotEmpty() } ?: return

            val gutter = e.component as? EditorGutterComponentEx ?: return
            e.consume()

            // Show a chooser popup with custom rendered parent samples.
            val popup = JBPopupFactory.getInstance()
                .createPopupChooserBuilder(parents)
                .setAdText(sampleInfo.toolTip)
                .setRenderer(RoundedCellRenderer(ParentCallsRenderer()))
                // use qualifiedName for filtering and selection callback as requested
                .setNamerForFiltering { it.qualifiedName }
                .setItemChosenCallback { element ->
                    logger.runAndLogException {
                        if (element == null) return@setItemChosenCallback
                        UnityProjectLifetimeService.getScope(project).launch(Dispatchers.EDT, CoroutineStart.UNDISPATCHED) {
                            logger.runAndLogException {
                                if (element.realParentQualifiedName != null)
                                    profilerModel.navigateByQualifiedName.fire(element.realParentQualifiedName)
                            }
                        }
                    }
                }
                .createPopup()

            val point = Point(e.x, e.y + JBUI.scale(3))
            popup.show(RelativePoint(gutter, point))
        }
    }

    /**
     * Custom renderer for parents popup: shows qualified name and formatted timing on the right.
     */
    private class ParentCallsRenderer : ListCellRenderer<ParentCalls> {

        private val panel = JPanel(BorderLayout(JBUI.scale(4), 0)).apply {
            border = JBUI.Borders.empty(0, JBUI.scale(4))
        }

        private val mainRenderer =
            SimpleListCellRenderer.create<ParentCalls> { label, value, _ ->
                label.text = value.qualifiedName
            }

        override fun getListCellRendererComponent(
            list: JList<out ParentCalls>,
            value: ParentCalls,
            index: Int,
            isSelected: Boolean,
            cellHasFocus: Boolean
        ): Component {
            val left = mainRenderer.getListCellRendererComponent(list, value, index, isSelected, cellHasFocus)

            // Right side: duration and percentage, formatted like label used in gutter
            val right = JPanel(BorderLayout(JBUI.scale(2), 0)).apply {
                val text = formatLabel("", value.duration, value.framePercentage)
                // text comes as " 99.99ms (9.9%)" after trimming empty name
                add(JBLabel(text), BorderLayout.EAST)
            }

            panel.removeAll()
            panel.add(left, BorderLayout.WEST)
            panel.add(right, BorderLayout.EAST)

            UIUtil.setBackgroundRecursively(panel, left.background)
            UIUtil.setForegroundRecursively(panel, left.foreground)
            return panel
        }
    }

    private fun getCurrentFont(editor: Editor): Font {
        val editorFont = editor.colorsScheme.getFont(EditorFontType.PLAIN)
        val editorFontSize = editorFont.size2D
        return editorFont.deriveFont(max(1f, editorFontSize - 1f))
    }

    private fun calculateReservationWidth(editor: Editor): Int {
        logger.runAndLogException {
            if (labelText.isBlank()) return 0
            editor as EditorImpl
            val currentFont = getCurrentFont(editor)
            val unscaledFont = currentFont.deriveFont(currentFont.size2D / JBUIScale.scale(editor.getScale()))
            val textWidth = editor.component.getFontMetrics(unscaledFont).stringWidth(labelText)
            return textWidth + 2 * H_GAP_ABSOLUTE + H_LEFT_MARGIN_ABSOLUTE + H_RIGHT_MARGIN_ABSOLUTE
        }
        return 0
    }

    companion object {
        private const val H_GAP_ABSOLUTE = 6
        private const val H_LEFT_MARGIN_ABSOLUTE = 4
        private const val H_RIGHT_MARGIN_ABSOLUTE = 6
        private const val V_GAP_ABSOLUTE = 1

        private val logger = Logger.getInstance(UnityProfilerActiveLineMarkerRenderer::class.java)

        // One reservation per editor
        private val aggregators = Collections.synchronizedMap(WeakHashMap<Editor, GutterReservationAggregator>())

        fun editorRenderSettings(editor: Editor): ProfilerGutterMarkRenderSettings? {
            if (!aggregators.containsKey(editor))
                return null
            
            return aggregators[editor]?.getRenderSettings()
        }
        
        private fun getOrCreateAggregator(
            editor: Editor,
            gutterMarksDisplaySettings: ProfilerGutterMarkRenderSettings,
        ): GutterReservationAggregator? {
            return logger.runAndLogException {
                val gutter = editor.gutter as? EditorGutterComponentEx ?: return null
                // Find a reliable parent disposable tied to editor lifecycle
                val parentDisposable: Disposable? = when (editor) {
                    is EditorImpl -> editor.disposable
                    is Disposable -> editor
                    else -> null
                }

                if (parentDisposable == null) {
                    logger.error("Cannot get parent disposable for editor ${editor.javaClass.name}")
                    return null // do not create/capture aggregator without a parent
                }
                val cached = aggregators[editor]
                if (cached != null && !Disposer.isDisposed(cached)) return cached

                val created = GutterReservationAggregator(gutter, gutterMarksDisplaySettings)
                Disposer.register(parentDisposable, created)
                Disposer.register(parentDisposable) { aggregators.remove(editor) }
                aggregators[editor] = created
                created
            }
        }

        private fun lerp(a: Int, b: Int, t: Double): Int =
            (a + (b - a) * t).roundToInt().coerceIn(0, 255)

        @Suppress("UseJBColor")
        private fun lerpColor(c1: Color, c2: Color, t: Double): Color = Color(
            lerp(c1.red, c2.red, t),
            lerp(c1.green, c2.green, t),
            lerp(c1.blue, c2.blue, t),
            lerp(c1.alpha, c2.alpha, t),
        )

        internal fun lerpJBColor(c1: JBColor, c2: JBColor, t: Double): JBColor = JBColor(
            lerpColor(c1, c2, t),
            lerpColor(c1.darkVariant, c2.darkVariant, t)
        )

        /**
         * Formats profiler label exactly like the C# template:
         * "{0} {1:F2}ms ({2:P1})" where
         *  - {0} is the qualified name
         *  - {1:F2} is duration in milliseconds with 2 decimals
         *  - {2:P1} is frame fraction as percentage with 1 decimal
         */
        @NlsSafe
        internal fun formatLabel(name: String, durationMs: Double, frameFraction: Double): String {
            val percent = frameFraction * 100.0
            return String.format(Locale.US, "%s %.2fms (%.1f%%)", name, durationMs, percent)
        }

        private fun formatFixedWidthDuration(durationMs: Double): String {
            // Render time using a fixed-size label of exactly 7 characters so that
            // text width and reserved size stay consistent across values.
            // See rules in the comment below; enforce Locale.US for stable decimal separator.
            return when {
                durationMs < 100 -> {
                    // " 99.99ms" → 7
                    String.format(Locale.US, "%5.2f", durationMs).plus("ms").padStart(7, ' ')
                }

                durationMs < 1000 -> {
                    // " 999.9ms" → 7
                    String.format(Locale.US, "%5.1f", durationMs).plus("ms").padStart(7, ' ')
                }

                else -> {
                    val s = durationMs / 1000.0
                    when {
                        s < 10 -> String.format(Locale.US, "%6.2f", s).plus("s")   // "   9.99s"
                        s < 100 -> String.format(Locale.US, "%6.1f", s).plus("s")  // "  99.9s"
                        s < 1000 -> String.format(Locale.US, "%6.0f", s).plus("s") // "  999s"
                        else -> {
                            // fall back to minutes to avoid huge seconds; still 7 chars: number(6) + 'm'
                            val m = s / 60.0
                            val minutesPart = when {
                                m < 100 -> String.format(Locale.US, "%6.1f", m)
                                m < 1000 -> String.format(Locale.US, "%6.0f", m)
                                else -> "999999" // clamp visually
                            }
                            minutesPart.plus("m")
                        }
                    }
                }
            }
        }

        private fun scale(editor: Editor, x: Int): Int = JBUI.scale(EditorUIUtil.scaleWidth(x, editor as EditorImpl))

        private fun hGap(editor: Editor): Int = scale(editor, H_GAP_ABSOLUTE)

        private fun hRightMargin(editor: Editor): Int = scale(editor, H_RIGHT_MARGIN_ABSOLUTE)

        private fun vGap(editor: Editor): Int = scale(editor, V_GAP_ABSOLUTE)

        private fun computeLineColorByPercentage(framePercentage: Double): Color {
            val t = framePercentage.coerceIn(0.0, 1.0)
            val lowPercentageColor = Color(128, 255, 128, 200)
            val highPercentageColor = Color(255, 0, 0, 255)
            return lerpColor(lowPercentageColor, highPercentageColor, t)
        }

    }

    // Reserving left gutter width holder used by the aggregator
    private class GutterSizeReservation(var requestedWidth: Int) : Disposable {
        var isDisposed: Boolean = false

        override fun dispose() {
            isDisposed = true
        }
    }

    // Aggregates reservations per editor to avoid multiplying reserved space.
    private class GutterReservationAggregator(
        private val gutter: EditorGutterComponentEx,
        private var gutterMarksDisplaySettings: ProfilerGutterMarkRenderSettings,
    ) : Disposable {
        private var holder = GutterSizeReservation(0)
        private val widthsByRenderer = WeakHashMap<UnityProfilerActiveLineMarkerRenderer, Int>()
        private var currentReserved = -1

        @Volatile
        private var isDisposed = false
        fun getRenderSettings() : ProfilerGutterMarkRenderSettings = gutterMarksDisplaySettings

        fun update(
            renderer: UnityProfilerActiveLineMarkerRenderer,
            width: Int,
            displaySettings: ProfilerGutterMarkRenderSettings,
        ) {
            logger.runAndLogException {
                if (width <= 0) {
                    widthsByRenderer.remove(renderer)
                } else {
                    widthsByRenderer[renderer] = width
                }
                if (isDisposed) return
                val newMax = widthsByRenderer.values.maxOrNull() ?: 0
                if (newMax == currentReserved && displaySettings == gutterMarksDisplaySettings) return
                Disposer.dispose(holder)
                currentReserved = newMax
                gutterMarksDisplaySettings = displaySettings
                holder = GutterSizeReservation(newMax)
                gutter.reserveLeftFreePaintersAreaWidth(holder, newMax)
                gutter.revalidateMarkup()
            }
        }

        override fun dispose() {
            isDisposed = true
            widthsByRenderer.clear()
            Disposer.dispose(holder)
        }
    }
}
