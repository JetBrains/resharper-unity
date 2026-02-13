package com.jetbrains.rider.plugins.unity.profiler.lineMarkers

import com.intellij.openapi.Disposable
import com.intellij.openapi.application.EDT
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
import com.intellij.openapi.util.Disposer
import com.intellij.platform.util.coroutines.childScope
import com.intellij.ui.ExperimentalUI
import com.intellij.ui.JBColor
import com.intellij.ui.awt.RelativePoint
import com.intellij.ui.scale.JBUIScale
import com.intellij.util.ui.JBUI
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rd.util.reactive.valueOrDefault
import com.jetbrains.rider.plugins.unity.UnityPluginScopeService
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ModelUnityProfilerSampleInfo
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ParentCalls
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ProfilerGutterMarkRenderSettings
import com.jetbrains.rider.plugins.unity.profiler.UnityProfilerStyle
import com.jetbrains.rider.plugins.unity.profiler.utils.UnityProfilerFormatUtils
import com.jetbrains.rider.plugins.unity.profiler.viewModels.UnityProfilerLineMarkerViewModel
import kotlinx.coroutines.*
import java.awt.*
import java.awt.event.MouseEvent
import java.util.*
import kotlin.math.max
import kotlin.math.roundToInt

class UnityProfilerActiveLineMarkerRenderer(
    val sampleInfo: ModelUnityProfilerSampleInfo,
    val markerViewModel: UnityProfilerLineMarkerViewModel,
    val project: Project,
    val lifetime: Lifetime,
) : LineMarkerRendererEx, ActiveGutterRenderer {

    private val labelText: String = UnityProfilerFormatUtils.formatFixedWidthDuration(sampleInfo.milliseconds)

    @Volatile
    private var registered = false
    private val registrationLock = Any()

    override fun getPosition(): LineMarkerRendererEx.Position = LineMarkerRendererEx.Position.LEFT

    override fun paint(editor: Editor, g: Graphics, r: Rectangle) {
        // Register on first paint with double-checked locking to prevent race condition
        // Registration failures are retried on subsequent paint calls
        if (!registered) {
            logger.runAndLogException {
                synchronized(registrationLock) {
                    if (!registered) {
                        registerRenderer(editor, this)
                        val displaySettings = markerViewModel.gutterMarksRenderSettings.valueOrDefault(ProfilerGutterMarkRenderSettings.Default)
                        updateRenderers(editor, displaySettings)
                        registered = true
                    }
                }
            }
            // If registration failed, skip painting this frame and retry on next paint
            if (!registered) return
        }

        // Paint operations - errors are logged but don't prevent future paint attempts
        logger.runAndLogException {
            val isEnabled = markerViewModel.isGutterMarksEnabled.valueOrDefault(false)
            if (!isEnabled) return@runAndLogException

            val displaySettings = markerViewModel.gutterMarksRenderSettings.valueOrDefault(ProfilerGutterMarkRenderSettings.Default)
            val (backgroundColor, borderColor) = Utils.computeMarkerColors(sampleInfo.framePercentage)
            val g2d = g as Graphics2D

            when (displaySettings) {
                ProfilerGutterMarkRenderSettings.Default -> {
                    val font = getCurrentFont(editor)
                    g2d.font = font
                    val fm = editor.component.getFontMetrics(font)
                    paintStandard(editor, g2d, r, backgroundColor, borderColor, fm)
                }

                ProfilerGutterMarkRenderSettings.Minimized -> {
                    paintMinimized(editor, g2d, r, backgroundColor)
                }
            }
        }
    }

    private fun paintStandard(
        editor: Editor,
        g2d: Graphics2D,
        r: Rectangle,
        backgroundColor: Color,
        borderColor: Color,
        fm: FontMetrics,
    ) {
        val hGap = Utils.hGap(editor)
        val rectWidth = fm.stringWidth(labelText) + 2 * hGap
        val geom = calculateGeometry(editor, r, rectWidth)

        // Background
        g2d.color = backgroundColor
        g2d.fillRoundRect(geom.x, geom.y, geom.width, geom.height, geom.arc, geom.arc)

        // Border
        g2d.color = borderColor
        g2d.drawRoundRect(geom.x, geom.y, geom.width - 1, geom.height - 1, geom.arc, geom.arc)

        // Text
        g2d.color = editor.colorsScheme.defaultForeground
        val baseline = geom.y + geom.height / 2 + fm.ascent * 2 / 5
        g2d.drawString(labelText, geom.x + hGap, baseline)
    }

    private fun paintMinimized(
        editor: Editor,
        g2d: Graphics2D,
        r: Rectangle,
        backgroundColor: Color,
    ) {
        val rectWidth = Utils.scale(editor, 6)
        val geom = calculateGeometry(editor, r, rectWidth)

        g2d.color = backgroundColor
        g2d.fillRoundRect(geom.x, geom.y, geom.width, geom.height, geom.arc, geom.arc)
    }

    private data class MarkerGeometry(val x: Int, val y: Int, val width: Int, val height: Int, val arc: Int)

    private fun calculateGeometry(editor: Editor, r: Rectangle, rectWidth: Int): MarkerGeometry {
        val vGap = Utils.vGap(editor)
        val height = r.height - 2 * vGap
        val x = r.x + r.width - rectWidth - Utils.hRightMargin(editor)
        val y = r.y + vGap
        val round = if (ExperimentalUI.isNewUI()) Utils.scale(editor, 4) else Utils.scale(editor, 2)
        return MarkerGeometry(x, y, rectWidth, height, 2 * round)
    }

    override fun getTooltipText(): String? = null

    override fun canDoAction(e: MouseEvent): Boolean = true

    override fun doAction(editor: Editor, e: MouseEvent) {
        logger.runAndLogException {
            val gutter = e.component as? EditorGutterComponentEx ?: return
            e.consume()

            val popup = ProfilerLineMarkerPopupFactory.create(project, sampleInfo, markerViewModel, gutter, e)
            val point = Point(e.x, e.y + JBUI.scale(3))
            popup.show(RelativePoint(gutter, point))
        }
    }

    private fun getCurrentFont(editor: Editor): Font {
        val editorFont = editor.colorsScheme.getFont(EditorFontType.PLAIN)
        val editorFontSize = editorFont.size2D
        return editorFont.deriveFont(max(1f, editorFontSize - 1f))
    }

    fun calculateReservationWidth(editor: Editor, displaySettings: ProfilerGutterMarkRenderSettings): Int {
        logger.runAndLogException {
            when (displaySettings) {
                ProfilerGutterMarkRenderSettings.Default -> {
                    if (labelText.isBlank()) return 0
                    editor as EditorImpl
                    val currentFont = getCurrentFont(editor)
                    val unscaledFont = currentFont.deriveFont(currentFont.size2D / JBUIScale.scale(editor.getScale()))
                    val textWidth = editor.component.getFontMetrics(unscaledFont).stringWidth(labelText)
                    return textWidth + 2 * H_GAP_ABSOLUTE + H_LEFT_MARGIN_ABSOLUTE + H_RIGHT_MARGIN_ABSOLUTE
                }

                ProfilerGutterMarkRenderSettings.Minimized -> {
                    return 2 * H_GAP_ABSOLUTE + H_LEFT_MARGIN_ABSOLUTE + H_RIGHT_MARGIN_ABSOLUTE
                }

                else -> return 0
            }
        }
        return 0
    }

    companion object {
        private const val H_GAP_ABSOLUTE = 4
        private const val H_LEFT_MARGIN_ABSOLUTE = 4
        private const val H_RIGHT_MARGIN_ABSOLUTE = 6
        private const val V_GAP_ABSOLUTE = 1

        private val logger = Logger.getInstance(UnityProfilerActiveLineMarkerRenderer::class.java)

        /**
         * Utilities for formatting, scaling, and color computation.
         */
        object Utils {


            fun scale(editor: Editor, x: Int): Int = JBUI.scale(EditorUIUtil.scaleWidth(x, editor as EditorImpl))
            fun hGap(editor: Editor): Int = scale(editor, H_GAP_ABSOLUTE)
            fun hRightMargin(editor: Editor): Int = scale(editor, H_RIGHT_MARGIN_ABSOLUTE)
            fun vGap(editor: Editor): Int = scale(editor, V_GAP_ABSOLUTE)

            private fun lerp(a: Int, b: Int, t: Double): Int = (a + (b - a) * t).roundToInt().coerceIn(0, 255)

            @Suppress("UseJBColor")
            private fun lerpColor(c1: Color, c2: Color, t: Double): Color = Color(
                lerp(c1.red, c2.red, t),
                lerp(c1.green, c2.green, t),
                lerp(c1.blue, c2.blue, t),
                lerp(c1.alpha, c2.alpha, t),
            )

            fun lerpJBColor(c1: JBColor, c2: JBColor, t: Double): JBColor = JBColor(
                lerpColor(c1, c2, t),
                lerpColor(c1.darkVariant, c2.darkVariant, t)
            )

            fun computeMarkerColors(framePercentage: Double): Pair<Color, Color> {
                val t = framePercentage.coerceIn(0.0, 1.0)
                val background = lerpJBColor(UnityProfilerStyle.markerColdBackground, UnityProfilerStyle.markerHotBackground, t)
                val border = lerpJBColor(UnityProfilerStyle.markerColdBorder, UnityProfilerStyle.markerHotBorder, t)
                return Pair(background, border)
            }
        }

        // One reservation per editor
        private val aggregators = Collections.synchronizedMap(WeakHashMap<Editor, GutterReservationAggregator>())

        fun registerRenderer(
            editor: Editor,
            renderer: UnityProfilerActiveLineMarkerRenderer,
        ) {
            logger.runAndLogException {
                val gutter = editor.gutter as? EditorGutterComponentEx ?: return
                val parentDisposable: Disposable? = when (editor) {
                    is EditorImpl -> editor.disposable
                    is Disposable -> editor
                    else -> null
                }

                if (parentDisposable == null) {
                    logger.error("Cannot get parent disposable for editor ${editor.javaClass.name}")
                    return
                }

                val aggregator = aggregators.computeIfAbsent(editor) {
                    val created = GutterReservationAggregator(gutter, renderer.project)
                    Disposer.register(parentDisposable, created)
                    Disposer.register(parentDisposable) { aggregators.remove(editor) }
                    created
                }

                aggregator.addRenderer(renderer)
            }
        }

        fun unregisterRenderer(
            editor: Editor,
            renderer: UnityProfilerActiveLineMarkerRenderer,
        ) {
            logger.runAndLogException {
                aggregators[editor]?.removeRenderer(renderer)
            }
        }

        fun updateRenderers(editor: Editor, displaySettings: ProfilerGutterMarkRenderSettings) {
            logger.runAndLogException {
                aggregators[editor]?.updateDisplaySettings(displaySettings)
            }
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
        project: Project,
    ) : Disposable {
        private var holder = GutterSizeReservation(0)
        private val renderers = Collections.newSetFromMap(WeakHashMap<UnityProfilerActiveLineMarkerRenderer, Boolean>())
        private var currentReserved = -1
        private var currentDisplaySettings: ProfilerGutterMarkRenderSettings? = null
        private val coroutineScope = UnityPluginScopeService.getScope(project).childScope("UnityProfilerActiveLineMarkerRenderer")

        @Volatile
        private var isDisposed = false

        fun addRenderer(renderer: UnityProfilerActiveLineMarkerRenderer) {
            renderers.add(renderer)
            recalculateAndUpdate()
        }

        fun removeRenderer(renderer: UnityProfilerActiveLineMarkerRenderer) {
            renderers.remove(renderer)
            recalculateAndUpdate()
        }

        fun updateDisplaySettings(displaySettings: ProfilerGutterMarkRenderSettings) {
            if (currentDisplaySettings != displaySettings) {
                currentDisplaySettings = displaySettings
                recalculateAndUpdate()
            }
        }

        private fun recalculateAndUpdate() {
            logger.runAndLogException {
                if (isDisposed) return

                val displaySettings = currentDisplaySettings ?: return
                val editor = gutter.editor as? EditorImpl ?: return

                val maxWidth = renderers.maxOfOrNull { renderer ->
                    renderer.calculateReservationWidth(editor, displaySettings)
                } ?: 0

                if (maxWidth == currentReserved) return

                // Schedule the update asynchronously to avoid updating gutter size during paint cycle
                coroutineScope.launch(Dispatchers.EDT) {
                    if (isDisposed) return@launch
                    val oldHolder = holder
                    currentReserved = maxWidth
                    holder = GutterSizeReservation(maxWidth)
                    gutter.reserveLeftFreePaintersAreaWidth(holder, maxWidth)
                    // Dispose old holder AFTER registering the new one to ensure proper shrinking
                    Disposer.dispose(oldHolder)
                    gutter.revalidateMarkup()
                }
            }
        }

        override fun dispose() {
            isDisposed = true
            renderers.clear()
            Disposer.dispose(holder)
            coroutineScope.cancel()
        }
    }
}
