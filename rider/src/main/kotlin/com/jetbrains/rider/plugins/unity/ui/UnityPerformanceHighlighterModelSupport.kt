package com.jetbrains.rider.plugins.unity.ui

import com.intellij.openapi.editor.Document
import com.intellij.openapi.editor.colors.EditorColorsManager
import com.jetbrains.rdclient.daemon.HighlighterRegistrationHost
import com.jetbrains.rdclient.daemon.IProtocolHighlighterModelHandler
import com.jetbrains.rdclient.daemon.IProtocolHighlighterModelSupport
import com.jetbrains.rider.model.RdMarkupModel
import com.jetbrains.rider.util.lifetime.Lifetime

class UnityPerformanceHighlighterModelSupport(private val registrationHost: HighlighterRegistrationHost, private val editorColorsManager: EditorColorsManager) : IProtocolHighlighterModelSupport {
    override fun createHandler(lifetime: Lifetime, markupModel: RdMarkupModel, document: Document): IProtocolHighlighterModelHandler? {
        return UnityPerformanceHighlighterModelHandler(registrationHost, editorColorsManager);
    }

}