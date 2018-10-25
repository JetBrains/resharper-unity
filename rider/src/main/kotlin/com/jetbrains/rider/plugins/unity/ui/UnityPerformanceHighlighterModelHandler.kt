package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.editor.colors.EditorColorsManager
import com.intellij.openapi.editor.ex.RangeHighlighterEx
import com.intellij.openapi.editor.markup.LineMarkerRenderer
import com.intellij.openapi.editor.markup.LineMarkerRendererEx
import com.intellij.openapi.editor.markup.RangeHighlighter
import com.jetbrains.rdclient.daemon.HighlighterRegistrationHost
import com.jetbrains.rdclient.daemon.IProtocolHighlighterModelHandler
import com.jetbrains.rdclient.daemon.util.HighlighterModelAgnosticComparator
import com.jetbrains.rider.daemon.highlighters.lineMarkers.RiderSimpleLineMarkerRenderer
import com.jetbrains.rider.model.HighlighterModel
import com.jetbrains.rider.model.UnityPerformanceHiglighterModel
import java.awt.Color

class UnityPerformanceHighlighterModelHandler(private val registrationHost: HighlighterRegistrationHost, private val editorColorsManager: EditorColorsManager) : IProtocolHighlighterModelHandler {
    override fun compare(model: HighlighterModel, highlighter: RangeHighlighter): Boolean {
        model as UnityPerformanceHiglighterModel
        return HighlighterModelAgnosticComparator.compare(model, highlighter)
    }

    override fun initialize(model: HighlighterModel, highlighter: RangeHighlighter) {
        val textAttribute= registrationHost.getTextAttributesKey(model.attributeId)
        val attributes = editorColorsManager.globalScheme.getAttributes(textAttribute)
        highlighter as RangeHighlighterEx
        highlighter.isAfterEndOfLine = true
        highlighter.lineMarkerRenderer = UnityPerformanceLineMarkerRenderer(attributes.backgroundColor, 3, LineMarkerRendererEx.Position.LEFT)
    }

    override fun move(startOffset: Int, endOffset: Int, model: HighlighterModel): HighlighterModel? {
        return (model as UnityPerformanceHiglighterModel).run {
            UnityPerformanceHiglighterModel(layer, isExactRange,
                documentVersion, textToHighlight, id, attributeId,
                startOffset, endOffset)
        }
    }

    override fun accept(model: HighlighterModel): Boolean {
        return model is UnityPerformanceHiglighterModel
    }

}