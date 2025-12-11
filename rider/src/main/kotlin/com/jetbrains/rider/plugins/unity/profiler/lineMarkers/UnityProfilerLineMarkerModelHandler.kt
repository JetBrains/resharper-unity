package com.jetbrains.rider.plugins.unity.profiler.lineMarkers

import com.intellij.openapi.editor.colors.TextAttributesKey
import com.intellij.openapi.editor.markup.RangeHighlighter
import com.intellij.openapi.project.Project
import com.jetbrains.rd.ide.model.HighlighterModel
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rdclient.daemon.IProtocolHighlighterModelHandler
import com.jetbrains.rdclient.daemon.util.HighlighterModelAgnosticComparator
import com.jetbrains.rider.plugins.unity.model.frontendBackend.FrontendBackendProfilerModel
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ProfilerHighlighterModel

class UnityProfilerLineMarkerModelHandler(
    val profilerModel: FrontendBackendProfilerModel,
    val project: Project,
    val lifetime: Lifetime
) : IProtocolHighlighterModelHandler {

    companion object {
        private val FakeAttributesKey = TextAttributesKey.createTextAttributesKey("UnityProfilerLineMarkerModelHandler.FakeAttributesKey")
    }

    override fun accept(model: HighlighterModel): Boolean {
        return model is ProfilerHighlighterModel
    }

    override fun initialize(model: HighlighterModel, highlighter: RangeHighlighter) {
        model as ProfilerHighlighterModel

        highlighter.lineMarkerRenderer = UnityProfilerActiveLineMarkerRenderer(model.sampleInformation, profilerModel, project, lifetime)
        highlighter.isGreedyToLeft = false
        highlighter.isGreedyToRight = false
        highlighter.setTextAttributesKey(FakeAttributesKey)
    }

    override fun compare(model: HighlighterModel, highlighter: RangeHighlighter): Boolean {
        return HighlighterModelAgnosticComparator.compare(model, highlighter)
    }

    override fun move(startOffset: Int, endOffset: Int, model: HighlighterModel): HighlighterModel {
        return (model as ProfilerHighlighterModel).run {
            ProfilerHighlighterModel(
                model.sampleInformation,
                layer = layer,
                isExactRange = true,
                documentVersion = documentVersion,
                isGreedyToLeft = false,
                isGreedyToRight = false,
                isThinErrorStripeMark = false,
                textToHighlight = textToHighlight,
                textAttributesKey = textAttributesKey,
                id = id,
                properties = properties,
                start = startOffset,
                end = endOffset
            )
        }
    }
}
