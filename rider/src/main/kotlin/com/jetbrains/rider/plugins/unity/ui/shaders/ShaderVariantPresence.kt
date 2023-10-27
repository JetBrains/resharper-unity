package com.jetbrains.rider.plugins.unity.ui.shaders

import com.intellij.codeInsight.daemon.impl.HighlightInfo
import com.intellij.openapi.Disposable
import com.intellij.openapi.components.service
import com.intellij.openapi.editor.Editor
import com.intellij.openapi.editor.ex.MarkupModelEx
import com.intellij.openapi.editor.ex.RangeHighlighterEx
import com.intellij.openapi.editor.ex.util.EditorUtil
import com.intellij.openapi.editor.impl.DocumentMarkupModel
import com.intellij.openapi.editor.impl.event.MarkupModelListener
import com.intellij.openapi.project.Project
import com.intellij.openapi.rd.createLifetime
import com.intellij.openapi.util.Key
import com.intellij.util.concurrency.annotations.RequiresEdt
import com.jetbrains.rd.util.collections.CountingSet
import com.jetbrains.rd.util.reactive.AddRemove
import com.jetbrains.rdclient.daemon.util.text

class ShaderVariantPresence(project: Project?, markupModel: MarkupModelEx) : Disposable {
    companion object {
        private const val SUPPRESSED_KEYWORD_EXTERNAL_NAME = "ReSharper.ReSharper.ShaderLab_SUPPRESSED_SHADER_KEYWORD"
        private const val ACTIVE_KEYWORD_EXTERNAL_NAME = "ReSharper.ReSharper.ShaderLab_ACTIVE_SHADER_KEYWORD"

        private val SHADER_VARIANT_PRESENCE_KEY = Key.create<ShaderVariantPresence>("rider.ShaderLab.SHADER_VARIANT_PRESENCE")

        fun ensure(editor: Editor) = editor.getUserData(SHADER_VARIANT_PRESENCE_KEY) ?: run {
            val project = editor.project
            val markupModel = DocumentMarkupModel.forDocument(editor.document, project, true) as MarkupModelEx
            ShaderVariantPresence(project, markupModel).also {
                EditorUtil.disposeWithEditor(editor, it)
                editor.putUserData(SHADER_VARIANT_PRESENCE_KEY, it)
            }
        }

        fun get(editor: Editor): ShaderVariantPresence? = editor.getUserData(SHADER_VARIANT_PRESENCE_KEY)
    }

    private val myListeners = mutableListOf<ChangeListener>()
    private val myEnabledKeywords: Set<String>

    val activeKeywords = CountingSet<String>()
    val suppressedKeywords = CountingSet<String>()

    init {
        markupModel.addMarkupModelListener(this, object : MarkupModelListener {
            override fun afterAdded(highlighter: RangeHighlighterEx) {
                val info = HighlightInfo.fromRangeHighlighter(highlighter) ?: return
                when (info.type.attributesKey.externalName) {
                    SUPPRESSED_KEYWORD_EXTERNAL_NAME -> addSuppressedKeywordHighlighter(highlighter)
                    ACTIVE_KEYWORD_EXTERNAL_NAME -> addActiveKeywordHighlighter(highlighter)
                }
            }

            override fun afterRemoved(highlighter: RangeHighlighterEx) {
                val info = HighlightInfo.fromRangeHighlighter(highlighter) ?: return
                when (info.type.attributesKey.externalName) {
                    SUPPRESSED_KEYWORD_EXTERNAL_NAME -> removeSuppressedKeywordHighlighter(highlighter)
                    ACTIVE_KEYWORD_EXTERNAL_NAME -> removeActiveKeywordHighlighter(highlighter)
                }
            }
        })
        myEnabledKeywords = project?.service<ShaderVariantsHost>()?.enabledShaderKeywords?.also {
            it.advise(this.createLifetime()) { evt ->
                when (evt.kind) {
                    AddRemove.Add -> onKeywordEnabled(evt.value)
                    AddRemove.Remove -> onKeywordDisabled(evt.value)
                }
            }
        } ?: emptySet()
    }

    fun getActiveCountForEnabledOnly() = activeKeywords.keys.count { myEnabledKeywords.contains(it) }

    private fun onKeywordEnabled(keyword: String) {
        if (activeKeywords.contains(keyword))
            fireChange()
    }

    private fun onKeywordDisabled(keyword: String) {
        if (activeKeywords.contains(keyword))
            fireChange()
    }

    private fun addSuppressedKeywordHighlighter(highlighter: RangeHighlighterEx) {
        if (suppressedKeywords.add(highlighter.text, 1) == 1)
            fireChange()
    }

    private fun removeSuppressedKeywordHighlighter(highlighter: RangeHighlighterEx) {
        if (suppressedKeywords.add(highlighter.text, -1) == 0)
            fireChange()
    }

    private fun addActiveKeywordHighlighter(highlighter: RangeHighlighterEx) {
        if (activeKeywords.add(highlighter.text, 1) == 1)
            fireChange()
    }

    private fun removeActiveKeywordHighlighter(highlighter: RangeHighlighterEx) {
        if (activeKeywords.add(highlighter.text, -1) == 0)
            fireChange()
    }

    @RequiresEdt
    private fun fireChange() {
        for (listener in myListeners)
            listener.shaderVariantKeywordsChanged()
    }

    @RequiresEdt
    fun addChangeListener(listener: ChangeListener) {
        myListeners.add(listener)
    }

    @RequiresEdt
    fun removeChangeListener(listener: ChangeListener) {
        myListeners.remove(listener)
    }

    override fun dispose() = Unit

    fun interface ChangeListener {
        fun shaderVariantKeywordsChanged()
    }
}