package com.jetbrains.resharper.plugins.unity.ideaInterop.fileTypes.shaderLab

import com.intellij.lexer.DummyLexer
import com.intellij.openapi.project.Project
import com.intellij.psi.tree.IElementType
import com.jetbrains.resharper.ideaInterop.fileTypes.RiderFileElementType
import com.jetbrains.resharper.ideaInterop.fileTypes.RiderParserDefinitionBase

class ShaderLabParserDefinition : RiderParserDefinitionBase(ShaderLabFileElementType, ShaderLabFileType) {
    companion object {
        val ShaderLabElementType = IElementType("RIDER_SHADERLAB", ShaderLabLanguage)
        val ShaderLabFileElementType = RiderFileElementType("RIDER_SHADERLAB_FILE", ShaderLabLanguage, ShaderLabElementType)
    }

    override fun createLexer(project: Project?) = DummyLexer(ShaderLabFileElementType)
}