package com.jetbrains.rider.plugins.unity.profiler.lineMarkers

import com.intellij.openapi.editor.colors.TextAttributesKey
import com.intellij.openapi.editor.markup.RangeHighlighter
import com.intellij.openapi.fileEditor.FileEditorManager
import com.intellij.openapi.fileEditor.TextEditor
import com.intellij.openapi.project.Project
import com.jetbrains.rd.ide.model.HighlighterModel
import com.jetbrains.rd.util.lifetime.Lifetime
import com.jetbrains.rdclient.daemon.IProtocolHighlighterModelHandler
import com.jetbrains.rdclient.daemon.util.HighlighterModelAgnosticComparator
import com.jetbrains.rider.plugins.unity.model.frontendBackend.ProfilerHighlighterModel
import com.jetbrains.rider.plugins.unity.profiler.viewModels.UnityProfilerLineMarkerViewModel

class UnityProfilerLineMarkerModelHandler(
    val markerViewModel: UnityProfilerLineMarkerViewModel,
    val project: Project,
    val lifetime: Lifetime
) : IProtocolHighlighterModelHandler {

    companion object {
        private val FakeAttributesKey = TextAttributesKey.createTextAttributesKey("UnityProfilerLineMarkerModelHandler.FakeAttributesKey")
    }
    
    init {
        // Listen for display settings changes and update all renderers
        markerViewModel.gutterMarksRenderSettings.advise(lifetime) { settings ->
            settings.let { displaySettings ->
                // Update all editors' renderers with new display settings
                // This will trigger recalculation of gutter width
                FileEditorManager.getInstance(project).allEditors
                    .filterIsInstance<TextEditor>()
                    .forEach { textEditor ->
                        UnityProfilerActiveLineMarkerRenderer.updateRenderers(textEditor.editor, displaySettings)
                    }
            }
        }
    }

    override fun accept(model: HighlighterModel): Boolean {
        return model is ProfilerHighlighterModel
    }

    override fun initialize(model: HighlighterModel, highlighter: RangeHighlighter) {
        model as ProfilerHighlighterModel

        highlighter.lineMarkerRenderer = UnityProfilerActiveLineMarkerRenderer(model.sampleInformation, markerViewModel, project, lifetime)
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
